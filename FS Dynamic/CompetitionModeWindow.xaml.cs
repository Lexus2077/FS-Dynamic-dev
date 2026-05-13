using System.Windows;

namespace FS_Dynamic
{
    public partial class CompetitionModeWindow : Window
    {
        public CompetitionMode SelectedMode { get; private set; } = CompetitionMode.Local;

        public CompetitionModeWindow()
        {
            InitializeComponent();
        }

        private void BtnLocal_Click(object sender, RoutedEventArgs e)
        {
            SelectedMode = CompetitionMode.Local;
            DialogResult = true;
            Close();
        }

        private void BtnJocker_Click(object sender, RoutedEventArgs e)
        {
            SelectedMode = CompetitionMode.Jocker;
            DialogResult = true;
            Close();
        }

        private void BtnQual_Click(object sender, RoutedEventArgs e)
        {
            SelectedMode = CompetitionMode.Qualification;
            DialogResult = true;
            Close();
        }

        private void BtnBracket_Click(object sender, RoutedEventArgs e)
        {
            SelectedMode = CompetitionMode.Bracket;
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
