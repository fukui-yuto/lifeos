using LifeOS.Models;
using System.Windows;

namespace LifeOS.Views;

public partial class TaskEditDialog : Window
{
    public TaskItem Task { get; }

    public TaskEditDialog(TaskItem task)
    {
        InitializeComponent();
        Task = task;
        TitleBox.Text = task.Title;
        PriorityBox.SelectedIndex = (int)task.Priority;
        DueDatePicker.SelectedDate = task.DueDate;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        Task.Title = TitleBox.Text;
        Task.Priority = (Priority)PriorityBox.SelectedIndex;
        Task.DueDate = DueDatePicker.SelectedDate;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
