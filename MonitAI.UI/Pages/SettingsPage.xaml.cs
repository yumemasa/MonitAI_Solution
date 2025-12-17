using System.Windows;
using System.Windows.Controls;

namespace MonitAI.UI.Pages
{
    /// <summary>
    /// Home page: Card layout with Goal input and Focus Duration slider
    /// </summary>
    public partial class Pattern1Page : UserControl
    {
        public Pattern1Page()
        {
            InitializeComponent();
        }

        private void GoalTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Enable/disable start button based on goal input
            if (StartButton != null)
            {
                StartButton.IsEnabled = !string.IsNullOrWhiteSpace(GoalTextBox.Text);
            }
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            // Placeholder for start focus session functionality
            string goal = GoalTextBox.Text ?? string.Empty;
            int focusTime = (int)Math.Round(TimeSlider.Value);
            MessageBox.Show($"Starting focus session\nGoal: {goal}\nDuration: {focusTime} minutes", "monitAI", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
