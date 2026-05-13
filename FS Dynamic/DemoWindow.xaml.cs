using System;
using System.Windows;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Threading;

namespace FS_Dynamic
{
    public partial class DemoWindow : Window
    {
        private DispatcherTimer updateTimer;
        private ITimerReadout readout;
        private bool isMaximized = false;

        public DemoWindow(ITimerReadout readout)
        {
            InitializeComponent();
            this.readout = readout;

            this.Width = 1200;
            this.Height = 600;
            this.Topmost = false;

            InitializeDemoDisplay();

            if (readout != null)
            {
                readout.DataUpdated += OnReadoutDataUpdated;
            }

            this.SizeChanged += DemoWindow_SizeChanged;
            this.StateChanged += DemoWindow_StateChanged;
        }

        private void DemoWindow_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                MaximizeButton.Content = "❐";
                isMaximized = true;
            }
            else
            {
                MaximizeButton.Content = "□";
                isMaximized = false;
            }

            UpdateFontSizes();
        }

        private void DemoWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateFontSizes();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            UpdateFontSizes();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.ButtonState == MouseButtonState.Pressed)
            {
                if (e.GetPosition(this).Y <= 35)
                {
                    this.DragMove();
                }
            }
        }

        private void UpdateFontSizes()
        {
            try
            {
                double baseWidth = 1200;
                double baseHeight = 600;

                double widthScale = this.ActualWidth / baseWidth;
                double heightScale = this.ActualHeight / baseHeight;

                double scaleFactor = Math.Min(widthScale, heightScale);

                scaleFactor = Math.Max(0.7, Math.Min(scaleFactor, 2.5));

                if (Result_Demo != null)
                {
                    Result_Demo.FontSize = 80 * scaleFactor;
                }

                if (Result_plus_Busts != null)
                {
                    Result_plus_Busts.FontSize = 96 * scaleFactor;
                }

                if (Bust_Q != null)
                {
                    Bust_Q.FontSize = 64 * scaleFactor;
                    Bust_Q.Width = 180 * scaleFactor;
                    Bust_Q.Height = 100 * scaleFactor;
                }

                if (Skip_Q != null)
                {
                    Skip_Q.FontSize = 64 * scaleFactor;
                    Skip_Q.Width = 180 * scaleFactor;
                    Skip_Q.Height = 100 * scaleFactor;
                }

                if (MainBorder != null)
                {
                    MainBorder.Margin = new Thickness(10 * scaleFactor);
                    MainBorder.CornerRadius = new CornerRadius(30 * scaleFactor);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка обновления шрифтов: {ex.Message}");
            }
        }

        private void InitializeDemoDisplay()
        {
            UpdateAllData();

            updateTimer = new DispatcherTimer();
            updateTimer.Interval = TimeSpan.FromMilliseconds(100);
            updateTimer.Tick += (s, e) => UpdateAllData();
            updateTimer.Start();
        }

        private void OnReadoutDataUpdated()
        {
            Dispatcher.BeginInvoke(new Action(UpdateAllData));
        }

        private void UpdateAllData()
        {
            try
            {
                if (readout != null)
                {
                    Result_Demo.Text = readout.TimeValue;
                    Result_plus_Busts.Text = readout.FinalTimeValue;
                    Bust_Q.Text = readout.BustValue;
                    Skip_Q.Text = readout.SkipValue;
                }
                else
                {
                    updateTimer?.Stop();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка обновления Demo окна: {ex.Message}");
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
            {
                this.WindowState = WindowState.Maximized;
                MaximizeButton.Content = "❐";
                isMaximized = true;
            }
            else
            {
                this.WindowState = WindowState.Normal;
                MaximizeButton.Content = "□";
                isMaximized = false;
            }

            UpdateFontSizes();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.F11)
            {
                if (this.WindowState == WindowState.Normal)
                {
                    this.WindowState = WindowState.Maximized;
                    this.WindowStyle = WindowStyle.None;
                    this.ResizeMode = ResizeMode.NoResize;
                    this.Topmost = true;
                    MaximizeButton.Content = "❐";
                    isMaximized = true;
                }
                else
                {
                    this.WindowState = WindowState.Normal;
                    this.WindowStyle = WindowStyle.SingleBorderWindow;
                    this.ResizeMode = ResizeMode.CanResize;
                    this.Topmost = false;
                    MaximizeButton.Content = "□";
                    isMaximized = false;
                }

                UpdateFontSizes();
            }
            else if (e.Key == Key.Escape && Keyboard.Modifiers == ModifierKeys.None)
            {
                this.Close();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (readout != null)
            {
                readout.DataUpdated -= OnReadoutDataUpdated;
            }

            this.SizeChanged -= DemoWindow_SizeChanged;
            this.StateChanged -= DemoWindow_StateChanged;
            updateTimer?.Stop();
            base.OnClosed(e);
        }
    }
}
