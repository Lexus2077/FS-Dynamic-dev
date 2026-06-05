using FS_Dynamic.Models;
using FS_Dynamic.Resources;
using FS_Dynamic.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
        private string path_each_tuch = "C:\\FS_Dynamic\\Result_each_tuch.txt";
        private int off_q;
        private string ready = "Ready";
        private string set = "Set";

        private DispatcherTimer decorativeTimer;

        private readonly BracketApiService _bracketApi = new BracketApiService();
        private readonly QualificationApiService _qualApi = new QualificationApiService();

        private List<BracketCompetition> _bracketCompetitions = new List<BracketCompetition>();
        private List<QualDiscipline> _bracketDisciplines = new List<QualDiscipline>();
        private List<BracketTimerEntry> _bracketEntries = new List<BracketTimerEntry>();
        private BracketCompetition _selectedBracketCompetition;
        private string _selectedBracketDisciplineCode;
        private BracketTimerEntry _selectedBracketEntry;

        public string TimeValue => Result.Text;
        public string FinalTimeValue => Result_plus_Busts.Text;
        public string BustValue => Bust_Q.Text;
        public string SkipValue => Skip_Q.Text;
        public string SelectedTeam
        {
            get
            {
                if (_selectedBracketEntry == null)
                {
                    return "—";
                }

                if (_selectedBracketEntry.entry_kind == "placement" && _selectedBracketEntry.placement?.team != null)
                {
                    var t = _selectedBracketEntry.placement.team;
                    return "#" + t.number + " «" + t.name + "»";
                }

                if (_selectedBracketEntry.entry_kind != "match" || _selectedBracketEntry.match == null)
                {
                    return "—";
                }

                int slot = rdoBracketSlot2.IsChecked == true ? 2 : 1;
                if (slot == 2 && _selectedBracketEntry.match.team2 != null)
                {
                    return "#" + _selectedBracketEntry.match.team2.number + " «" + _selectedBracketEntry.match.team2.name + "»";
                }

                if (_selectedBracketEntry.match.team1 != null)
                {
                    return "#" + _selectedBracketEntry.match.team1.number + " «" + _selectedBracketEntry.match.team1.name + "»";
                }

                return "—";
            }
        }

        public string SelectedRound
        {
            get
            {
                if (_selectedBracketEntry == null)
                {
                    return "—";
                }

                if (_selectedBracketEntry.entry_kind == "placement" && _selectedBracketEntry.placement != null)
                {
                    return _selectedBracketEntry.placement.placement_name ?? _selectedBracketEntry.placement.placement_id;
                }

                if (_selectedBracketEntry.match != null)
                {
                    return "Матч " + _selectedBracketEntry.match.match_id;
                }

                return "—";
            }
        }

        public event Action DataUpdated;

        public BracketTimerWindow()
        {
            InitializeComponent();
            InitializeDecorativeTimer();
            COM.ItemsSource = ports;
            sp.DataReceived += DataRecieved;
            LoadBracketCompetitions();
            Shutdown.OnStateChanged += OnSystemStateChanged;
            UpdateButtonState();
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
            if (_selectedBracketEntry == null)
            {
                return;
            }

            if (_selectedBracketEntry.entry_kind == "placement" && _selectedBracketEntry.placement?.team != null)
            {
                Data.Choosen_TeamName = _selectedBracketEntry.placement.team.name;
                return;
            }

            if (_selectedBracketEntry.entry_kind != "match" || _selectedBracketEntry.match == null)
            {
                return;
            }

            int slot = rdoBracketSlot2.IsChecked == true ? 2 : 1;
            if (slot == 2 && _selectedBracketEntry.match.team2 != null)
            {
                Data.Choosen_TeamName = _selectedBracketEntry.match.team2.name;
            }
            else if (_selectedBracketEntry.match.team1 != null)
            {
                Data.Choosen_TeamName = _selectedBracketEntry.match.team1.name;
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

        private void ApplyBracketSlotRadioLabels(AvailableMatch match)
        {
            if (match == null)
            {
                rdoBracketSlot1.Content = "Ввод для команды 1";
                rdoBracketSlot2.Content = "Ввод для команды 2";
                return;
            }

            rdoBracketSlot1.Content = match.team1 != null
                ? "Ввод для команды #" + match.team1.number
                : "Ввод для команды 1";
            rdoBracketSlot2.Content = match.team2 != null
                ? "Ввод для команды #" + match.team2.number
                : "Ввод для команды 2";
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
            _bracketEntries.Clear();
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
                var matchesTask = _bracketApi.GetAvailableMatches(
                    _selectedBracketCompetition.id,
                    _selectedBracketDisciplineCode);
                var placementsTask = _bracketApi.GetAvailablePlacementEntries(
                    _selectedBracketCompetition.id,
                    _selectedBracketDisciplineCode);

                await Task.WhenAll(matchesTask, placementsTask);

                var matchesResponse = matchesTask.Result;
                var placementsResponse = placementsTask.Result;

                if (!matchesResponse.success)
                {
                    MessageBox.Show("Не удалось загрузить матчи: " + (matchesResponse.error ?? ""));
                    return;
                }
                if (!placementsResponse.success)
                {
                    MessageBox.Show("Не удалось загрузить распределение мест: " + (placementsResponse.error ?? ""));
                    return;
                }

                var entries = new List<BracketTimerEntry>();
                foreach (var match in matchesResponse.data ?? new List<AvailableMatch>())
                {
                    entries.Add(BracketTimerEntry.FromMatch(match));
                }
                foreach (var placement in placementsResponse.data ?? new List<AvailablePlacementEntry>())
                {
                    entries.Add(BracketTimerEntry.FromPlacement(placement));
                }

                _bracketEntries = entries;
                cboBracketMatches.ItemsSource = null;
                cboBracketMatches.ItemsSource = _bracketEntries;

                lblBracketNoMatches.Visibility =
                    !string.IsNullOrEmpty(_selectedBracketDisciplineCode) && _bracketEntries.Count == 0
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
            _selectedBracketEntry = cboBracketMatches.SelectedItem as BracketTimerEntry;
            if (_selectedBracketEntry == null)
            {
                ClearBracketMatchPanel();
                return;
            }

            if (_selectedBracketEntry.entry_kind == "placement")
            {
                ApplyPlacementSelection(_selectedBracketEntry.placement);
                return;
            }

            ApplyMatchSelection(_selectedBracketEntry.match);
        }

        private void ApplyPlacementSelection(AvailablePlacementEntry entry)
        {
            if (entry == null)
            {
                ClearBracketMatchPanel();
                return;
            }

            SetBracketMatchPanelVisibility(false);
            lblBracketTeam1Result.Visibility = Visibility.Visible;
            lblBracketTeam1Result.Text = string.Format(
                "Распределение мест ({0}–{1})\nКоманда: #{2} «{3}»\nРезультат ещё не введён",
                entry.min_place,
                entry.max_place,
                entry.team != null ? entry.team.number : 0,
                entry.team != null ? entry.team.name : "");
            lblBracketTeam2Result.Text = string.Empty;
            rdoBracketSlot1.IsChecked = true;
            UpdateChoosenTeamNameFromBracket();
            OnDataUpdated();
        }

        private void ApplyMatchSelection(AvailableMatch m)
        {
            if (m == null)
            {
                ClearBracketMatchPanel();
                return;
            }

            SetBracketMatchPanelVisibility(true);
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

            ApplyBracketSlotRadioLabels(m);
            UpdateChoosenTeamNameFromBracket();
            OnDataUpdated();
        }

        private void RdoBracketSlot_Checked(object sender, RoutedEventArgs e)
        {
            UpdateChoosenTeamNameFromBracket();
            OnDataUpdated();
        }

        private void ClearBracketMatchPanel()
        {
            lblBracketTeam1Result.Text = string.Empty;
            lblBracketTeam2Result.Text = string.Empty;
            rdoBracketSlot1.IsChecked = false;
            rdoBracketSlot2.IsChecked = false;
            ApplyBracketSlotRadioLabels(null);
            SetBracketMatchPanelVisibility(false);
        }

        private async Task SaveBracketResultToApi()
        {
            if (_selectedBracketEntry == null)
            {
                MessageBox.Show("Выберите матч или запись распределения мест!");
                return;
            }

            if (_selectedBracketCompetition == null || string.IsNullOrEmpty(_selectedBracketDisciplineCode))
            {
                MessageBox.Show("Выберите соревнование и дисциплину.");
                return;
            }

            int timeMs = TimeFormatter.ConvertTimeToMilliseconds(Result.Text);
            if (timeMs <= 0)
            {
                MessageBox.Show("Время должно быть > 0!");
                return;
            }

            if (_selectedBracketEntry.entry_kind == "placement")
            {
                await SavePlacementResultToApi(timeMs);
                return;
            }

            await SaveMatchResultToApi(timeMs);
        }

        private async Task SaveMatchResultToApi(int timeMs)
        {
            var match = _selectedBracketEntry?.match;
            if (match == null)
            {
                MessageBox.Show("Выберите матч!");
                return;
            }

            int slot = rdoBracketSlot2.IsChecked == true ? 2 : 1;
            string stageKey = match.kind == "final" ? match.stage_key : null;
            if (match.kind == "final" && string.IsNullOrEmpty(stageKey))
            {
                MessageBox.Show("У финального матча не указан stage_key (ошибка данных).");
                return;
            }

            var response = await _bracketApi.SaveMatchResult(
                _selectedBracketCompetition.id,
                _selectedBracketDisciplineCode,
                match.match_id,
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
                ResetTimerAfterSave();
                await LoadBracketMatches();
            }
            else
            {
                MessageBox.Show("Ошибка: " + (response.error ?? ""));
            }
        }

        private async Task SavePlacementResultToApi(int timeMs)
        {
            var placement = _selectedBracketEntry?.placement;
            if (placement == null)
            {
                MessageBox.Show("Выберите команду в распределении мест!");
                return;
            }

            var response = await _bracketApi.SavePlacementResult(
                _selectedBracketCompetition.id,
                _selectedBracketDisciplineCode,
                placement.placement_id,
                placement.team_id,
                timeMs,
                bust_q,
                skip_q);

            if (response.success && response.data != null)
            {
                var d = response.data;
                string msg = "Результат распределения мест сохранён.\n"
                    + "Время: " + Result.Text + " | Б:" + bust_q + " С:" + skip_q + "\n\n"
                    + (d.message ?? "");

                if (d.place.HasValue)
                {
                    msg += "\n\nМесто: " + d.place.Value;
                }

                MessageBox.Show(msg);
                ResetTimerAfterSave();
                await LoadBracketMatches();
            }
            else
            {
                MessageBox.Show("Ошибка: " + (response.error ?? ""));
            }
        }

        private void ResetTimerAfterSave()
        {
            bust_q = 0;
            skip_q = 0;
            stopWatch.Reset();
            OnDataUpdated();
        }

        private async void Result_Time_Click(object sender, RoutedEventArgs e)
        {
            FinalResult();
            await SaveBracketResultToApi();
        }

        public void FinalResult()
        {
            TimeSpan ts_bust;

            if (cboBracketDisciplines.Text.StartsWith("DS"))
            {
                ts_bust = new TimeSpan(0, 0, 0, 3, 0);
            }
            else
            {
                ts_bust = new TimeSpan(0, 0, 0, 5, 0);
            }

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

        private void Switch_Click(object sender, RoutedEventArgs e)
        {
            if (!Shutdown.IsOn)
            {
                sp.Write("d");
            }
            else
            {
                sp.Write("t");
            }
            Shutdown.Toggle();
            UpdateButtonState();
        }

        private void OnSystemStateChanged(bool isOn)
        {
            Dispatcher.Invoke(() => UpdateButtonState());
        }

        private void UpdateButtonState()
        {
            Button btn = this.FindName("SystemBtn") as Button;
            if (btn != null)
            {
                btn.Content = Shutdown.IsOn ? "Switch Off" : "Switch On";

                var converter = new System.Windows.Media.BrushConverter();

                if (Shutdown.IsOn)
                {
                    btn.Background = (Brush)converter.ConvertFromString("#FFCA2E26");
                }
                else
                {
                    btn.Background = (Brush)converter.ConvertFromString("#FF22D20A");
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            Shutdown.OnStateChanged -= OnSystemStateChanged;
            base.OnClosed(e);
        }
    }
}
