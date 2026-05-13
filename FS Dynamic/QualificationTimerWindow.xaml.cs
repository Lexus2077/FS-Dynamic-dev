using FS_Dynamic.Models;
using FS_Dynamic.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace FS_Dynamic
{
    public partial class QualificationTimerWindow : Window, ITimerReadout
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

        private readonly QualificationApiService _qualApi = new QualificationApiService();

        private List<QualCompetition> _qualCompetitions = new List<QualCompetition>();
        private List<QualDiscipline> _qualDisciplines = new List<QualDiscipline>();
        private List<QualRound> _qualRounds = new List<QualRound>();
        private List<QualTeam> _qualTeams = new List<QualTeam>();
        private List<QualificationResultRow> _qualAllResults = new List<QualificationResultRow>();

        private QualCompetition _selectedQualCompetition;
        private QualDiscipline _selectedQualDiscipline;
        private QualRound _selectedQualRound;
        private QualTeam _selectedQualTeam;

        private bool _firstTeamInRound = true;

        public string TimeValue => Result.Text;
        public string FinalTimeValue => Result_plus_Busts.Text;
        public string BustValue => Bust_Q.Text;
        public string SkipValue => Skip_Q.Text;
        public string SelectedTeam => cboQualTeams.SelectedItem is QualTeam qt ? qt.display : "—";
        public string SelectedRound => cboQualRounds.SelectedItem is QualRound r ? ("Раунд " + r.round_number) : "—";

        public event Action DataUpdated;

        public QualificationTimerWindow()
        {
            InitializeComponent();
            InitializeDecorativeTimer();
            COM.ItemsSource = ports;
            sp.DataReceived += DataRecieved;
            LoadQualCompetitions();
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
            if (!_firstTeamInRound)
            {
                SwitchToNextQualTeam();
            }

            _firstTeamInRound = false;
            if (cboQualTeams.SelectedItem is QualTeam qt)
            {
                Data.Choosen_TeamName = qt.display;
            }

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

        private async void LoadQualCompetitions()
        {
            try
            {
                var response = await _qualApi.GetCompetitions();
                if (response.success && response.data != null)
                {
                    _qualCompetitions = response.data;
                    cboQualCompetitions.ItemsSource = _qualCompetitions;
                }
                else
                {
                    MessageBox.Show("Ошибка загрузки соревнований (квалификация): " + (response.error ?? ""));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }

        private async void CboQualCompetitions_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (cboQualCompetitions.SelectedItem is QualCompetition selected)
            {
                _selectedQualCompetition = selected;

                var response = await _qualApi.GetDisciplines(selected.id);
                if (response.success && response.data != null)
                {
                    _qualDisciplines = response.data;
                    cboQualDisciplines.ItemsSource = _qualDisciplines;
                    cboQualDisciplines.IsEnabled = true;
                }
            }
        }

        private async void CboQualDisciplines_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (cboQualDisciplines.SelectedItem is QualDiscipline selected && _selectedQualCompetition != null)
            {
                _selectedQualDiscipline = selected;

                var roundsTask = _qualApi.GetRounds(_selectedQualCompetition.id, selected.discipline);
                var teamsTask = _qualApi.GetTeams(_selectedQualCompetition.id, selected.discipline);
                var resultsTask = _qualApi.GetResults(_selectedQualCompetition.id, selected.discipline);

                await Task.WhenAll(roundsTask, teamsTask, resultsTask);

                var roundsResp = await roundsTask;
                var teamsResp = await teamsTask;
                var resultsResp = await resultsTask;

                if (roundsResp.success && roundsResp.data != null)
                {
                    _qualRounds = roundsResp.data;
                    cboQualRounds.ItemsSource = null;
                    cboQualRounds.ItemsSource = _qualRounds;
                    cboQualRounds.IsEnabled = true;
                }
                else
                {
                    _qualRounds = new List<QualRound>();
                    cboQualRounds.ItemsSource = null;
                }

                if (teamsResp.success && teamsResp.data != null)
                {
                    _qualTeams = teamsResp.data;
                }
                else
                {
                    _qualTeams = new List<QualTeam>();
                }

                if (resultsResp.success && resultsResp.data != null)
                {
                    _qualAllResults = resultsResp.data;
                }
                else
                {
                    _qualAllResults = new List<QualificationResultRow>();
                }

                _firstTeamInRound = true;
                if (_qualRounds.Count > 0)
                {
                    cboQualRounds.SelectedIndex = 0;
                }
                else
                {
                    BindQualTeamsGrid();
                    EnrichTeamsWithResults();
                }
            }
        }

        private void CboQualRounds_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (cboQualRounds.SelectedItem is QualRound selected)
            {
                _selectedQualRound = selected;
                _firstTeamInRound = true;
                EnrichTeamsWithResults();
                BindQualTeamsGrid();
                UpdateExistingResultLabel();
            }
        }

        private void BindQualTeamsGrid()
        {
            cboQualTeams.ItemsSource = null;
            cboQualTeams.ItemsSource = _qualTeams;
            cboQualTeams.DisplayMemberPath = "display";
        }

        private void EnrichTeamsWithResults()
        {
            if (_qualTeams == null)
            {
                return;
            }

            int roundNum = _selectedQualRound != null ? _selectedQualRound.round_number : 0;

            foreach (var team in _qualTeams)
            {
                team.existing_result = null;
            }

            if (roundNum <= 0 || _qualAllResults == null)
            {
                return;
            }

            foreach (var team in _qualTeams)
            {
                var row = _qualAllResults.FirstOrDefault(r =>
                    r.team_id == team.id && r.round_number == roundNum);
                if (row != null)
                {
                    team.existing_result = new QualExistingResult
                    {
                        time_ms = row.time_ms,
                        busts = row.busts,
                        skips = row.skips,
                        total_time_ms = row.total_time_ms
                    };
                }
            }
        }

        private void CboQualTeams_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (cboQualTeams.SelectedItem is QualTeam selected)
            {
                _selectedQualTeam = selected;
            }
            else
            {
                _selectedQualTeam = null;
            }

            UpdateExistingResultLabel();
        }

        private void UpdateExistingResultLabel()
        {
            if (_selectedQualTeam == null)
            {
                lblExistingResult.Text = "Выберите команду";
                return;
            }

            if (_selectedQualTeam.existing_result != null)
            {
                var r = _selectedQualTeam.existing_result;
                lblExistingResult.Text = string.Format(
                    "Уже введено: {0} | Б:{1} С:{2}",
                    r.time_display,
                    r.busts,
                    r.skips);
            }
            else
            {
                lblExistingResult.Text = "Результат ещё не введён";
            }
        }

        private async Task RefreshQualAfterSave()
        {
            if (_selectedQualCompetition == null || _selectedQualDiscipline == null)
            {
                return;
            }

            int prevRound = _selectedQualRound != null ? _selectedQualRound.round_number : 0;

            var roundsResp = await _qualApi.GetRounds(_selectedQualCompetition.id, _selectedQualDiscipline.discipline);
            var resultsResp = await _qualApi.GetResults(_selectedQualCompetition.id, _selectedQualDiscipline.discipline);

            if (roundsResp.success && roundsResp.data != null)
            {
                _qualRounds = roundsResp.data;
                cboQualRounds.ItemsSource = null;
                cboQualRounds.ItemsSource = _qualRounds;
                if (prevRound > 0)
                {
                    var match = _qualRounds.FirstOrDefault(r => r.round_number == prevRound);
                    if (match != null)
                    {
                        cboQualRounds.SelectedItem = match;
                    }
                }
            }

            if (resultsResp.success && resultsResp.data != null)
            {
                _qualAllResults = resultsResp.data;
            }

            EnrichTeamsWithResults();
            BindQualTeamsGrid();
            UpdateExistingResultLabel();
        }

        private async Task SaveQualResultToApi()
        {
            if (_selectedQualTeam == null || _selectedQualRound == null)
            {
                MessageBox.Show("Выберите команду и раунд!");
                return;
            }

            int timeMs = TimeFormatter.ConvertTimeToMilliseconds(Result.Text);
            if (timeMs <= 0)
            {
                MessageBox.Show("Время должно быть > 0!");
                return;
            }

            var response = await _qualApi.SaveResult(
                _selectedQualCompetition.id,
                _selectedQualDiscipline.discipline,
                _selectedQualTeam.id,
                _selectedQualRound.round_number,
                timeMs,
                bust_q,
                skip_q);

            if (response.success && response.data != null)
            {
                var d = response.data;
                double totalSec = d.total_time_ms.HasValue ? d.total_time_ms.Value / 1000.0 : 0;
                MessageBox.Show(
                    "Результат сохранён!\n" +
                    "Команда: " + _selectedQualTeam.display + "\n" +
                    "Время: " + Result.Text + " | Басты: " + bust_q + " | Скипы: " + skip_q + "\n" +
                    "Итого: " + totalSec.ToString("F3", CultureInfo.InvariantCulture) + " сек");

                bust_q = 0;
                skip_q = 0;
                stopWatch.Reset();
                OnDataUpdated();
                await RefreshQualAfterSave();
            }
            else
            {
                MessageBox.Show("Ошибка: " + (response.error ?? ""));
            }
        }

        private async void Result_Time_Click(object sender, RoutedEventArgs e)
        {
            FinalResult();
            await SaveQualResultToApi();
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

        private void SwitchToNextQualTeam()
        {
            if (cboQualTeams.Items.Count == 0)
            {
                return;
            }

            int currentIndex = cboQualTeams.SelectedIndex;
            int nextIndex = currentIndex + 1;

            if (nextIndex >= cboQualTeams.Items.Count)
            {
                nextIndex = cboQualTeams.Items.Count - 1;
            }

            cboQualTeams.SelectedIndex = nextIndex;

            if (cboQualTeams.SelectedItem is QualTeam selected)
            {
                _selectedQualTeam = selected;
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
