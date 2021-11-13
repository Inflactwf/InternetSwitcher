using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;

namespace Internet_Switcher
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        UserActivityHook acthook;
        private bool ActiveRecord;

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        private Timer TimeBeforeStart;
        private Timer TimeAfterStart;
        private Timer TimeBeforeClick;
        private Timer TimeAfterClick;
        private bool HookIsEnabled;
        private bool FirstEnter = false;

        public MainWindow()
        {
            InitializeComponent();
            TimeBeforeStart = new Timer();
            TimeBeforeStart.Interval = Convert.ToInt32(TimeBeforeStartTxtBox.Text);
            TimeBeforeStart.Enabled = false;
            TimeBeforeStart.Tick += new EventHandler(TimeBeforeStart_Tick);

            TimeAfterStart = new Timer();
            TimeAfterStart.Interval = Convert.ToInt32(TimeAfterStartTxtBox.Text);
            TimeAfterStart.Enabled = false;
            TimeAfterStart.Tick += new EventHandler(TimeAfterStart_Tick);

            TimeBeforeClick = new Timer();
            TimeBeforeClick.Interval = 500;
            TimeBeforeClick.Enabled = false;
            TimeBeforeClick.Tick += new EventHandler(TimeBeforeClick_Tick);

            TimeAfterClick = new Timer();
            TimeAfterClick.Interval = 500;
            TimeAfterClick.Enabled = false;
            TimeAfterClick.Tick += new EventHandler(TimeAfterClick_Tick);

            acthook = new UserActivityHook();
            acthook.KeyDown += new KeyEventHandler(GetKey);
            acthook.Start();
            HookIsEnabled = true;
            CurrentTimeBefore.Text = TimeBeforeStartTxtBox.Text;
            CurrentTimeAfter.Text = TimeAfterStartTxtBox.Text;
        }

        public void GetKey(object sender, KeyEventArgs e)
        {
            if (ActiveRecord)
            {
                KeyLabel.Content = e.KeyData.ToString();
                ActiveRecord = false;
                ChooseButton.IsEnabled = true;
            }
            else
            {
                if (e.KeyData.ToString().Equals(KeyLabel.Content))
                {
                    TimeBeforeStart.Start();
                }
            }
        }

        private void TimeBeforeStart_Tick(object sender, EventArgs e)
        {
            Disable(AdapterNameBox.Text);
            TimeAfterStart.Start();
            TimeBeforeStart.Stop();
        }

        private void TimeAfterStart_Tick(object sender, EventArgs e)
        {
            Enable(AdapterNameBox.Text);
            TimeAfterStart.Stop();
        }

        public void Enable(string interfaceName)
        {
            ProcessStartInfo psi = new ProcessStartInfo("netsh", "interface set interface \"" + interfaceName + "\" enable");
            Process p = new Process();
            p.StartInfo = psi;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.Start();
            p.WaitForExit();
        }

        public void Disable(string interfaceName)
        {
            ProcessStartInfo psi = new ProcessStartInfo("netsh", "interface set interface \"" + interfaceName + "\" disable");
            Process p = new Process();
            p.StartInfo = psi;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.Start();
            p.WaitForExit();
        }

        private void ChooseButton_Click(object sender, RoutedEventArgs e)
        {
            ActiveRecord = true;
            ChooseButton.IsEnabled = false;
            KeyLabel.Content = "Press a button...";
        }

        private void TimeBeforeClick_Tick(object sender, EventArgs e)
        {
            BeforeButton.IsEnabled = true;
            TimeBeforeClick.Stop();
        }

        private void TimeAfterClick_Tick(object sender, EventArgs e)
        {
            AfterButton.IsEnabled = true;
            TimeAfterClick.Stop();
        }

        private void BeforeButton_Click(object sender, RoutedEventArgs e)
        {
            if (!TimeBeforeStart.Enabled)
            {
                TimeBeforeStart.Interval = Convert.ToInt32(TimeBeforeStartTxtBox.Text);
                CurrentTimeBefore.Text = TimeBeforeStart.Interval.ToString();
                BeforeButton.IsEnabled = false;
                TimeBeforeClick.Start();
            }
            else
            {
                if(System.Windows.Forms.MessageBox.Show("Timer before internet should be turned off is already in work. Do you want to break it manually?", "Internet Switcher", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == System.Windows.Forms.DialogResult.OK)
                {
                    TimeBeforeStart.Stop();
                }
            }
        }

        private void AfterButton_Click(object sender, RoutedEventArgs e)
        {
            if (!TimeAfterStart.Enabled)
            {
                TimeAfterStart.Interval = Convert.ToInt32(TimeAfterStartTxtBox.Text);
                CurrentTimeAfter.Text = TimeAfterStart.Interval.ToString();
                AfterButton.IsEnabled = false;
                TimeAfterClick.Start();
            }
            else
            {
                if (System.Windows.Forms.MessageBox.Show("Timer after internet should be turned on is already in work. Do you want to break it manually?", "Internet Switcher", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == System.Windows.Forms.DialogResult.OK)
                {
                    TimeAfterStart.Stop();
                }
            }
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (OnOffSlider.Value == 0)
            {
                if (HookIsEnabled == true)
                {
                    acthook.Stop();
                    HookIsEnabled = false;
                }

                OnOffSlider.Value = 0;
                OffBox.Foreground = Brushes.DarkRed;
                OffBox.Header = "OFF";
                ChooseButton.IsEnabled = false;
                TimeBeforeStartTxtBox.IsEnabled = false;
                TimeAfterStartTxtBox.IsEnabled = false;
                BeforeButton.IsEnabled = false;
                AfterButton.IsEnabled = false;
            }
            else if (OnOffSlider.Value < 1)
            {
                if (HookIsEnabled == false)
                {
                    acthook.Start();
                    HookIsEnabled = true;
                }
                OnOffSlider.Value = 1;
                OffBox.Foreground = Brushes.DarkGreen;
                OffBox.Header = "ON";
                ChooseButton.IsEnabled = true;
                TimeBeforeStartTxtBox.IsEnabled = true;
                TimeAfterStartTxtBox.IsEnabled = true;
                BeforeButton.IsEnabled = true;
                AfterButton.IsEnabled = true;
            }
        }

        private void SaveAdapterBtn_Click(object sender, RoutedEventArgs e)
        {
            if (FirstEnter == true)
            {
                AdapterNameBox.IsEnabled = false;
                SaveAdapterBtn.Content = "EDIT";
                FirstEnter = false;
            }
            else
            {
                AdapterNameBox.IsEnabled = true;
                SaveAdapterBtn.Content = "SAVE";
                FirstEnter = true;
            }
        }
    }
}
