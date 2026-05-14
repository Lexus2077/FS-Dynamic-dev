using FS_Dynamic.Models;
using FS_Dynamic.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace FS_Dynamic
{
    public partial class BracketTimerWindow : Window, ITimerReadout
    {
        private readonly SerialPort sp = new SerialPort();
        private readonly string[] ports = SerialPort.GetPortNames();
        private readonly Stopwatch stopWatch = new Stopwatch();
        private string res;
        private int bust_q;
        private int skip_q;
        private TimeSpan ts_0 = new TimeSpan(0, 0, 0, 0, 0);
        private string path_each_tuch = "C:\\+\\FS_Arduino\\Result_each_tuch.txt";
        private int off_q;
        private string ready = "Ready";
        private string set = "Set";

        private DispatcherTimer decorativeTimer;

        private readonly BracketApiService _bracketApi = new BracketApiService();
        private readonly QualificationApiService _qualApi = new QualificationApiService();

        private List<BracketCompetition> _bracketCompetitions = new List<BracketCompetition>();
        private List<QualDiscipline> _bracketDisciplines = new List<QualDiscipline>();
        private List<AvailableMatch> _bracketMatches = new List<AvailableMatch>();
        private BracketCompetition _selectedBracketCompetition;
        private string _selectedBracketDisciplineCode;
        private AvailableMatch _selectedBracketMatch;

        public string TimeValue => Result.Text;
        public string FinalTimeValue => Result_plus_Busts.Text;
        public string BustValue => Bust_Q.Text;
        public string SkipValue => Skip_Q.Text;
        public string SelectedTeam
        {
            get
            {
                if (_selectedBracketMatch == null)
                {
                    return "—";
                }

                int slot = rdoBracketSlot2.IsChecked == true ? 2 : 1;
                if (slot == 2 && _selectedBracketMatch.team2 != null)
                {
                    return "#" + _selectedBracketMatch.team2.number + " «" + _selectedBracketMatch.team2.name + "»";
                }

                if (_selectedBracketMatch.team1 != null)
                {
                    return "#" + _selectedBracketMatch.team1.number + " «" + _selectedBracketMatch.team1.name + "»";
                }

                return "—";
            }
        }

        public string SelectedRound =>
            _selectedBracketMatch != null ? ("Матч " + _selectedBracketMatch.match_id) : "—";

        public event Action DataUpdated;

        public BracketTimerWindow()
        {
            InitializeComponent();
            InitializeDecorativeTimer();
            COM.ItemsSource = ports;
            sp.DataReceived += DataRecieved;
            LoadBracketCompetitions();
        }

        private void OnDataUpdated()
        {
            DataUpdated?.Invoke();
        }

        private void COM_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
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

        private void ChangeCompetitionMode_Click(object sender, RoutedEventArgs e)
        {
            CompetitionModeNavigator.ShowModePickerAndSwitch(this);
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
            UpdateChoosenTeamNameFromBracket();
            OnDataUpdated();
        }

        private void UpdateChoosenTeamNameFromBracket()
        {
            if (_selectedBracketMatch == null)
            {
                return;
            }

            int slot = rdoBracketSlot2.IsChecked == true ? 2 : 1;
            if (slot == 2 && _selectedBracketMatch.team2 != null)
            {
                Data.Choosen_TeamName = _selectedBracketMatch.team2.name;
            }
            else if (_selectedBracketMatch.team1 != null)
            {
                Data.Choosen_TeamName = _selectedBracketMatch.team1.name;
            }
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

        private void Stop_Round_Click(object sender, RoutedEventArgs e)
        {
            sp.Write("f");
            stopWatch.Stop();
            Dispatcher.Invoke(() => Result.Text = string.Format("{0:00}:{1:000}", (int)ts_0.TotalSeconds, ts_0.Milliseconds));
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

        private void SetBracketMatchPanelVisibility(bool visible)
        {
            var v = visible ? Visibility.Visible : Visibility.Collapsed;
            lblBracketTeam1Result.Visibility = v;
            lblBracketTeam2Result.Visibility = v;
            rdoBracketSlot1.Visibility = v;
            rdoBracketSlot2.Visibility = v;
        }

        private async void LoadBracketCompetitions()
        {
            try
            {
                var response = await _bracketApi.GetCompetitions();
                if (response.success && response.data != null)
                {
                    _bracketCompetitions = response.data;
                    cboBracketCompetitions.ItemsSource = _bracketCompetitions;
                }
                else
                {
                    MessageBox.Show("Ошибка загрузки соревнований (сетка): " + (response.error ?? ""));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }

        private async void CboBracketCompetitions_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            _bracketMatches.Clear();
            cboBracketMatches.ItemsSource = null;
            _selectedBracketDisciplineCode = null;
            lblBracketNoMatches.Visibility = Visibility.Collapsed;

            if (cboBracketCompetitions.SelectedItem is BracketCompetition selected)
            {
                _selectedBracketCompetition = selected;
                var response = await _qualApi.GetDisciplines(selected.id);
                if (response.success && response.data != null)
                {
                    _bracketDisciplines = response.data;
                    cboBracketDisciplines.ItemsSource = _bracketDisciplines;
                    cboBracketDisciplines.IsEnabled = true;
                }
            }
        }

        private async void CboBracketDisciplines_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (cboBracketDisciplines.SelectedItem is QualDiscipline d && _selectedBracketCompetition != null)
            {
                _selectedBracketDisciplineCode = d.discipline;
                await LoadBracketMatches();
            }
        }

        private async void BtnBracketRefresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadBracketMatches();
        }

        private async Task LoadBracketMatches()
        {
            if (_selectedBracketCompetition == null || string.IsNullOrEmpty(_selectedBracketDisciplineCode))
            {
                return;
            }

            try
            {
                var response = await _bracketApi.GetAvailableMatches(
                    _selectedBracketCompetition.id,
                    _selectedBracketDisciplineCode);

                if (!response.success)
                {
                    MessageBox.Show("Не удалось загрузить матчи: " + (response.error ?? ""));
                    return;
                }

                _bracketMatches = response.data ?? new List<AvailableMatch>();
                cboBracketMatches.ItemsSource = null;
                cboBracketMatches.ItemsSource = _bracketMatches;

                lblBracketNoMatches.Visibility =
                    !string.IsNullOrEmpty(_selectedBracketDisciplineCode) && _bracketMatches.Count == 0
                        ? Visibility.Visible
                        : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }

        private void CboBracketMatches_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            _selectedBracketMatch = cboBracketMatches.SelectedItem as AvailableMatch;
            if (_selectedBracketMatch == null)
            {
                ClearBracketMatchPanel();
                return;
            }

            SetBracketMatchPanelVisibility(true);
            var m = _selectedBracketMatch;
            lblBracketTeam1Result.Text = string.Format(
                "Команда 1: #{0} «{1}»\nУже: {2}",
                m.team1 != null ? m.team1.number : 0,
                m.team1 != null ? m.team1.name : "",
                m.team1_result_display);
            lblBracketTeam2Result.Text = string.Format(
                "Команда 2: #{0} «{1}»\nУже: {2}",
                m.team2 != null ? m.team2.number : 0,
                m.team2 != null ? m.team2.name : "",
                m.team2_result_display);

            if (!m.team1_has_result)
            {
                rdoBracketSlot1.IsChecked = true;
            }
            else if (!m.team2_has_result)
            {
                rdoBracketSlot2.IsChecked = true;
            }
            else
            {
                rdoBracketSlot1.IsChecked = true;
            }

            OnDataUpdated();
        }

        private void RdoBracketSlot_Checked(object sender, RoutedEventArgs e)
        {
        }

        private void ClearBracketMatchPanel()
        {
            lblBracketTeam1Result.Text = string.Empty;
            lblBracketTeam2Result.Text = string.Empty;
            rdoBracketSlot1.IsChecked = false;
            rdoBracketSlot2.IsChecked = false;
            SetBracketMatchPanelVisibility(false);
        }

        private async Task SaveBracketResultToApi()
        {
            if (_selectedBracketMatch == null)
            {
                MessageBox.Show("Выберите матч!");
                return;
            }

            if (_selectedBracketCompetition == null || string.IsNullOrEmpty(_selectedBracketDisciplineCode))
            {
                MessageBox.Show("Выберите соревнование и дисциплину.");
                return;
            }

            int slot = rdoBracketSlot2.IsChecked == true ? 2 : 1;

            int timeMs = TimeFormatter.ConvertTimeToMilliseconds(Result.Text);
            if (timeMs <= 0)
            {
                MessageBox.Show("Время должно быть > 0!");
                return;
            }

            string stageKey = _selectedBracketMatch.kind == "final" ? _selectedBracketMatch.stage_key : null;
            if (_selectedBracketMatch.kind == "final" && string.IsNullOrEmpty(stageKey))
            {
                MessageBox.Show("У финального матча не указан stage_key (ошибка данных).");
                return;
            }

            var response = await _bracketApi.SaveMatchResult(
                _selectedBracketCompetition.id,
                _selectedBracketDisciplineCode,
                _selectedBracketMatch.match_id,
                slot,
                timeMs,
                bust_q,
                skip_q,
                stageKey);

            if (response.success && response.data != null)
            {
                var d = response.data;
                string msg = "Результат команды " + slot + " сохранён.\n"
                    + "Время: " + Result.Text + " | Б:" + bust_q + " С:" + skip_q + "\n\n"
                    + (d.message ?? "");

                if (d.winner.HasValue && d.winner_team != null)
                {
                    msg += "\n\nПобедитель: #" + d.winner_team.number + " «" + d.winner_team.name + "»";
                    if (!string.IsNullOrEmpty(d.next_match_winner))
                    {
                        msg += "\nДалее (победитель): " + d.next_match_winner;
                    }

                    if (!string.IsNullOrEmpty(d.next_match_loser))
                    {
                        msg += "\nПроигравшие: " + d.next_match_loser;
                    }
                }

                MessageBox.Show(msg);
                bust_q = 0;
                skip_q = 0;
                stopWatch.Reset();
                OnDataUpdated();
                await LoadBracketMatches();
            }
            else
            {
                MessageBox.Show("Ошибка: " + (response.error ?? ""));
            }
        }

        private async void Result_Time_Click(object sender, RoutedEventArgs e)
        {
            FinalResult();
            await SaveBracketResultToApi();
        }

        public void FinalResult()
        {
            TimeSpan ts_bust = new TimeSpan(0, 0, 0, 5, 0);
            TimeSpan ts_skip = new TimeSpan(0, 0, 0, 20, 0);
            string resultwithbusts;
            if (bust_q != 0 && skip_q == 0)
            {
                TimeSpan ts_bust_v = TimeSpan.FromSeconds(ts_bust.Seconds * bust_q);
                TimeSpan overall = ts_bust_v.Add(ts_0);
                resultwithbusts = $"{(int)overall.TotalSeconds:00}:{overall.Milliseconds:000}";
                Result_plus_Busts.Text = resultwithbusts;
            }
            else if (bust_q == 0 && skip_q != 0)
            {
                TimeSpan ts_skip_v = TimeSpan.FromSeconds(ts_skip.Seconds * skip_q);
                TimeSpan overall = ts_skip_v.Add(ts_0);
                resultwithbusts = $"{(int)overall.TotalSeconds:00}:{overall.Milliseconds:000}";
                Result_plus_Busts.Text = resultwithbusts;
            }
            else if (bust_q != 0 && skip_q != 0)
            {
                TimeSpan ts_skip_v = TimeSpan.FromSeconds(ts_skip.Seconds * skip_q);
                TimeSpan ts_bust_v = TimeSpan.FromSeconds(ts_bust.Seconds * bust_q);
                TimeSpan preview_overall = ts_bust_v.Add(ts_0);
                TimeSpan overall = preview_overall.Add(ts_skip_v);
                resultwithbusts = $"{(int)overall.TotalSeconds:00}:{overall.Milliseconds:000}";
                Result_plus_Busts.Text = resultwithbusts;
            }
            else
            {
                resultwithbusts = $"{(int)ts_0.TotalSeconds:00}:{ts_0.Milliseconds:000}";
                Result_plus_Busts.Text = resultwithbusts;
            }

            OnDataUpdated();
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
                Debug.WriteLine("Ошибка при закрытии порта: " + ex.Message);
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            CloseSerialPort();
            base.OnClosing(e);
        }
    }
}
