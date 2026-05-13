using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using FS_Dynamic.Models;

namespace FS_Dynamic
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Обработчик запуска приложения - вызывается ПЕРВЫМ при старте
        /// Здесь решаем какое окно открывать
        /// </summary>
        /// 

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var loginWindow = new LoginWindow();
            bool? loginResult = loginWindow.ShowDialog();


            if (loginResult == true && loginWindow.CurrentUser != null)
            {
                switch (loginWindow.CurrentUser.Role.ToLower())
                {
                    case "admin":
                        var modeWindow = new CompetitionModeWindow();
                        bool? modeResult = modeWindow.ShowDialog();
                        if (modeResult != true)
                        {
                            Current.Shutdown();
                            return;
                        }

                        switch (modeWindow.SelectedMode)
                        {
                            case CompetitionMode.Local:
                                new MainWindow(jockerMode: false).Show();
                                break;
                            case CompetitionMode.Jocker:
                                new MainWindow(jockerMode: true).Show();
                                break;
                            case CompetitionMode.Qualification:
                                new QualificationTimerWindow().Show();
                                break;
                            case CompetitionMode.Bracket:
                                new BracketTimerWindow().Show();
                                break;
                            default:
                                new MainWindow(jockerMode: false).Show();
                                break;
                        }

                        break;
                    case "operator":
                        new OperatorWindow().Show();
                        break;
                    case "athlete":
                        new AthleteWindow().Show();
                        break;
                    default:
                        MessageBox.Show($"Неизвестная роль: {loginWindow.CurrentUser.Role}");
                        Current.Shutdown();
                        break;
                }
            }
            else
            {
                Current.Shutdown();
            }
        }

  
    }
}
