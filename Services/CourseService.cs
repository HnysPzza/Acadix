using System.Text.Json;
using AcadsJulie.Models;

namespace AcadsJulie.Services;

public class CourseService
{
    private const string ProgressKey = "course_progress";
    private const string EnrolledCoursesKey = "enrolled_courses";
    private List<LearningPath>? _cachedCourses;
    private Dictionary<string, CourseProgress>? _cachedProgress;

    public List<LearningPath> GetAllCourses()
    {
        if (_cachedCourses != null)
            return _cachedCourses;

        _cachedCourses = GetSampleCourses();
        return _cachedCourses;
    }

    public List<LearningPath> GetCoursesByCategory(string category)
    {
        return GetAllCourses()
            .Where(c => c.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public List<LearningPath> GetFeaturedCourses()
    {
        return GetAllCourses().Where(c => c.IsFeatured).ToList();
    }

    public LearningPath? GetCourseById(string courseId)
    {
        return GetAllCourses().FirstOrDefault(c => c.Id == courseId);
    }

    public List<LearningPath> GetEnrolledCourses()
    {
        var enrolledIds = GetEnrolledCourseIds();
        return GetAllCourses().Where(c => enrolledIds.Contains(c.Id)).ToList();
    }

    public void EnrollInCourse(string courseId)
    {
        var enrolledIds = GetEnrolledCourseIds();
        if (!enrolledIds.Contains(courseId))
        {
            enrolledIds.Add(courseId);
            SaveEnrolledCourseIds(enrolledIds);

            var progress = new CourseProgress
            {
                CourseId = courseId,
                EnrolledAt = DateTime.Now
            };
            SaveProgress(courseId, progress);
        }
    }

    public CourseProgress GetProgress(string courseId)
    {
        var allProgress = GetAllProgress();
        if (allProgress.TryGetValue(courseId, out var progress))
            return progress;

        return new CourseProgress { CourseId = courseId };
    }

    public void CompleteLesson(string courseId, string lessonId)
    {
        var progress = GetProgress(courseId);
        if (!progress.CompletedLessonIds.Contains(lessonId))
        {
            progress.CompletedLessonIds.Add(lessonId);
            progress.LastAccessedAt = DateTime.Now;

            var course = GetCourseById(courseId);
            if (course != null && progress.CompletedLessonIds.Count == course.TotalLessons)
            {
                progress.IsCompleted = true;
                progress.CompletedAt = DateTime.Now;
                
                App.ProfileService.AddXP(100);
            }
            else
            {
                App.ProfileService.AddXP(20);
            }

            SaveProgress(courseId, progress);
        }
    }

    public void SaveQuizScore(string courseId, string lessonId, int score)
    {
        var progress = GetProgress(courseId);
        progress.QuizScores[lessonId] = score;
        progress.LastAccessedAt = DateTime.Now;
        
        if (score >= 80)
            App.ProfileService.AddXP(10);
        
        SaveProgress(courseId, progress);
    }

    public void AddStudyTime(string courseId, int minutes)
    {
        var progress = GetProgress(courseId);
        progress.TotalTimeSpentMinutes += minutes;
        progress.LastAccessedAt = DateTime.Now;
        SaveProgress(courseId, progress);
    }

    public List<string> GetCategories()
    {
        return GetAllCourses()
            .Select(c => c.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToList();
    }

    private Dictionary<string, CourseProgress> GetAllProgress()
    {
        if (_cachedProgress != null)
            return _cachedProgress;

        var json = Preferences.Get(ProgressKey, null);
        _cachedProgress = json == null
            ? new Dictionary<string, CourseProgress>()
            : JsonSerializer.Deserialize<Dictionary<string, CourseProgress>>(json) ?? new Dictionary<string, CourseProgress>();

        return _cachedProgress;
    }

    private void SaveProgress(string courseId, CourseProgress progress)
    {
        var allProgress = GetAllProgress();
        allProgress[courseId] = progress;
        _cachedProgress = allProgress;
        Preferences.Set(ProgressKey, JsonSerializer.Serialize(allProgress));
    }

    private List<string> GetEnrolledCourseIds()
    {
        var json = Preferences.Get(EnrolledCoursesKey, null);
        return json == null
            ? new List<string>()
            : JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
    }

    private void SaveEnrolledCourseIds(List<string> ids)
    {
        Preferences.Set(EnrolledCoursesKey, JsonSerializer.Serialize(ids));
    }

    private List<LearningPath> GetSampleCourses()
    {
        return new List<LearningPath>
        {
            new LearningPath
            {
                Id = "algebra-basics",
                Title = "Algebra Fundamentals",
                Description = "Master the basics of algebra including equations, variables, and problem-solving",
                Category = "Mathematics",
                DifficultyLevel = "Beginner",
                EstimatedHours = 8,
                ThumbnailEmoji = "🔢",
                IsFeatured = true,
                Lessons = new List<Lesson>
                {
                    new Lesson
                    {
                        Id = "lesson-1",
                        Title = "Introduction to Variables",
                        OrderIndex = 1,
                        Content = "Variables are symbols that represent unknown values. In algebra, we use letters like x, y, and z to represent these values.\n\nFor example:\n• x + 5 = 10\n• 2y = 12\n• z - 3 = 7\n\nVariables allow us to write general rules and solve problems where we don't know all the values yet.",
                        EstimatedMinutes = 15,
                        KeyConcepts = new List<string> { "Variables", "Unknown values", "Algebraic expressions" }
                    },
                    new Lesson
                    {
                        Id = "lesson-2",
                        Title = "Solving Simple Equations",
                        OrderIndex = 2,
                        Content = "To solve an equation, we need to isolate the variable on one side.\n\nSteps:\n1. Identify the variable\n2. Use inverse operations\n3. Simplify both sides\n4. Check your answer\n\nExample: x + 5 = 10\nSubtract 5 from both sides: x = 5",
                        EstimatedMinutes = 20,
                        KeyConcepts = new List<string> { "Equations", "Inverse operations", "Solving for x" }
                    }
                }
            },
            new LearningPath
            {
                Id = "philippine-history",
                Title = "Philippine History 101",
                Description = "Explore key events and figures in Philippine history from pre-colonial times to modern era",
                Category = "History",
                DifficultyLevel = "Beginner",
                EstimatedHours = 10,
                ThumbnailEmoji = "🇵🇭",
                IsFeatured = true,
                Lessons = new List<Lesson>
                {
                    new Lesson
                    {
                        Id = "lesson-1",
                        Title = "Pre-Colonial Philippines",
                        OrderIndex = 1,
                        Content = "Before Spanish colonization, the Philippines had thriving communities with their own systems of government, trade, and culture.\n\nKey Points:\n• Barangays were the basic political units\n• Trade with China, India, and Southeast Asian neighbors\n• Rich oral traditions and indigenous writing systems\n• Animistic beliefs and early Islamic influence",
                        EstimatedMinutes = 25,
                        KeyConcepts = new List<string> { "Barangay system", "Pre-colonial trade", "Indigenous culture" }
                    }
                }
            },
            new LearningPath
            {
                Id = "biology-basics",
                Title = "Introduction to Biology",
                Description = "Learn the fundamentals of life science including cells, organisms, and ecosystems",
                Category = "Science",
                DifficultyLevel = "Beginner",
                EstimatedHours = 12,
                ThumbnailEmoji = "🧬",
                IsFeatured = false,
                Lessons = new List<Lesson>
                {
                    new Lesson
                    {
                        Id = "lesson-1",
                        Title = "What is Life?",
                        OrderIndex = 1,
                        Content = "Living things share common characteristics that distinguish them from non-living things.\n\nCharacteristics of Life:\n1. Organization - cells and structures\n2. Metabolism - energy processing\n3. Growth - increase in size\n4. Reproduction - creating offspring\n5. Response to stimuli\n6. Homeostasis - maintaining balance\n7. Adaptation - evolution over time",
                        EstimatedMinutes = 20,
                        KeyConcepts = new List<string> { "Characteristics of life", "Living organisms", "Biology basics" }
                    }
                }
            },
            new LearningPath
            {
                Id = "english-grammar",
                Title = "English Grammar Essentials",
                Description = "Master the fundamentals of English grammar for better communication",
                Category = "Language",
                DifficultyLevel = "Beginner",
                EstimatedHours = 6,
                ThumbnailEmoji = "📝",
                IsFeatured = false,
                Lessons = new List<Lesson>
                {
                    new Lesson
                    {
                        Id = "lesson-1",
                        Title = "Parts of Speech",
                        OrderIndex = 1,
                        Content = "Understanding the eight parts of speech is fundamental to grammar.\n\n1. Nouns - person, place, thing, or idea\n2. Pronouns - replace nouns (he, she, it)\n3. Verbs - action or state of being\n4. Adjectives - describe nouns\n5. Adverbs - describe verbs, adjectives, or other adverbs\n6. Prepositions - show relationships (in, on, at)\n7. Conjunctions - connect words or phrases (and, but, or)\n8. Interjections - express emotion (wow, ouch)",
                        EstimatedMinutes = 18,
                        KeyConcepts = new List<string> { "Parts of speech", "Grammar basics", "Word types" }
                    }
                }
            }
        };
    }
}
