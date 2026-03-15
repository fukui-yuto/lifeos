using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LifeOS.Models;
using LifeOS.Services;
using System.Collections.ObjectModel;

namespace LifeOS.ViewModels;

public partial class StickyNoteViewModel : ObservableObject
{
    private readonly MainViewModel _main;
    private readonly DatabaseService _db;

    public StickyNote Note { get; }

    [ObservableProperty] private string _title;
    [ObservableProperty] private string _content;
    [ObservableProperty] private string _color;
    [ObservableProperty] private double _positionX;
    [ObservableProperty] private double _positionY;
    [ObservableProperty] private double _width;
    [ObservableProperty] private double _height;
    [ObservableProperty] private string _newTaskTitle = string.Empty;

    public ObservableCollection<TaskItem> Tasks { get; }

    public StickyNoteViewModel(StickyNote note, MainViewModel main, DatabaseService db)
    {
        Note = note;
        _main = main;
        _db = db;
        _title = note.Title;
        _content = note.Content;
        _color = note.Color;
        _positionX = note.PositionX;
        _positionY = note.PositionY;
        _width = note.Width;
        _height = note.Height;
        Tasks = new ObservableCollection<TaskItem>(note.Tasks.OrderBy(t => t.CreatedAt));
    }

    partial void OnTitleChanged(string value) => Save();
    partial void OnContentChanged(string value) => Save();
    partial void OnColorChanged(string value) => Save();
    partial void OnPositionXChanged(double value) => SavePosition();
    partial void OnPositionYChanged(double value) => SavePosition();
    partial void OnWidthChanged(double value) => SavePosition();
    partial void OnHeightChanged(double value) => SavePosition();

    private void Save()
    {
        Note.Title = Title;
        Note.Content = Content;
        Note.Color = Color;
        _main.UpdateNote(Note);
    }

    private void SavePosition()
    {
        Note.PositionX = PositionX;
        Note.PositionY = PositionY;
        Note.Width = Width;
        Note.Height = Height;
        _main.UpdateNote(Note);
    }

    [RelayCommand]
    public void AddTask()
    {
        if (string.IsNullOrWhiteSpace(NewTaskTitle)) return;
        var task = new TaskItem
        {
            Title = NewTaskTitle.Trim(),
            StickyNoteId = Note.Id
        };
        _db.AddTask(task);
        _db.AddOperationLog("タスク追加", task.Title);
        Tasks.Add(task);
        Note.Tasks.Add(task);
        NewTaskTitle = string.Empty;
        _main.ReloadTasks();
    }

    public void ToggleTask(TaskItem task)
    {
        task.IsCompleted = !task.IsCompleted;
        _db.UpdateTask(task);
        _db.AddOperationLog(task.IsCompleted ? "タスク完了" : "タスク未完了", task.Title);
        _main.ReloadTasks();
    }

    public void UpdateTaskTitle(TaskItem task, string newTitle)
    {
        task.Title = newTitle;
        _db.UpdateTask(task);
        _db.AddOperationLog("タスク更新", newTitle);
        _main.ReloadTasks();
    }

    public void DeleteTask(TaskItem task)
    {
        _db.AddOperationLog("タスク削除", task.Title);
        _db.DeleteTask(task.Id);
        Tasks.Remove(task);
        Note.Tasks.Remove(task);
        _main.ReloadTasks();
    }

    public void ReloadTasks()
    {
        var fresh = _db.GetAllNotes().FirstOrDefault(n => n.Id == Note.Id);
        if (fresh == null) return;
        Tasks.Clear();
        Note.Tasks.Clear();
        foreach (var t in fresh.Tasks.OrderBy(t => t.CreatedAt))
        {
            Tasks.Add(t);
            Note.Tasks.Add(t);
        }
    }

    [RelayCommand]
    public void Delete() => _main.DeleteNote(Note);
}
