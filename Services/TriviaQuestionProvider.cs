using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AcadsJulie.Data;
using AcadsJulie.Models;

namespace AcadsJulie.Services;

/// <summary>
/// Single source of trivia questions for the game.
///
/// Combines the Open Trivia Database with the built-in <see cref="TriviaQuestionBank"/> and
/// enforces the two rules the game needs:
///
/// <list type="number">
/// <item><b>Difficulty matters.</b> Requested difficulty is passed to the API and used to filter
/// local questions, falling back to neighbouring levels only when a level runs dry.</item>
/// <item><b>No repeats.</b> Every question served is remembered per player, so a question will
/// not come back until the whole pool for that category has been used. OpenTDB session tokens
/// give this server-side; the local history covers offline play, the local bank, and repeats
/// across token resets.</item>
/// </list>
///
/// Remote failure is never fatal — the local bank always backs the session up, so the quiz works
/// offline exactly as it did before.
/// </summary>
public class TriviaQuestionProvider
{
    /// <summary>Cap on remembered questions per category+difficulty scope.</summary>
    private const int MaxHistoryPerScope = 400;

    private const string HistoryKeyPrefix = "trivia_seen_";

    private readonly OpenTriviaService _openTrivia;

    public TriviaQuestionProvider(OpenTriviaService openTrivia)
    {
        _openTrivia = openTrivia;
    }

    public TriviaQuestionProvider() : this(new OpenTriviaService()) { }

    /// <summary>Describes where a batch of questions came from, for UI messaging.</summary>
    public sealed class QuestionBatch
    {
        public List<TriviaQuestion> Questions { get; init; } = [];

        /// <summary>How many came from OpenTDB.</summary>
        public int RemoteCount { get; init; }

        /// <summary>How many came from the built-in bank.</summary>
        public int LocalCount { get; init; }

        /// <summary>True when the API could not be reached and only local questions were used.</summary>
        public bool UsedOfflineFallback { get; init; }

        /// <summary>True when the no-repeat history was cleared because the pool was exhausted.</summary>
        public bool HistoryRecycled { get; init; }
    }

    /// <summary>
    /// Builds a question set for one game.
    /// </summary>
    /// <param name="field">Acadix field, e.g. "Science".</param>
    /// <param name="subField">Acadix sub-field, e.g. "Astronomy".</param>
    /// <param name="difficulty">"Easy", "Medium" or "Hard".</param>
    /// <param name="count">How many questions the game wants.</param>
    public async Task<QuestionBatch> GetQuestionsAsync(
        string field,
        string? subField,
        string difficulty,
        int count,
        CancellationToken cancellationToken = default)
    {
        if (count <= 0)
            return new QuestionBatch();

        var scopeKey = BuildScopeKey(field, subField, difficulty);
        var seen = LoadHistory(scopeKey);

        var selected = new List<TriviaQuestion>(count);
        var chosenHashes = new HashSet<string>(StringComparer.Ordinal);
        var remoteCount = 0;
        var usedOfflineFallback = false;

        // ---- 1. Remote first ------------------------------------------------------------
        // OpenTDB is the primary source; its session token already avoids repeats server-side,
        // and the local history filter below catches anything the token missed.
        if (TriviaCategoryMap.GetCategoryId(field, subField) is { } categoryId)
        {
            var result = await _openTrivia.GetQuestionsAsync(categoryId, difficulty, count, cancellationToken);

            if (result.Status == OpenTriviaService.FetchStatus.Unavailable)
                usedOfflineFallback = true;

            foreach (var question in result.Questions)
            {
                if (selected.Count >= count)
                    break;

                var hash = HashQuestion(question.QuestionText);
                if (seen.Contains(hash) || !chosenHashes.Add(hash))
                    continue;

                // The API does not know about Acadix's taxonomy; tag it so stats, themes and the
                // parrot outfit resolver keep working.
                question.Field = field;
                question.SubField = subField ?? string.Empty;

                selected.Add(question);
                remoteCount++;
            }
        }

        // ---- 2. Top up from the local bank ----------------------------------------------
        var localAdded = FillFromLocalBank(field, subField, difficulty, count, selected, chosenHashes, seen);

        // ---- 3. Pool exhausted: recycle -------------------------------------------------
        // Only recycle when the *history* is what is holding us back. If nothing has been seen
        // yet and we are still short, the category is simply small (or we are offline) — wiping
        // history then would clear it on every single game and destroy the no-repeat guarantee.
        var historyRecycled = false;
        if (selected.Count < count && seen.Count > 0)
        {
            historyRecycled = true;

            // Release the oldest half first. Questions seen longest ago come back before recent
            // ones, so a small category rotates instead of replaying the last game verbatim.
            seen.ForgetOldest(seen.Count / 2);
            localAdded += FillFromLocalBank(field, subField, difficulty, count, selected, chosenHashes, seen);

            // Still short: the pool genuinely cannot fill a full quiz, so start a clean cycle.
            if (selected.Count < count)
            {
                seen.Clear();
                localAdded += FillFromLocalBank(field, subField, difficulty, count, selected, chosenHashes, seen);
            }
        }

        // Randomise presentation order so remote and local questions interleave rather than
        // appearing as two distinct blocks.
        var ordered = selected.OrderBy(_ => Random.Shared.Next()).ToList();

        SaveHistory(scopeKey, seen, ordered.Select(q => HashQuestion(q.QuestionText)));

        return new QuestionBatch
        {
            Questions = ordered,
            RemoteCount = remoteCount,
            LocalCount = localAdded,
            UsedOfflineFallback = usedOfflineFallback,
            HistoryRecycled = historyRecycled
        };
    }

    /// <summary>
    /// Adds unseen local questions, preferring the requested difficulty and widening to
    /// neighbouring levels only if that level cannot fill the quiz.
    /// </summary>
    private static int FillFromLocalBank(
        string field,
        string? subField,
        string difficulty,
        int count,
        List<TriviaQuestion> selected,
        HashSet<string> chosenHashes,
        SeenHistory seen)
    {
        if (selected.Count >= count)
            return 0;

        // Ask the bank for a generous pool so there is room to skip already-seen questions.
        var pool = TriviaQuestionBank.GetQuestions(field, subField, difficulty, count * 4);
        var added = 0;

        foreach (var question in pool)
        {
            if (selected.Count >= count)
                break;

            var hash = HashQuestion(question.QuestionText);
            if (seen.Contains(hash) || !chosenHashes.Add(hash))
                continue;

            selected.Add(question);
            added++;
        }

        return added;
    }

    // ---- No-repeat history ----------------------------------------------------------------
    //
    // Only a short hash of the question text is stored, not the text itself: it keeps the
    // Preferences entry small and means the history stays valid even if wording is tweaked
    // upstream. History is per account because it goes through ScopedPreferences.

    /// <summary>
    /// Seen-question hashes for one scope, oldest first. Insertion order matters because the
    /// history is trimmed from the front, so the longest-unseen questions return first.
    /// </summary>
    private sealed class SeenHistory
    {
        private readonly List<string> _ordered;
        private readonly HashSet<string> _lookup;

        public SeenHistory(IEnumerable<string> hashes)
        {
            _ordered = [];
            _lookup = new HashSet<string>(StringComparer.Ordinal);

            foreach (var hash in hashes)
                Add(hash);
        }

        public int Count => _ordered.Count;

        public bool Contains(string hash) => _lookup.Contains(hash);

        public void Add(string hash)
        {
            if (_lookup.Add(hash))
                _ordered.Add(hash);
        }

        public void Clear()
        {
            _ordered.Clear();
            _lookup.Clear();
        }

        /// <summary>
        /// Drops the <paramref name="howMany"/> oldest entries, making those questions eligible
        /// again while recently-seen ones stay blocked.
        /// </summary>
        public void ForgetOldest(int howMany)
        {
            howMany = Math.Min(howMany, _ordered.Count);
            if (howMany <= 0)
                return;

            for (var i = 0; i < howMany; i++)
                _lookup.Remove(_ordered[i]);

            _ordered.RemoveRange(0, howMany);
        }

        /// <summary>Most recent <paramref name="max"/> entries, oldest first.</summary>
        public List<string> ToTrimmedList(int max) =>
            _ordered.Count <= max
                ? new List<string>(_ordered)
                : _ordered.GetRange(_ordered.Count - max, max);
    }

    private static SeenHistory LoadHistory(string scopeKey)
    {
        var json = ScopedPreferences.Get(HistoryKeyPrefix + scopeKey, null);
        if (string.IsNullOrWhiteSpace(json))
            return new SeenHistory([]);

        try
        {
            return new SeenHistory(JsonSerializer.Deserialize<List<string>>(json) ?? []);
        }
        catch (JsonException)
        {
            return new SeenHistory([]);
        }
    }

    private static void SaveHistory(string scopeKey, SeenHistory seen, IEnumerable<string> newHashes)
    {
        foreach (var hash in newHashes)
            seen.Add(hash);

        // Bound the stored history. Dropping the oldest entries means very old questions can
        // eventually reappear, which is the intended behaviour for a long-lived player.
        ScopedPreferences.Set(
            HistoryKeyPrefix + scopeKey,
            JsonSerializer.Serialize(seen.ToTrimmedList(MaxHistoryPerScope)));
    }

    private static void ClearHistory(string scopeKey) =>
        ScopedPreferences.Remove(HistoryKeyPrefix + scopeKey);

    /// <summary>
    /// All history keys this app can generate. Preferences has no key enumeration API, so the
    /// combinations are derived from the categories the setup screen can produce.
    /// </summary>
    public static IEnumerable<string> GetAllHistoryKeys()
    {
        // field -> sub-fields offered by TriviaSetupViewModel and the local bank.
        (string Field, string[] SubFields)[] scopes =
        [
            ("History", ["WorldHistory", "PhilippineHistory"]),
            ("Math",    ["Arithmetic", "Algebra", "Geometry", "Statistics"]),
            ("Science", ["General", "Physics", "Chemistry", "EarthScience"]),
            ("Space",   ["Astronomy", "SpaceExploration"]),
            ("Biology", ["HumanBody", "Cells", "Ecology", "Biology"]),
            ("Animals", ["Land", "Sea", "Air"]),
            ("General", ["GeneralKnowledge"])
        ];

        string[] difficulties = ["Easy", "Medium", "Hard"];

        foreach (var (field, subFields) in scopes)
            foreach (var subField in subFields)
                foreach (var difficulty in difficulties)
                    yield return HistoryKeyPrefix + BuildScopeKey(field, subField, difficulty);
    }

    /// <summary>Clears seen-question history for every category (used by Reset Progress).</summary>
    public static void ClearAllHistory()
    {
        foreach (var key in GetAllHistoryKeys())
            ScopedPreferences.Remove(key);
    }

    private static string BuildScopeKey(string field, string? subField, string difficulty) =>
        $"{field}_{subField}_{difficulty}".ToLowerInvariant().Replace(' ', '_');

    /// <summary>Short, stable hash of the normalised question text.</summary>
    private static string HashQuestion(string questionText)
    {
        var normalised = questionText.Trim().ToLowerInvariant();
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalised));

        // 8 bytes is ample: collisions across a few hundred questions are vanishingly unlikely.
        return Convert.ToHexString(bytes, 0, 8);
    }
}
