using System.Text.Json;
using AcadsJulie.Models;

namespace AcadsJulie.Services;

public class GoalService
{
    private const string GoalsKey = "user_goals";
    private const string HabitsKey = "user_habits";
    private List<Goal>? _cachedGoals;
    private List<Habit>? _cachedHabits;

    public List<Goal> GetGoals()
    {
        if (_cachedGoals != null)
            return _cachedGoals;

        var json = Preferences.Get(GoalsKey, null);
        _cachedGoals = json == null
            ? new List<Goal>()
            : JsonSerializer.Deserialize<List<Goal>>(json) ?? new List<Goal>();

        return _cachedGoals;
    }

    public void AddGoal(Goal goal)
    {
        var goals = GetGoals();
        goals.Add(goal);
        SaveGoals(goals);
    }

    public void UpdateGoalProgress(string goalId, int newValue)
    {
        var goals = GetGoals();
        var goal = goals.FirstOrDefault(g => g.Id == goalId);
        if (goal == null) return;

        goal.CurrentValue = newValue;
        
        if (goal.CurrentValue >= goal.TargetValue && !goal.IsCompleted)
        {
            goal.IsCompleted = true;
            goal.CompletedAt = DateTime.Now;
            App.ProfileService.AddXP(50);
        }

        SaveGoals(goals);
    }

    public void DeleteGoal(string goalId)
    {
        var goals = GetGoals();
        goals.RemoveAll(g => g.Id == goalId);
        SaveGoals(goals);
    }

    public List<Habit> GetHabits()
    {
        if (_cachedHabits != null)
            return _cachedHabits;

        var json = Preferences.Get(HabitsKey, null);
        _cachedHabits = json == null
            ? new List<Habit>()
            : JsonSerializer.Deserialize<List<Habit>>(json) ?? new List<Habit>();

        return _cachedHabits;
    }

    public void AddHabit(Habit habit)
    {
        var habits = GetHabits();
        habits.Add(habit);
        SaveHabits(habits);
    }

    public void LogHabitCompletion(string habitId)
    {
        var habits = GetHabits();
        var habit = habits.FirstOrDefault(h => h.Id == habitId);
        if (habit == null) return;

        if (!habit.CompletedToday)
        {
            habit.CompletedDates.Add(DateTime.Now);
            App.ProfileService.AddXP(10);
        }

        SaveHabits(habits);
    }

    public void DeleteHabit(string habitId)
    {
        var habits = GetHabits();
        habits.RemoveAll(h => h.Id == habitId);
        SaveHabits(habits);
    }

    public List<Goal> GetActiveGoals()
    {
        return GetGoals().Where(g => !g.IsCompleted).OrderBy(g => g.CreatedAt).ToList();
    }

    public List<Habit> GetActiveHabits()
    {
        return GetHabits().Where(h => h.IsActive).OrderByDescending(h => h.CurrentStreak).ToList();
    }

    private void SaveGoals(List<Goal> goals)
    {
        _cachedGoals = goals;
        Preferences.Set(GoalsKey, JsonSerializer.Serialize(goals));
    }

    private void SaveHabits(List<Habit> habits)
    {
        _cachedHabits = habits;
        Preferences.Set(HabitsKey, JsonSerializer.Serialize(habits));
    }
}
