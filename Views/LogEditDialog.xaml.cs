using LifeOS.Models;
using System.Windows;

namespace LifeOS.Views;

public partial class LogEditDialog : Window
{
    public LearningLog Log { get; } = new();

    public LogEditDialog()
    {
        InitializeComponent();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        Log.Subject = SubjectBox.Text;
        Log.Topic = TopicBox.Text;
        Log.Note = NoteBox.Text;
        Log.DurationMinutes = int.TryParse(DurationBox.Text, out var d) ? d : 0;
        Log.LoggedAt = DateTime.Now;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
