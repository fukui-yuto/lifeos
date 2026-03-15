using LifeOS.Models;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace LifeOS.Views;

public partial class GoalEditDialog : Window
{
    public LearningGoal Goal { get; }

    public GoalEditDialog(LearningGoal goal, IEnumerable<StickyNote>? notes = null)
    {
        InitializeComponent();
        Goal = goal;
        TitleBox.Text = goal.Title;
        DescriptionBox.Text = goal.Description;
        TargetDatePicker.SelectedDate = goal.TargetDate;

        if (notes != null)
        {
            var noteList = notes.ToList();
            NoteListBox.ItemsSource = noteList;
            var linkedIds = goal.LinkedNotes.Select(n => n.Id).ToHashSet();
            foreach (var note in noteList.Where(n => linkedIds.Contains(n.Id)))
                NoteListBox.SelectedItems.Add(note);
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TitleBox.Text)) return;
        Goal.Title = TitleBox.Text.Trim();
        Goal.Description = DescriptionBox.Text.Trim();
        Goal.TargetDate = TargetDatePicker.SelectedDate;
        Goal.LinkedNotes = NoteListBox.SelectedItems.Cast<StickyNote>().ToList();
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
