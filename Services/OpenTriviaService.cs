using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AcadsJulie.Models;

namespace AcadsJulie.Services;

/// <summary>
/// Client for the Open Trivia Database (https://opentdb.com).
///
/// Two API behaviours drive the design here:
///
/// 1. <b>Session tokens.</b> Passing a token makes the API never return the same question twice
///    for that token, which is exactly the "no repeats" requirement. Tokens die after 6 hours of
///    inactivity, and once every question matching a query has been served the API returns
///    response code 4 and the token must be reset.
///
/// 2. <b>Rate limiting.</b> Each IP may only call the API once every 5 seconds (response code 5).
///    All requests therefore pass through a single gate that spaces them out, and callers should
///    fetch in batches rather than per question.
/// </summary>
public class OpenTriviaService
{
    private const string BaseUrl = "https://opentdb.com";
    private const string TokenKey = "opentdb_session_token";

    /// <summary>API allows one request per IP per 5 seconds; a little headroom avoids code 5.</summary>
    private static readonly TimeSpan MinRequestInterval = TimeSpan.FromMilliseconds(5500);

    /// <summary>Hard cap imposed by the API.</summary>
    public const int MaxQuestionsPerRequest = 50;

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(15) };

    // Serialises all outbound calls so the 5-second rule is honoured even when several parts of
    // the app ask for questions at once.
    private static readonly SemaphoreSlim RequestGate = new(1, 1);
    private static DateTimeOffset _lastRequestUtc = DateTimeOffset.MinValue;

    private string? _sessionToken;

    /// <summary>Outcome of a fetch, so callers can react to "offline" vs "no questions".</summary>
    public enum FetchStatus
    {
        Success,
        /// <summary>API had fewer questions than requested (response code 1). Partial results possible.</summary>
        NotEnoughQuestions,
        /// <summary>Network unreachable, timeout, or unexpected server response.</summary>
        Unavailable,
        /// <summary>Category/difficulty combination is not served remotely.</summary>
        Unsupported
    }

    public sealed class FetchResult
    {
        public FetchStatus Status { get; init; }
        public List<TriviaQuestion> Questions { get; init; } = [];
        public bool HasQuestions => Questions.Count > 0;
    }

    /// <summary>
    /// Fetches multiple-choice questions for an OpenTDB category.
    /// </summary>
    /// <param name="categoryId">OpenTDB category id, or null for any category.</param>
    /// <param name="difficulty">"Easy" / "Medium" / "Hard" (case-insensitive), or null for any.</param>
    /// <param name="amount">1–50. Values above 50 are clamped by the API contract.</param>
    public async Task<FetchResult> GetQuestionsAsync(
        int? categoryId,
        string? difficulty,
        int amount,
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
            return new FetchResult { Status = FetchStatus.Success };

        amount = Math.Min(amount, MaxQuestionsPerRequest);

        try
        {
            var token = await EnsureTokenAsync(cancellationToken);
            var response = await RequestQuestionsAsync(categoryId, difficulty, amount, token, cancellationToken);

            // Code 3 (token not found) and 4 (token exhausted) both mean: get a fresh token and
            // retry once. Code 4 in particular is expected — it just means this player has now
            // seen every question the API holds for that query.
            if (response is { ResponseCode: 3 or 4 })
            {
                await ResetTokenAsync(cancellationToken);
                token = await EnsureTokenAsync(cancellationToken);
                response = await RequestQuestionsAsync(categoryId, difficulty, amount, token, cancellationToken);
            }

            if (response == null)
                return new FetchResult { Status = FetchStatus.Unavailable };

            return response.ResponseCode switch
            {
                0 => new FetchResult
                {
                    Status = FetchStatus.Success,
                    Questions = MapQuestions(response.Results, difficulty)
                },

                // Not enough questions for this query — return whatever came back (often none)
                // and let the caller top up from the local bank.
                1 => new FetchResult
                {
                    Status = FetchStatus.NotEnoughQuestions,
                    Questions = MapQuestions(response.Results, difficulty)
                },

                // 2 = invalid parameter: our category/difficulty combination is not valid.
                2 => new FetchResult { Status = FetchStatus.Unsupported },

                // 5 = rate limited despite the gate (e.g. another app instance on the same IP).
                _ => new FetchResult { Status = FetchStatus.Unavailable }
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Offline, DNS failure, timeout, malformed JSON — all mean "use the local bank".
            System.Diagnostics.Debug.WriteLine($"[OpenTrivia] Fetch failed: {ex.Message}");
            return new FetchResult { Status = FetchStatus.Unavailable };
        }
    }

    private async Task<OpenTriviaResponse?> RequestQuestionsAsync(
        int? categoryId,
        string? difficulty,
        int amount,
        string? token,
        CancellationToken cancellationToken)
    {
        var url = new StringBuilder($"{BaseUrl}/api.php?amount={amount}");

        if (categoryId is > 0)
            url.Append($"&category={categoryId}");

        var normalisedDifficulty = NormaliseDifficulty(difficulty);
        if (normalisedDifficulty != null)
            url.Append($"&difficulty={normalisedDifficulty}");

        // Only multiple-choice: the game UI renders a 2x2 answer grid, so true/false questions
        // (which return 2 options) would leave it half empty.
        url.Append("&type=multiple");

        // Base64 sidesteps HTML-entity decoding entirely — OpenTDB content is full of &quot;,
        // &#039; and accented characters that are easy to get wrong.
        url.Append("&encode=base64");

        if (!string.IsNullOrWhiteSpace(token))
            url.Append($"&token={Uri.EscapeDataString(token)}");

        var json = await SendThrottledAsync(url.ToString(), cancellationToken);
        return json == null
            ? null
            : JsonSerializer.Deserialize<OpenTriviaResponse>(json);
    }

    /// <summary>Returns the cached session token, requesting one if needed.</summary>
    private async Task<string?> EnsureTokenAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_sessionToken))
            return _sessionToken;

        // Tokens survive app restarts (they live 6 hours), so reuse a stored one where possible
        // to keep the no-repeat guarantee across sessions.
        var stored = Preferences.Default.Get(TokenKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(stored))
        {
            _sessionToken = stored;
            return _sessionToken;
        }

        var json = await SendThrottledAsync($"{BaseUrl}/api_token.php?command=request", cancellationToken);
        if (json == null)
            return null;

        var response = JsonSerializer.Deserialize<OpenTriviaTokenResponse>(json);
        if (response is not { ResponseCode: 0 } || string.IsNullOrWhiteSpace(response.Token))
            return null;

        _sessionToken = response.Token;
        Preferences.Default.Set(TokenKey, _sessionToken);
        return _sessionToken;
    }

    /// <summary>
    /// Discards the current token. Called when the API reports the token is exhausted, which
    /// means the player has seen every remote question for that query.
    /// </summary>
    public async Task ResetTokenAsync(CancellationToken cancellationToken = default)
    {
        var token = _sessionToken;
        _sessionToken = null;
        Preferences.Default.Remove(TokenKey);

        if (string.IsNullOrWhiteSpace(token))
            return;

        try
        {
            await SendThrottledAsync($"{BaseUrl}/api_token.php?command=reset&token={Uri.EscapeDataString(token)}", cancellationToken);
        }
        catch
        {
            // The token is dropped locally either way; a failed reset just leaves it to expire.
        }
    }

    /// <summary>Performs a GET while enforcing the API's one-request-per-5-seconds rule.</summary>
    private static async Task<string?> SendThrottledAsync(string url, CancellationToken cancellationToken)
    {
        await RequestGate.WaitAsync(cancellationToken);
        try
        {
            var sinceLast = DateTimeOffset.UtcNow - _lastRequestUtc;
            if (sinceLast < MinRequestInterval)
                await Task.Delay(MinRequestInterval - sinceLast, cancellationToken);

            using var response = await Http.GetAsync(url, cancellationToken);
            _lastRequestUtc = DateTimeOffset.UtcNow;

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
                return null;

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        finally
        {
            RequestGate.Release();
        }
    }

    private static List<TriviaQuestion> MapQuestions(List<OpenTriviaResult>? results, string? requestedDifficulty)
    {
        if (results == null || results.Count == 0)
            return [];

        var mapped = new List<TriviaQuestion>(results.Count);

        foreach (var result in results)
        {
            var questionText = DecodeBase64(result.Question);
            var correct = DecodeBase64(result.CorrectAnswer);

            if (string.IsNullOrWhiteSpace(questionText) || string.IsNullOrWhiteSpace(correct))
                continue;

            var options = new List<string> { correct };
            foreach (var incorrect in result.IncorrectAnswers ?? [])
            {
                var decoded = DecodeBase64(incorrect);
                if (!string.IsNullOrWhiteSpace(decoded))
                    options.Add(decoded);
            }

            // The UI grid expects four options.
            if (options.Count != 4)
                continue;

            var difficulty = Capitalise(DecodeBase64(result.Difficulty)) is { Length: > 0 } d
                ? d
                : Capitalise(requestedDifficulty) ?? "Medium";

            mapped.Add(new TriviaQuestion
            {
                QuestionText = questionText,
                Options = options,
                CorrectAnswer = correct,
                Difficulty = difficulty,
                Depth = difficulty.Equals("Hard", StringComparison.OrdinalIgnoreCase) ? "Deep" : "General"
            });
        }

        return mapped;
    }

    private static string DecodeBase64(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        try
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(value)).Trim();
        }
        catch (FormatException)
        {
            // Shouldn't happen with encode=base64, but never let one bad row kill the batch.
            return string.Empty;
        }
    }

    private static string? NormaliseDifficulty(string? difficulty) =>
        difficulty?.Trim().ToLowerInvariant() switch
        {
            "easy" => "easy",
            "medium" => "medium",
            "hard" => "hard",
            _ => null
        };

    private static string? Capitalise(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : char.ToUpperInvariant(value[0]) + value[1..].ToLowerInvariant();

    private sealed class OpenTriviaResponse
    {
        [JsonPropertyName("response_code")]
        public int ResponseCode { get; set; }

        [JsonPropertyName("results")]
        public List<OpenTriviaResult>? Results { get; set; }
    }

    private sealed class OpenTriviaResult
    {
        [JsonPropertyName("question")]
        public string? Question { get; set; }

        [JsonPropertyName("correct_answer")]
        public string? CorrectAnswer { get; set; }

        [JsonPropertyName("incorrect_answers")]
        public List<string>? IncorrectAnswers { get; set; }

        [JsonPropertyName("difficulty")]
        public string? Difficulty { get; set; }
    }

    private sealed class OpenTriviaTokenResponse
    {
        [JsonPropertyName("response_code")]
        public int ResponseCode { get; set; }

        [JsonPropertyName("token")]
        public string? Token { get; set; }
    }
}
