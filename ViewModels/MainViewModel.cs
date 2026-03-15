using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LifeOS.Models;
using LifeOS.Services;
using System.Collections.ObjectModel;

namespace LifeOS.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public readonly DatabaseService Db;

    [ObservableProperty] private ObservableCollection<StickyNote> _notes = new();
    [ObservableProperty] private ObservableCollection<TaskItem> _tasks = new();
    [ObservableProperty] private ObservableCollection<LearningGoal> _goals = new();

    public MainViewModel(DatabaseService db)
    {
        Db = db;
        LoadAll();
    }

    public void LoadAll()
    {
        Notes = new ObservableCollection<StickyNote>(Db.GetAllNotes());
        Tasks = new ObservableCollection<TaskItem>(Db.GetAllTasks());
        Goals = new ObservableCollection<LearningGoal>(Db.GetAllGoals());
    }

    public void ReloadTasks()
    {
        Tasks = new ObservableCollection<TaskItem>(Db.GetAllTasks());
    }

    // --- Notes ---
    [RelayCommand]
    public void AddNote()
    {
        var note = Db.AddNote(new StickyNote { Title = "新しいノート" });
        Notes.Add(note);
        Db.AddOperationLog("ノート追加", note.Title);
    }

    public void UpdateNote(StickyNote note) => Db.UpdateNote(note);

    public void DeleteNote(StickyNote note)
    {
        Db.AddOperationLog("ノート削除", note.Title);
        Db.DeleteNote(note.Id);
        Notes.Remove(note);
        ReloadTasks();
    }

    // --- Tasks ---
    [RelayCommand]
    public void AddTask()
    {
        var task = Db.AddTask(new TaskItem { Title = "新しいタスク" });
        Tasks.Add(task);
        Db.AddOperationLog("タスク追加", task.Title);
    }

    public void UpdateTask(TaskItem task)
    {
        Db.UpdateTask(task);
        if (task.IsCompleted) Db.AddOperationLog("タスク完了", task.Title);
    }

    public void DeleteTask(TaskItem task)
    {
        Db.AddOperationLog("タスク削除", task.Title);
        Db.DeleteTask(task.Id);
        Tasks.Remove(task);
    }

    // --- Goals ---
    [RelayCommand]
    public void AddGoal()
    {
        var goal = Db.AddGoal(new LearningGoal { Title = "新しい目標" });
        Goals.Add(goal);
        Db.AddOperationLog("目標追加", goal.Title);
    }

    public void UpdateGoal(LearningGoal goal)
    {
        Db.UpdateGoal(goal);
        Db.AddOperationLog("目標更新", goal.Title);
    }

    public void DeleteGoal(LearningGoal goal)
    {
        Db.AddOperationLog("目標削除", goal.Title);
        Db.DeleteGoal(goal.Id);
        Goals.Remove(goal);
    }
}
