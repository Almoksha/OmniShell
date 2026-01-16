using System;
using System.Diagnostics;
using System.Windows;

namespace OmniShell.Views.Widgets;

public partial class CalendarWidget : System.Windows.Controls.UserControl
{
    public CalendarWidget()
    {
        InitializeComponent();
        UpdateDate();
    }

    private void UpdateDate()
    {
        var now = DateTime.Now;
        DayOfWeekText.Text = now.ToString("dddd");
        DayNumberText.Text = now.Day.ToString();
        MonthYearText.Text = now.ToString("MMMM yyyy");
    }

    private void OpenCalendarButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "outlookcal:",
                UseShellExecute = true
            });
        }
        catch
        {
            // Fallback to Windows Calendar
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "ms-clock:",
                    UseShellExecute = true
                });
            }
            catch { }
        }
    }
}
