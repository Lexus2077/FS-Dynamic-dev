using System.Windows;

namespace FS_Dynamic
{
    /// <summary>
    /// Режим работы главного окна таймера (выбирается после входа администратора).
    /// </summary>
    public enum CompetitionMode
    {
        /// <summary>Результаты в локальные файлы (Teams.txt / Rounds.txt).</summary>
        Local = 0,

        /// <summary>Рулетка (Joker) — API.</summary>
        Jocker = 1,

        /// <summary>Квалификация — API.</summary>
        Qualification = 2,

        /// <summary>Турнирная сетка — API.</summary>
        Bracket = 3,
    }

    /// <summary>
    /// Повторный выбор режима из окна таймера без перезапуска приложения.
    /// </summary>
    public static class CompetitionModeNavigator
    {
        public static void ShowModePickerAndSwitch(Window currentWindow)
        {
            var modeWindow = new CompetitionModeWindow { Owner = currentWindow };
            if (modeWindow.ShowDialog() != true)
            {
                return;
            }

            Window next;
            switch (modeWindow.SelectedMode)
            {
                case CompetitionMode.Local:
                    next = new MainWindow(jockerMode: false);
                    break;
                case CompetitionMode.Jocker:
                    next = new MainWindow(jockerMode: true);
                    break;
                case CompetitionMode.Qualification:
                    next = new QualificationTimerWindow();
                    break;
                case CompetitionMode.Bracket:
                    next = new BracketTimerWindow();
                    break;
                default:
                    next = new MainWindow(jockerMode: false);
                    break;
            }

            next.Show();
            currentWindow.Close();
        }
    }
}
