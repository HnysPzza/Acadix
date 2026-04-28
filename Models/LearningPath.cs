namespace AcadsJulie.Models;

public class LearningPath
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string DifficultyLevel { get; set; } = "Beginner";
    public int EstimatedHours { get; set; }
    public string ThumbnailEmoji { get; set; } = "📚";
    public List<string> Prerequisites { get; set; } = new();
    public List<Lesson> Lessons { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsFeatured { get; set; }
    
    public int TotalLessons => Lessons.Count;
    public string DurationLabel => EstimatedHours == 1 ? "1 hour" : $"{EstimatedHours} hours";
    public string LessonsLabel => TotalLessons == 1 ? "1 lesson" : $"{TotalLessons} lessons";
}

public class Lesson
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public string ContentType { get; set; } = "Text";
    public string Content { get; set; } = string.Empty;
    public string VideoUrl { get; set; } = string.Empty;
    public List<LessonQuiz> Quizzes { get; set; } = new();
    public int EstimatedMinutes { get; set; } = 10;
    public List<string> KeyConcepts { get; set; } = new();
    
    public string DurationLabel => EstimatedMinutes == 1 ? "1 min" : $"{EstimatedMinutes} min";
}

public class LessonQuiz
{
    public string Question { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public int CorrectAnswerIndex { get; set; }
    public string Explanation { get; set; } = string.Empty;
}

public class CourseProgress
{
    public string UserId { get; set; } = "default";
    public string CourseId { get; set; } = string.Empty;
    public List<string> CompletedLessonIds { get; set; } = new();
    public Dictionary<string, int> QuizScores { get; set; } = new();
    public DateTime EnrolledAt { get; set; } = DateTime.Now;
    public DateTime? LastAccessedAt { get; set; }
    public int TotalTimeSpentMinutes { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    public double ProgressPercentage(int totalLessons)
    {
        if (totalLessons == 0) return 0;
        return (double)CompletedLessonIds.Count / totalLessons * 100;
    }
    
    public string ProgressLabel(int totalLessons) => 
        $"{CompletedLessonIds.Count}/{totalLessons} lessons";
}
