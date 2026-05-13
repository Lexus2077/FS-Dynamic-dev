using FS_Dynamic.Models;
using FS_Dynamic.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace FS_Dynamic
{
    public partial class MainWindow : Window, ITimerReadout
    {
        private readonly SerialPort sp = new SerialPort();
        private readonly string[] ports = SerialPort.GetPortNames();
        private readonly Stopwatch stopWatch = new Stopwatch();
        private string res;
        private int bust_q;
        private int skip_q;
        private TimeSpan ts_0 = new TimeSpan(0, 0, 0, 0, 0);

        private string path = "C:\\+\\FS_Arduino\\Results.txt";
        private string path_teams = "C:\\+\\FS_Arduino\\Teams.txt";
        private string path_each_tuch = "C:\\+\\FS_Arduino\\Result_each_tuch.txt";
        private string team_name;
        private string round_number;
        private string path_rounds = "C:\\+\\FS_Arduino\\Rounds.txt";
        private int off_q;
        private string ready = "Ready";
        private string set = "Set";

        private string resultwithbusts;

        public string TimeValue => Result.Text;
        public string FinalTimeValue => Result_plus_Busts.Text;
        public string BustValue => Bust_Q.Text;
        public string SkipValue => Skip_Q.Text;

        public string SelectedTeam =>
            _jockerMode
                ? (_selectedTeam?.team_members_display ?? "Команда не выбрана")
                : (Team_Name.SelectedItem?.ToString() ?? "Команда не выбрана");

        public string SelectedRound =>
            _jockerMode
                ? (_selectedRound != null ? "Раунд " + _selectedRound.round_number : "Не выбран")
                : (Rounds.SelectedItem?.ToString() ?? "Не выбран");

        public event Action DataUpdated;

        private DispatcherTimer decorativeTimer;

        private readonly JockerApiService _jockerApi = new JockerApiService();
        private readonly bool _jockerMode;

        private List<Competition> _jockerCompetitions = new List<Competition>();
        private List<Discipline> _jockerDisciplines = new List<Discipline>();
        private List<Round> _jockerRounds = new List<Round>();
        private List<JockerTeam> _jockerTeams = new List<JockerTeam>();

        private Competition _selectedCompetition;
        private Discipline _selectedDiscipline;
        private Round _selectedRound;
        private JockerTeam _selectedTeam;

        private bool _firstTeamInRound = true;

        /// <param name="jockerMode">true — Рулетка (API), false — локальные файлы.</param>
        public MainWindow(bool jockerMode)
        {
            _jockerMode = jockerMode;
            InitializeComponent();
            InitializeDecorativeTimer();
            COM.ItemsSource = ports;
            sp.DataReceived += DataRecieved;

            if (_jockerMode)
            {
                Team_Name.Visibility = Visibility.Collapsed;
                Rounds.Visibility = Visibility.Collapsed;
                cboJockerCompetitions.Visibility = Visibility.Visible;
                cboJockerDisciplines.Visibility = Visibility.Visible;
                cboJockerRounds.Visibility = Visibility.Visible;
                cboJockerTeams.Visibility = Visibility.Visible;
                LoadJockerCompetitions();
            }
            else
            {
                cboJockerCompetitions.Visibility = Visibility.Collapsed;
                cboJockerDisciplines.Visibility = Visibility.Collapsed;
                cboJockerRounds.Visibility = Visibility.Collapsed;
                cboJockerTeams.Visibility = Visibility.Collapsed;
            }
        }

        private void COM_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (sp.IsOpen)
                {
                    sp.Close();
                }

                sp.PortName = COM.SelectedItem as string;
                sp.BaudRate = 250000;
                sp.Open();
                MessageBox.Show("Порт открыт");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void DataRecieved(object sender, SerialDataReceivedEventArgs e)
        {
            Dispatcher.Invoke(() => TextIn.Text = res = sp.ReadExisting());
            switch (res)
            {
                case "on":
                    stopWatch.Start();
                    StartDecorativeTimer();
                    break;
                case "off":
                    TimeSpan ts = stopWatch.Elapsed;
                    Print();
                    ts_0 = ts;
                    break;
            }
        }

        private void Yellow(object sender, RoutedEventArgs e)
        {
            sp.Write("y");
            Dispatcher.Invoke(() => TextIn.Text = ready);
            Result.Text = "00:000";
            Result_plus_Busts.Text = "00:000";
            Bust_Q.Text = "0";
            Skip_Q.Text = "0";
            bust_q = 0;
            skip_q = 0;
            off_q = 0;
            if (!_jockerMode)
            {
                Team_Name.SelectedIndex++;
            }

            if (!_firstTeamInRound)
            {
                SwitchToNextTeam();
            }

            _firstTeamInRound = false;
            Data.Choosen_TeamName = _jockerMode && _selectedTeam != null
                ? _selectedTeam.team_members_display
                : Team_Name.Text;

            OnDataUpdated();
        }

        private void White(object sender, RoutedEventArgs e)
        {
            sp.Write("w");
            Dispatcher.Invoke(() => TextIn.Text = set);
        }

        private void Bust_Click(object sender, RoutedEventArgs e)
        {
            bust_q++;
            Bust_Q.Text = bust_q.ToString();
            sp.Write("b");
            OnDataUpdated();
        }

        private void Bust_min_Click(object sender, RoutedEventArgs e)
        {
            bust_q--;
            Bust_Q.Text = bust_q.ToString();
            OnDataUpdated();
        }

        private void Skip_plus_Click(object sender, RoutedEventArgs e)
        {
            skip_q++;
            Skip_Q.Text = skip_q.ToString();
            sp.Write("b");
            OnDataUpdated();
        }

        private void Skip_min_Click(object sender, RoutedEventArgs e)
        {
            skip_q--;
            Skip_Q.Text = skip_q.ToString();
            OnDataUpdated();
        }

        private void Team_Name_Loaded(object sender, RoutedEventArgs e)
        {
            StreamReader reader = new StreamReader(path_teams);
            string x = reader.ReadToEnd();
            string[] y = x.Split('\n');
            foreach (string s in y)
            {
                Team_Name.Items.Add(s);
            }

            reader.Close();
        }

        private void Rounds_Loaded(object sender, RoutedEventArgs e)
        {
            StreamReader reader_1 = new StreamReader(path_rounds);
            string x = reader_1.ReadToEnd();
            string[] y = x.Split('\n');
            foreach (string s in y)
            {
                Rounds.Items.Add(s);
            }

            reader_1.Close();
        }

        private void Stop_Round_Click(object sender, RoutedEventArgs e)
        {
            sp.Write("f");
            stopWatch.Stop();
            string elapsedTime = string.Format("{0:00}:{1:000}", (int)ts_0.TotalSeconds, ts_0.Milliseconds);
            Dispatcher.Invoke(() => Result.Text = elapsedTime);
            stopWatch.Reset();
            StopDecorativeTimer();
            OnDataUpdated();
        }

        private void Print()
        {
            string result_each_tuch = ts_0.ToString("hh\\:mm\\:ss\\:fff");
            using (var file = new FileStream(path_each_tuch, FileMode.Append))
            using (var stream = new StreamWriter(file))
            {
                stream.WriteLine(result_each_tuch);
            }
        }

        private void Lines_ON_Click(object sender, RoutedEventArgs e)
        {
            sp.Write("g");
        }

        private void Red_Signal_Click(object sender, RoutedEventArgs e)
        {
            sp.Write("r");
        }

        private void Open_Demo(object sender, RoutedEventArgs e)
        {
            try
            {
                new DemoWindow(this).Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void OnDataUpdated()
        {
            DataUpdated?.Invoke();
        }

        private void InitializeDecorativeTimer()
        {
            decorativeTimer = new DispatcherTimer();
            decorativeTimer.Interval = TimeSpan.FromMilliseconds(30);
            decorativeTimer.Tick += (s, ev) => UpdateDecorativeDisplay();
        }

        private void StartDecorativeTimer()
        {
            decorativeTimer.Start();
        }

        private void StopDecorativeTimer()
        {
            decorativeTimer?.Stop();
        }

        private void UpdateDecorativeDisplay()
        {
            if (stopWatch.IsRunning)
            {
                TimeSpan rs = stopWatch.Elapsed;
                string elapsedTime = $"{(int)rs.TotalSeconds:00}:{rs.Milliseconds:000}";

                Result.Text = elapsedTime;
                OnDataUpdated();
            }
        }

        private async void LoadJockerCompetitions()
        {
            try
            {
                var response = await _jockerApi.GetCompetitions();

                if (response.success)
                {
                    _jockerCompetitions = response.data;
                    cboJockerCompetitions.ItemsSource = _jockerCompetitions;
                }
                else
                {
                    MessageBox.Show($"Ошибка загрузки соревнований: {response.error}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private async void CboJockerCompetitions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboJockerCompetitions.SelectedItem is Competition selected)
            {
                _selectedCompetition = selected;

                var response = await _jockerApi.GetDisciplines(selected.id);

                if (response.success)
                {
                    _jockerDisciplines = response.data;
                    cboJockerDisciplines.ItemsSource = _jockerDisciplines;
                    cboJockerDisciplines.IsEnabled = true;
                }
            }
        }

        private async void CboJockerDisciplines_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboJockerDisciplines.SelectedItem is Discipline selected && _selectedCompetition != null)
            {
                _selectedDiscipline = selected;

                var response = await _jockerApi.GetRounds(_selectedCompetition.id, selected.discipline);

                if (response.success)
                {
                    _jockerRounds = response.data;
                    cboJockerRounds.ItemsSource = _jockerRounds;
                    cboJockerRounds.IsEnabled = true;
                }
            }
        }

        private async void CboJockerRounds_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboJockerRounds.SelectedItem is Round selected &&
                _selectedCompetition != null &&
                _selectedDiscipline != null)
            {
                _selectedRound = selected;
                _firstTeamInRound = true;
                var response = await _jockerApi.GetTeams(
                    _selectedCompetition.id,
                    _selectedDiscipline.discipline,
                    selected.round_number);

                if (response.success)
                {
                    _jockerTeams = response.data;
                    cboJockerTeams.ItemsSource = _jockerTeams;
                    cboJockerTeams.IsEnabled = true;
                }
            }
        }

        private void CboJockerTeams_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboJockerTeams.SelectedItem is JockerTeam selected)
            {
                _selectedTeam = selected;
            }
        }

        private async void Result_Time_Click(object sender, RoutedEventArgs e)
        {
            if (_jockerMode)
            {
                FinalResult();
                await SaveResultToApi();
            }
            else
            {
                SaveResultToFile();
            }
        }

        private async Task LoadTeamsForCurrentRound()
        {
            if (_selectedCompetition != null && _selectedDiscipline != null && _selectedRound != null)
            {
                try
                {
                    Debug.WriteLine($"Перезагружаем команды для раунда {_selectedRound.round_number}");

                    var response = await _jockerApi.GetTeams(
                        _selectedCompetition.id,
                        _selectedDiscipline.discipline,
                        _selectedRound.round_number);

                    if (response.success && response.data != null)
                    {
                        var currentSelectionId = _selectedTeam?.id;

                        _jockerTeams = response.data
                            .OrderBy(t => t.team_number)
                            .ToList();

                        cboJockerTeams.ItemsSource = null;
                        cboJockerTeams.ItemsSource = _jockerTeams;
                        cboJockerTeams.DisplayMemberPath = "team_members_display";

                        if (currentSelectionId.HasValue)
                        {
                            var restoredSelection = _jockerTeams.FirstOrDefault(t => t.id == currentSelectionId.Value);
                            if (restoredSelection != null)
                            {
                                cboJockerTeams.SelectedItem = restoredSelection;
                            }
                        }

                        Debug.WriteLine($"Загружено {_jockerTeams.Count} команд");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Ошибка загрузки команд: {ex.Message}");
                }
            }
        }

        private async Task SaveResultToApi()
        {
            if (_selectedTeam == null)
            {
                MessageBox.Show("Выберите команду!");
                return;
            }

            try
            {
                string cleanTime = Result.Text;
                int timeMs = TimeFormatter.ConvertTimeToMilliseconds(cleanTime);

                if (timeMs <= 0)
                {
                    MessageBox.Show("Время должно быть больше нуля!");
                    return;
                }

                Debug.WriteLine($"\n=== Сохранение результата ===");
                Debug.WriteLine($"ID команды: {_selectedTeam.id}");
                Debug.WriteLine($"Время: {cleanTime} = {timeMs} мс");
                Debug.WriteLine($"Басты: {bust_q}");
                Debug.WriteLine($"Скипы: {skip_q}");

                var response = await _jockerApi.SaveTeamResult(
                    _selectedCompetition?.id ?? 0,
                    _selectedDiscipline?.discipline ?? "",
                    _selectedRound?.round_number ?? 0,
                    _selectedTeam.id,
                    timeMs,
                    bust_q,
                    skip_q);

                if (response.success)
                {
                    MessageBox.Show($"✅ Результат сохранен!\n" +
                                  $"Команда: {_selectedTeam.team_members_display}\n" +
                                  $"Время: {cleanTime}");

                    bust_q = 0;
                    skip_q = 0;
                    stopWatch.Reset();

                    OnDataUpdated();
                    await LoadTeamsForCurrentRound();
                }
                else
                {
                    MessageBox.Show($"❌ Ошибка: {response.error}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
                Debug.WriteLine($"💥 {ex}");
            }
        }

        private void SaveResultToFile()
        {
            TimeSpan ts_bust = new TimeSpan(0, 0, 0, 5, 0);
            TimeSpan ts_skip = new TimeSpan(0, 0, 0, 20, 0);
            if (bust_q != 0 && skip_q == 0)
            {
                TimeSpan ts_bust_v = TimeSpan.FromSeconds(ts_bust.Seconds * bust_q);
                TimeSpan overall = ts_bust_v.Add(ts_0);
                resultwithbusts = $"{(int)overall.TotalSeconds:00}:{overall.Milliseconds:000}";

                Result_plus_Busts.Text = resultwithbusts;
                team_name = Team_Name.Text;
                round_number = Rounds.Text;
                string Bust_q = bust_q.ToString();
                string Space = " Busts: ";
                string OverAllResult = round_number + team_name + resultwithbusts + Space + Bust_q;
                using (var file = new FileStream(path, FileMode.Append))
                using (var stream = new StreamWriter(file))
                {
                    stream.WriteLine(OverAllResult);
                }

                OnDataUpdated();
            }
            else if (bust_q == 0 && skip_q != 0)
            {
                TimeSpan ts_skip_v = TimeSpan.FromSeconds(ts_skip.Seconds * skip_q);
                TimeSpan overall = ts_skip_v.Add(ts_0);
                resultwithbusts = $"{(int)overall.TotalSeconds:00}:{overall.Milliseconds:000}";
                Result_plus_Busts.Text = resultwithbusts;
                team_name = Team_Name.Text;
                round_number = Rounds.Text;
                string Skip_q = skip_q.ToString();
                string Space = " Skip: ";
                string OverAllResult = round_number + team_name + resultwithbusts + Space + Skip_q;
                using (var file = new FileStream(path, FileMode.Append))
                using (var stream = new StreamWriter(file))
                {
                    stream.WriteLine(OverAllResult);
                }

                OnDataUpdated();
            }
            else if (bust_q != 0 && skip_q != 0)
            {
                TimeSpan ts_skip_v = TimeSpan.FromSeconds(ts_skip.Seconds * skip_q);
                TimeSpan ts_bust_v = TimeSpan.FromSeconds(ts_bust.Seconds * bust_q);
                TimeSpan preview_overall = ts_bust_v.Add(ts_0);
                TimeSpan overall = preview_overall.Add(ts_skip_v);
                resultwithbusts = $"{(int)overall.TotalSeconds:00}:{overall.Milliseconds:000}";
                Result_plus_Busts.Text = resultwithbusts;
                team_name = Team_Name.Text;
                round_number = Rounds.Text;
                string Skip_q = skip_q.ToString();
                string Space = " Skip: ";
                string Bust_q = bust_q.ToString();
                string Space_1 = " Busts: ";
                string OverAllResult = round_number + team_name + resultwithbusts + Space_1 + Bust_q + Space + Skip_q;
                using (var file = new FileStream(path, FileMode.Append))
                using (var stream = new StreamWriter(file))
                {
                    stream.WriteLine(OverAllResult);
                }

                OnDataUpdated();
            }
            else
            {
                resultwithbusts = $"{(int)ts_0.TotalSeconds:00}:{ts_0.Milliseconds:000}";
                Result_plus_Busts.Text = resultwithbusts;
                round_number = Rounds.Text;
                team_name = Team_Name.Text;
                string teamname_result = round_number + team_name + resultwithbusts;
                using (var file = new FileStream(path, FileMode.Append))
                using (var stream = new StreamWriter(file))
                {
                    stream.WriteLine(teamname_result);
                }

                OnDataUpdated();
            }
        }

        private void CloseSerialPort()
        {
            try
            {
                if (sp != null && sp.IsOpen)
                {
                    sp.DiscardInBuffer();
                    sp.DiscardOutBuffer();
                    sp.Close();
                    sp.Dispose();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при закрытии порта: {ex.Message}");
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            CloseSerialPort();
            base.OnClosing(e);
        }

        public void FinalResult()
        {
            TimeSpan ts_bust = new TimeSpan(0, 0, 0, 5, 0);
            TimeSpan ts_skip = new TimeSpan(0, 0, 0, 20, 0);
            if (bust_q != 0 && skip_q == 0)
            {
                TimeSpan ts_bust_v = TimeSpan.FromSeconds(ts_bust.Seconds * bust_q);
                TimeSpan overall = ts_bust_v.Add(ts_0);
                resultwithbusts = $"{(int)overall.TotalSeconds:00}:{overall.Milliseconds:000}";
                Result_plus_Busts.Text = resultwithbusts;
                OnDataUpdated();
            }
            else if (bust_q == 0 && skip_q != 0)
            {
                TimeSpan ts_skip_v = TimeSpan.FromSeconds(ts_skip.Seconds * skip_q);
                TimeSpan overall = ts_skip_v.Add(ts_0);
                resultwithbusts = $"{(int)overall.TotalSeconds:00}:{overall.Milliseconds:000}";
                Result_plus_Busts.Text = resultwithbusts;
                OnDataUpdated();
            }
            else if (bust_q != 0 && skip_q != 0)
            {
                TimeSpan ts_skip_v = TimeSpan.FromSeconds(ts_skip.Seconds * skip_q);
                TimeSpan ts_bust_v = TimeSpan.FromSeconds(ts_bust.Seconds * bust_q);
                TimeSpan preview_overall = ts_bust_v.Add(ts_0);
                TimeSpan overall = preview_overall.Add(ts_skip_v);
                resultwithbusts = $"{(int)overall.TotalSeconds:00}:{overall.Milliseconds:000}";
                Result_plus_Busts.Text = resultwithbusts;

                OnDataUpdated();
            }
            else
            {
                resultwithbusts = $"{(int)ts_0.TotalSeconds:00}:{ts_0.Milliseconds:000}";
                Result_plus_Busts.Text = resultwithbusts;

                OnDataUpdated();
            }
        }

        private void SwitchToNextTeam()
        {
            if (_jockerMode)
            {
                SwitchToNextJockerTeam();
            }
            else
            {
                SwitchToNextLocalTeam();
            }
        }

        private void SwitchToNextLocalTeam()
        {
            if (Team_Name.Items.Count == 0)
            {
                return;
            }

            int currentIndex = Team_Name.SelectedIndex;
            int nextIndex = currentIndex + 1;

            if (nextIndex >= Team_Name.Items.Count)
            {
                nextIndex = Team_Name.Items.Count - 1;
            }

            Team_Name.SelectedIndex = nextIndex;
        }

        private void SwitchToNextJockerTeam()
        {
            if (cboJockerTeams.Items.Count == 0)
            {
                return;
            }

            int currentIndex = cboJockerTeams.SelectedIndex;
            int nextIndex = currentIndex + 1;

            if (nextIndex >= cboJockerTeams.Items.Count)
            {
                nextIndex = cboJockerTeams.Items.Count - 1;
            }

            cboJockerTeams.SelectedIndex = nextIndex;

            if (cboJockerTeams.SelectedItem is JockerTeam selected)
            {
                _selectedTeam = selected;
                OnDataUpdated();
            }
        }
    }
}
