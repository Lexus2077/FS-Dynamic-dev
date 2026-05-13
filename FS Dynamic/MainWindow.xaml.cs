using FS_Dynamic.Models;
using FS_Dynamic.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace FS_Dynamic
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        

        SerialPort sp = new SerialPort();
        string[] ports = SerialPort.GetPortNames();
        Stopwatch stopWatch = new Stopwatch();
        string res;
        int bust_q = 0;
        int skip_q = 0;
        TimeSpan ts = new TimeSpan(0, 0, 0, 0, 0);
        TimeSpan ts_0 = new TimeSpan(0, 0, 0, 0, 0);

        string resultwithbusts;
        DateTime timer_1 = new DateTime(0, 0);
        DateTime timer_2 = new DateTime(0, 0);
        
        string path = "C:\\+\\FS_Arduino\\Results.txt";
        string path_teams = "C:\\+\\FS_Arduino\\Teams.txt";
        string path_each_tuch = "C:\\+\\FS_Arduino\\Result_each_tuch.txt";
        string team_name;
        string round_number;
        string path_rounds = "C:\\+\\FS_Arduino\\Rounds.txt";
        int off_q = 0;
        string ready = "Ready";
        string set = "Set";
        TimeSpan interval = new TimeSpan(0, 0, 0, 0, 1);
        const int FlagOn = 1;
        const int FlagOff = 0;
        

        // Ниже свойства для доступа из DemoWindow
        public string TimeValue => Result.Text;
        public string FinalTimeValue => Result_plus_Busts.Text;
        public string BustValue => Bust_Q.Text;
        public string SkipValue => Skip_Q.Text;
        public string SelectedTeam => Team_Name.SelectedItem?.ToString() ?? "Команда не выбрана";
        public string SelectedRound => Rounds.SelectedItem?.ToString() ?? "Не выбран";

        public event Action DataUpdated; // Событие для уведомления об изменении данных

        private DispatcherTimer decorativeTimer;

        // Ниже поля для режима Рулетка

        // 🔴 УПРОЩЕННЫЕ ПОЛЯ ДЛЯ JOCKER API
        private JockerApiService _jockerApi = new JockerApiService();
        private bool _isJockerMode = false;

        private QualificationApiService _qualApi = new QualificationApiService();
        private bool _isQualMode = false;

        private List<QualCompetition> _qualCompetitions = new List<QualCompetition>();
        private List<QualDiscipline> _qualDisciplines = new List<QualDiscipline>();
        private List<QualRound> _qualRounds = new List<QualRound>();
        private List<QualTeam> _qualTeams = new List<QualTeam>();
        private List<QualificationResultRow> _qualAllResults = new List<QualificationResultRow>();

        private QualCompetition _selectedQualCompetition;
        private QualDiscipline _selectedQualDiscipline;
        private QualRound _selectedQualRound;
        private QualTeam _selectedQualTeam;

        // НЕ НУЖНЫ ObservableCollection - используем просто List и привязку через ItemsSource
        private List<Competition> _jockerCompetitions = new List<Competition>();
        private List<Discipline> _jockerDisciplines = new List<Discipline>();
        private List<Round> _jockerRounds = new List<Round>();
        private List<JockerTeam> _jockerTeams = new List<JockerTeam>();

        // Текущие выбранные элементы
        private Competition _selectedCompetition;
        private Discipline _selectedDiscipline;
        private Round _selectedRound;
        private JockerTeam _selectedTeam;

        private bool _firstTeamInRound = true;

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                InitializeDecorativeTimer();
                COM.ItemsSource = ports;
                sp.DataReceived += new SerialDataReceivedEventHandler(DataRecieved);
                //this.KeyDown += Window_KeyDown;
            }
            catch (Exception ex)
            { System.Diagnostics.Debug.WriteLine($"💥 ОШИБКА в MainWindow: {ex}"); }
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

        void DataRecieved(object sender, SerialDataReceivedEventArgs e) //Обработка входящих сигналов
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
                    //off_q++;
                    //if (off_q == 2)
                    //{
                    //    StopDecorativeTimer();
                                         
                       
                    //}
                    break;
            }

        }

       
        private void Yellow(object sender, RoutedEventArgs e) // Режим желтых линий
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
            if (!_isJockerMode && !_isQualMode)
            {
                Team_Name.SelectedIndex++;
            }
            if (!_firstTeamInRound)
            {
                SwitchToNextTeam();
            }
            _firstTeamInRound = false;
            if (_isQualMode && cboQualTeams.SelectedItem is QualTeam qt)
            {
                Data.Choosen_TeamName = qt.display;
            }
            else
            {
                Data.Choosen_TeamName = Team_Name.Text;
            }

        }

        private void White(object sender, RoutedEventArgs e)
        {
            sp.Write("w");
            Dispatcher.Invoke(() => TextIn.Text = set);
        }



        private void Bust_Click(object sender, RoutedEventArgs e) //Добавление баста
        {
            bust_q++;
            Bust_Q.Text = bust_q.ToString();
            sp.Write("b");
            OnDataUpdated();
        }

        private void Bust_min_Click(object sender, RoutedEventArgs e) //Снятие баста
        {
            bust_q--;
            Bust_Q.Text = bust_q.ToString();
            OnDataUpdated();
        }

        private void Skip_plus_Click(object sender, RoutedEventArgs e) //Добавление скипа
        {
            skip_q++;
            Skip_Q.Text = skip_q.ToString();
            sp.Write("b");
            OnDataUpdated();
        }

        private void Skip_min_Click(object sender, RoutedEventArgs e) //Снятие скипа
        {
            skip_q--;
            Skip_Q.Text = skip_q.ToString();
            OnDataUpdated();
        }

        

        private void Team_Name_Loaded(object sender, RoutedEventArgs e)
        {//Загрузка имен команд
            StreamReader reader = new StreamReader(path_teams);
            string x = reader.ReadToEnd();
            string[] y = x.Split('\n');
            foreach (string s in y)
            {
                Team_Name.Items.Add(s);
            }
        }

        //void Window_KeyDown(object sender, KeyEventArgs e)
        //{//Управление с клавиатуры

        //    if (e.Key == Key.Space)
        //    {
        //        Bust_Click(Bust, null);
        //        e.Handled = true;
        //    }
           
        //}

        private void Rounds_Loaded(object sender, RoutedEventArgs e) // Загрузка названий раундов
        {
            StreamReader reader_1 = new StreamReader(path_rounds);
            string x = reader_1.ReadToEnd();
            string[] y = x.Split('\n');
            foreach (string s in y)
            {
                Rounds.Items.Add(s);
            }
        }

        private void Stop_Round_Click(object sender, RoutedEventArgs e) // Кнопка окончания раунда
        {
            sp.Write("f");
            stopWatch.Stop();
            string elapsedTime = String.Format("{0:00}:{1:000}", (int)ts_0.TotalSeconds, ts_0.Milliseconds);
            Dispatcher.Invoke(() => Result.Text = elapsedTime);
            stopWatch.Reset();
            //off_q = 0;
            StopDecorativeTimer();
            OnDataUpdated();

        }

        private void Print() // Сохранение данных срабатывания датчика
        {
            string result_each_tuch = ts_0.ToString("hh\\:mm\\:ss\\:fff");
            string result_each_tuch_1 = result_each_tuch;
            FileStream file = new FileStream(path_each_tuch, FileMode.Append);
            StreamWriter stream = new StreamWriter(file);
            stream.WriteLine(result_each_tuch_1);
            stream.Close();
            file.Close();
        }


        private void Lines_ON_Click(object sender, RoutedEventArgs e) // Включние линий синяя-зеленая
        {
            sp.Write("g");
        }

        private void Red_Signal_Click(object sender, RoutedEventArgs e) // Моргание линий красным цветом
        {
            sp.Write("r");
        }

        private void Open_Demo(object sender, RoutedEventArgs e) // Открытие демонстрационного окна
        {
            try
            {

                DemoWindow demo = new DemoWindow(this);
                demo.Show();
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private void OnDataUpdated() // Метод обновления данных
        {
            DataUpdated?.Invoke();
        }

        private void InitializeDecorativeTimer() // Инициализация декоративного таймера
        {
            decorativeTimer = new DispatcherTimer();
            decorativeTimer.Interval = TimeSpan.FromMilliseconds(30);
            decorativeTimer.Tick += (s, e) => UpdateDecorativeDisplay();
        }

        private void StartDecorativeTimer() // Запуск декоративного таймера
        {
            decorativeTimer.Start();
        }

        private void StopDecorativeTimer() // Останов декоративного таймера
        { 
            decorativeTimer?.Stop();
        }

        private void UpdateDecorativeDisplay() // Вывод данных декоративного таймера в UI
        {
            if (stopWatch.IsRunning)
            {
                TimeSpan rs = stopWatch.Elapsed;
                string elapsedTime = $"{(int)rs.TotalSeconds:00}:{rs.Milliseconds:000}";

                Result.Text = elapsedTime; // Декоративное отображение
                OnDataUpdated(); // Уведомление в Demo окно
            }
        }

       private void ChkJockerMode_Checked(object sender, RoutedEventArgs e)
        {
            if (chkQualMode.IsChecked == true)
            {
                chkQualMode.IsChecked = false;
            }
            // 1. Переключаем видимость
            Team_Name.Visibility = Visibility.Collapsed;
            Rounds.Visibility = Visibility.Collapsed;
            
            cboJockerCompetitions.Visibility = Visibility.Visible;
            cboJockerDisciplines.Visibility = Visibility.Visible;
            cboJockerRounds.Visibility = Visibility.Visible;
            cboJockerTeams.Visibility = Visibility.Visible;
            lblExistingResult.Visibility = Visibility.Collapsed;

            // 2. Устанавливаем режим
            _isJockerMode = true;
            
            // 3. Загружаем соревнования
            LoadJockerCompetitions();
        }
        
        private void ChkJockerMode_Unchecked(object sender, RoutedEventArgs e)
        {
            // 1. Возвращаем локальные элементы
            if (!_isQualMode)
            {
                Team_Name.Visibility = Visibility.Visible;
                Rounds.Visibility = Visibility.Visible;
            }
            
            // 2. Скрываем API элементы
            cboJockerCompetitions.Visibility = Visibility.Collapsed;
            cboJockerDisciplines.Visibility = Visibility.Collapsed;
            cboJockerRounds.Visibility = Visibility.Collapsed;
            cboJockerTeams.Visibility = Visibility.Collapsed;
            
            // 3. Сбрасываем режим
            _isJockerMode = false;
            
            // 4. Очищаем списки
            _jockerCompetitions.Clear();
            _jockerDisciplines.Clear();
            _jockerRounds.Clear();
            _jockerTeams.Clear();
            
            cboJockerCompetitions.ItemsSource = null;
            cboJockerDisciplines.ItemsSource = null;
            cboJockerRounds.ItemsSource = null;
            cboJockerTeams.ItemsSource = null;
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
                
                // Загружаем дисциплины
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
                
                // Загружаем раунды
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
                // 🔴 Сброс флага при смене раунда в режиме Рулетка
                _firstTeamInRound = true;
                // Загружаем команды
                var response = await _jockerApi.GetTeams(
                    _selectedCompetition.id,
                    _selectedDiscipline.discipline,
                    selected.round_number
                );
                
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
                // Здесь можно обновить UI с информацией о команде
            }
        }

        private void ChkQualMode_Checked(object sender, RoutedEventArgs e)
        {
            if (chkJockerMode.IsChecked == true)
            {
                chkJockerMode.IsChecked = false;
            }

            Team_Name.Visibility = Visibility.Collapsed;
            Rounds.Visibility = Visibility.Collapsed;

            cboJockerCompetitions.Visibility = Visibility.Collapsed;
            cboJockerDisciplines.Visibility = Visibility.Collapsed;
            cboJockerRounds.Visibility = Visibility.Collapsed;
            cboJockerTeams.Visibility = Visibility.Collapsed;

            cboQualCompetitions.Visibility = Visibility.Visible;
            cboQualDisciplines.Visibility = Visibility.Visible;
            cboQualRounds.Visibility = Visibility.Visible;
            cboQualTeams.Visibility = Visibility.Visible;
            lblExistingResult.Visibility = Visibility.Visible;

            _isQualMode = true;
            LoadQualCompetitions();
            UpdateExistingResultLabel();
        }

        private void ChkQualMode_Unchecked(object sender, RoutedEventArgs e)
        {
            _isQualMode = false;

            cboQualCompetitions.Visibility = Visibility.Collapsed;
            cboQualDisciplines.Visibility = Visibility.Collapsed;
            cboQualRounds.Visibility = Visibility.Collapsed;
            cboQualTeams.Visibility = Visibility.Collapsed;
            lblExistingResult.Visibility = Visibility.Collapsed;
            lblExistingResult.Text = string.Empty;

            _qualCompetitions.Clear();
            _qualDisciplines.Clear();
            _qualRounds.Clear();
            _qualTeams.Clear();
            _qualAllResults.Clear();

            cboQualCompetitions.ItemsSource = null;
            cboQualDisciplines.ItemsSource = null;
            cboQualRounds.ItemsSource = null;
            cboQualTeams.ItemsSource = null;

            if (chkJockerMode.IsChecked != true)
            {
                Team_Name.Visibility = Visibility.Visible;
                Rounds.Visibility = Visibility.Visible;
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

        private async void CboQualCompetitions_SelectionChanged(object sender, SelectionChangedEventArgs e)
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

        private async void CboQualDisciplines_SelectionChanged(object sender, SelectionChangedEventArgs e)
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

        private void CboQualRounds_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboQualRounds.SelectedItem is QualRound selected)
            {
                _selectedQualRound = selected;
                _firstTeamInRound = true;
                EnrichTeamsWithResults();
                BindQualTeamsGrid();
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

        private void CboQualTeams_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
            if (!_isQualMode)
            {
                return;
            }

            if (_selectedQualTeam == null)
            {
                lblExistingResult.Text = "Выберите команду";
                lblExistingResult.Visibility = Visibility.Visible;
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
                lblExistingResult.Visibility = Visibility.Visible;
            }
            else
            {
                lblExistingResult.Text = "Результат ещё не введён";
                lblExistingResult.Visibility = Visibility.Visible;
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

            int timeMs = ConvertTimeToMilliseconds(Result.Text);
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
        
        // ============================================
        // МОДИФИЦИРОВАННЫЙ МЕТОД СОХРАНЕНИЯ РЕЗУЛЬТАТОВ
        // ============================================
        
        private async void Result_Time_Click(object sender, RoutedEventArgs e)
        {
            if (_isJockerMode)
            {
                FinalResult();
                // Сохраняем через API
                await SaveResultToApi();
            }
            else if (_isQualMode)
            {
                FinalResult();
                await SaveQualResultToApi();
            }
            else
            {
                // Сохраняем в файл (ваш существующий код)
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
                        _selectedRound.round_number
                    );

                    if (response.success && response.data != null)
                    {
                        // Сохраняем текущий выбор
                        var currentSelectionId = _selectedTeam?.id;

                        // 🔴 СОРТИРУЕМ команды по team_number
                        _jockerTeams = response.data
                            .OrderBy(t => t.team_number) // Сортировка по номеру команды
                            .ToList();

                        // Принудительно обновить ItemsSource
                        cboJockerTeams.ItemsSource = null;
                        cboJockerTeams.ItemsSource = _jockerTeams;
                        cboJockerTeams.DisplayMemberPath = "team_members_display";

                        // Восстанавливаем выбор, если возможно
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
                // Получаем время БЕЗ штрафов
                string cleanTime = Result.Text;
                int timeMs = ConvertTimeToMilliseconds(cleanTime);

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
                    _selectedCompetition?.id ?? 0, // competition_id не обязателен, но передадим
                    _selectedDiscipline?.discipline ?? "",
                    _selectedRound?.round_number ?? 0,
                    _selectedTeam.id,
                    timeMs,
                    bust_q,
                    skip_q
                );

                if (response.success)
                {
                    MessageBox.Show($"✅ Результат сохранен!\n" +
                                  $"Команда: {_selectedTeam.team_members_display}\n" +
                                  $"Время: {cleanTime}");

                    // Сброс состояния
                    bust_q = 0;
                    skip_q = 0;
                    //Bust_Q.Text = "0";
                    //Skip_Q.Text = "0";
                    stopWatch.Reset();
                    //Result.Text = "00:000";
                    //Result_plus_Busts.Text = "00:000";
                    //off_q = 0;

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

        private int ConvertTimeToMilliseconds(string timeText)
        {
            if (string.IsNullOrWhiteSpace(timeText) || timeText == "00:000" || timeText == "--:--")
            {
                Debug.WriteLine($"⚠️ Пустое время, возвращаем 0");
                return 0;
            }

            try
            {
                // Формат: "СС:ммм" где СС - секунды, ммм - миллисекунды
                // Пример: "65:344" = 65 секунд и 344 миллисекунды

                timeText = timeText.Trim();

                if (!timeText.Contains(":"))
                {
                    Debug.WriteLine($"❌ Некорректный формат времени: '{timeText}' (нет двоеточия)");
                    return 0;
                }

                string[] parts = timeText.Split(':');

                if (parts.Length != 2)
                {
                    Debug.WriteLine($"❌ Некорректный формат времени: '{timeText}' (не 2 части)");
                    return 0;
                }

                if (int.TryParse(parts[0], out int seconds) &&
                    int.TryParse(parts[1], out int milliseconds))
                {
                    // Проверяем диапазоны
                    if (seconds < 0 || seconds > 599) // до 10 минут
                    {
                        Debug.WriteLine($"⚠️ Секунды вне диапазона: {seconds}");
                    }

                    if (milliseconds < 0 || milliseconds > 999)
                    {
                        Debug.WriteLine($"⚠️ Миллисекунды вне диапазона: {milliseconds}");
                        milliseconds = Math.Max(0, Math.Min(999, milliseconds));
                    }

                    int totalMs = (seconds * 1000) + milliseconds;
                    Debug.WriteLine($"✅ Преобразовано '{timeText}' -> {totalMs} мс");
                    return totalMs;
                }
                else
                {
                    Debug.WriteLine($"❌ Не удалось распарсить время: '{timeText}'");
                    return 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"💥 Ошибка преобразования времени '{timeText}': {ex.Message}");
                return 0;
            }
        }

        private void SaveResultToFile()
        {
            // Финализация и сохранение результатов

            TimeSpan ts_bust = new TimeSpan(0, 0, 0, 5, 0);
            TimeSpan ts_skip = new TimeSpan(0, 0, 0, 20, 0);
            if (bust_q != 0 && skip_q == 0)
            {// Басты есть, скипов нет

                TimeSpan ts_bust_v = TimeSpan.FromSeconds(ts_bust.Seconds * (bust_q));
                TimeSpan overall = ts_bust_v.Add(ts_0);
                string time = $"{(int)ts_0.TotalSeconds:00}:{ts_0.Milliseconds:000}";
                resultwithbusts = $"{(int)overall.TotalSeconds:00}:{overall.Milliseconds:000}";

                Result_plus_Busts.Text = resultwithbusts;
                team_name = Team_Name.Text;
                round_number = Rounds.Text;
                string Bust_q = bust_q.ToString();
                string Space = " Busts: ";
                string OverAllResult = round_number + team_name + resultwithbusts + Space + Bust_q;
                FileStream file = new FileStream(path, FileMode.Append);
                StreamWriter stream = new StreamWriter(file);
                stream.WriteLine(OverAllResult);
                stream.Close();
                file.Close();

                OnDataUpdated();

            }
            else if (bust_q == 0 && skip_q != 0)
            {// Бастов нет, скипы есть

                TimeSpan ts_skip_v = TimeSpan.FromSeconds(ts_skip.Seconds * (skip_q));
                TimeSpan overall = ts_skip_v.Add(ts_0);
                string time = $"{(int)ts_0.TotalSeconds:00}:{ts_0.Milliseconds:000}";
                resultwithbusts = $"{(int)overall.TotalSeconds:00}:{overall.Milliseconds:000}";
                Result_plus_Busts.Text = resultwithbusts;
                team_name = Team_Name.Text;
                round_number = Rounds.Text;
                string Skip_q = skip_q.ToString();
                string Space = " Skip: ";
                string OverAllResult = round_number + team_name + resultwithbusts + Space + Skip_q;
                FileStream file = new FileStream(path, FileMode.Append);
                StreamWriter stream = new StreamWriter(file);
                stream.WriteLine(OverAllResult);
                stream.Close();
                file.Close();

                OnDataUpdated();
            }
            else if (bust_q != 0 && skip_q != 0)
            { //Басты и скипы есть
                TimeSpan ts_skip_v = TimeSpan.FromSeconds(ts_skip.Seconds * (skip_q));
                TimeSpan ts_bust_v = TimeSpan.FromSeconds(ts_bust.Seconds * (bust_q));
                TimeSpan preview_overall = ts_bust_v.Add(ts_0);
                TimeSpan overall = preview_overall.Add(ts_skip_v);
                string time = $"{(int)ts_0.TotalSeconds:00}:{ts_0.Milliseconds:000}";
                resultwithbusts = $"{(int)overall.TotalSeconds:00}:{overall.Milliseconds:000}";
                Result_plus_Busts.Text = resultwithbusts;
                team_name = Team_Name.Text;
                round_number = Rounds.Text;
                string Skip_q = skip_q.ToString();
                string Space = " Skip: ";
                string Bust_q = bust_q.ToString();
                string Space_1 = " Busts: ";
                string OverAllResult = round_number + team_name + resultwithbusts + Space_1 + Bust_q + Space + Skip_q;
                FileStream file = new FileStream(path, FileMode.Append);
                StreamWriter stream = new StreamWriter(file);
                stream.WriteLine(OverAllResult);
                stream.Close();
                file.Close();

                OnDataUpdated();

            }
            else
            { //Штрафы отсутствуют
                resultwithbusts = $"{(int)ts_0.TotalSeconds:00}:{ts_0.Milliseconds:000}";
                string time = $"{(int)ts_0.TotalSeconds:00}:{ts_0.Milliseconds:000}";
                Result_plus_Busts.Text = resultwithbusts;
                round_number = Rounds.Text;
                team_name = Team_Name.Text;
                string teamname_result = round_number + team_name + resultwithbusts;
                FileStream file = new FileStream(path, FileMode.Append);
                StreamWriter stream = new StreamWriter(file);
                stream.WriteLine(teamname_result);
                stream.Close();
                file.Close();

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

        private void OperatorWindow_Closed(object sender, EventArgs e)
        {
            CloseSerialPort();
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
            {// Басты есть, скипов нет

                TimeSpan ts_bust_v = TimeSpan.FromSeconds(ts_bust.Seconds * (bust_q));
                TimeSpan overall = ts_bust_v.Add(ts_0);
                string time = $"{(int)ts_0.TotalSeconds:00}:{ts_0.Milliseconds:000}";
                resultwithbusts = $"{(int)overall.TotalSeconds:00}:{overall.Milliseconds:000}";
                Result_plus_Busts.Text = resultwithbusts;
                OnDataUpdated();

            }
            else if (bust_q == 0 && skip_q != 0)
            {// Бастов нет, скипы есть

                TimeSpan ts_skip_v = TimeSpan.FromSeconds(ts_skip.Seconds * (skip_q));
                TimeSpan overall = ts_skip_v.Add(ts_0);
                string time = $"{(int)ts_0.TotalSeconds:00}:{ts_0.Milliseconds:000}";
                resultwithbusts = $"{(int)overall.TotalSeconds:00}:{overall.Milliseconds:000}";
                Result_plus_Busts.Text = resultwithbusts;
                OnDataUpdated();
            }
            else if (bust_q != 0 && skip_q != 0)
            { //Басты и скипы есть
                TimeSpan ts_skip_v = TimeSpan.FromSeconds(ts_skip.Seconds * (skip_q));
                TimeSpan ts_bust_v = TimeSpan.FromSeconds(ts_bust.Seconds * (bust_q));
                TimeSpan preview_overall = ts_bust_v.Add(ts_0);
                TimeSpan overall = preview_overall.Add(ts_skip_v);
                string time = $"{(int)ts_0.TotalSeconds:00}:{ts_0.Milliseconds:000}";
                resultwithbusts = $"{(int)overall.TotalSeconds:00}:{overall.Milliseconds:000}";
                Result_plus_Busts.Text = resultwithbusts;

                OnDataUpdated();

            }
            else
            { //Штрафы отсутствуют
                resultwithbusts = $"{(int)ts_0.TotalSeconds:00}:{ts_0.Milliseconds:000}";
                string time = $"{(int)ts_0.TotalSeconds:00}:{ts_0.Milliseconds:000}";
                Result_plus_Busts.Text = resultwithbusts;

                OnDataUpdated();
            }

        }

        private void SwitchToNextTeam()
        {
            if (_isQualMode)
            {
                SwitchToNextQualTeam();
            }
            else if (_isJockerMode)
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
            if (Team_Name.Items.Count == 0) return;

            int currentIndex = Team_Name.SelectedIndex;
            int nextIndex = currentIndex + 1;

            // Если достигли конца списка - останавливаемся на последней
            if (nextIndex >= Team_Name.Items.Count)
            {
                // Либо можно зациклить: nextIndex = 0;
                // Или остановиться: nextIndex = Team_Name.Items.Count - 1;
                nextIndex = Team_Name.Items.Count - 1; // Остановка на последней
            }

            Team_Name.SelectedIndex = nextIndex;
        }

        private void SwitchToNextJockerTeam()
        {
            if (cboJockerTeams.Items.Count == 0) return;

            int currentIndex = cboJockerTeams.SelectedIndex;
            int nextIndex = currentIndex + 1;

            if (nextIndex >= cboJockerTeams.Items.Count)
            {
                nextIndex = cboJockerTeams.Items.Count - 1; // Остановка на последней
            }

            cboJockerTeams.SelectedIndex = nextIndex;

            // Обновляем выбранную команду для режима Рулетка
            if (cboJockerTeams.SelectedItem is JockerTeam selected)
            {
                _selectedTeam = selected;
                OnDataUpdated();
            }
        }

        private void SwitchToNextQualTeam()
        {
            if (cboQualTeams.Items.Count == 0) return;

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


    }

    

    
}
