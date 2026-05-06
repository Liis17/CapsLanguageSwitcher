using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using Application = System.Windows.Application;

namespace CapsLanguageSwitcher
{
    public partial class MainWindow : Window
    {
        private NotifyIcon? _notifyIcon;
        private Icon? _trayIcon;
        private ToolStripMenuItem? _trayOpenItem;
        private ToolStripMenuItem? _trayExitItem;

        private bool _isExit;
        private bool _suppressSettingsHandlers;

        private const string AppName = "CapsLanguageSwitcher";

        public ICommand ExitAppCommand { get; }

        public MainWindow()
        {
            InitializeComponent();

            ExitAppCommand = new RelayCommand(ExitApp);

            var app = (App)Application.Current;
            VersionTextBlock.Text = $"β {app.AppVersion}";

            InitTray();

            LocalizationService.LanguageChanged += UpdateTrayLocalization;

            Loaded += OnLoaded;
            SourceInitialized += OnSourceInitialized;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var app = (App)Application.Current;

            _suppressSettingsHandlers = true;
            try
            {
                AutoStartCheckBox.IsChecked = IsAutoStartEnabled();
                SelectComboBoxByTag(MethodComboBox, app.Settings.SwitchMethod.ToString());
                SelectComboBoxByTag(LanguageComboBox, app.Settings.Language);
            }
            finally
            {
                _suppressSettingsHandlers = false;
            }
        }

        private void OnSourceInitialized(object? sender, EventArgs e)
        {
            var helper = new WindowInteropHelper(this);
            var source = HwndSource.FromHwnd(helper.Handle);
            source?.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if ((uint)msg == SingleInstance.WM_SHOWME)
            {
                ShowWindow();
                handled = true;
            }
            return IntPtr.Zero;
        }

        private static void SelectComboBoxByTag(System.Windows.Controls.ComboBox box, string tag)
        {
            foreach (var item in box.Items)
            {
                if (item is ComboBoxItem cbi && string.Equals(cbi.Tag?.ToString(), tag, StringComparison.OrdinalIgnoreCase))
                {
                    box.SelectedItem = cbi;
                    return;
                }
            }
            if (box.Items.Count > 0) box.SelectedIndex = 0;
        }

        #region Трей и контекстное меню

        private void InitTray()
        {
            _notifyIcon = new NotifyIcon();
            var uri = new Uri("pack://application:,,,/Assets/icon.ico", UriKind.Absolute);
            var streamInfo = Application.GetResourceStream(uri);

            if (streamInfo != null)
            {
                using var iconStream = streamInfo.Stream;
                _trayIcon = new Icon(iconStream);
                _notifyIcon.Icon = _trayIcon;
            }
            else
            {
                _notifyIcon.Icon = SystemIcons.Warning;
            }
            _notifyIcon.Visible = true;

            var contextMenu = new ContextMenuStrip();
            _trayOpenItem = new ToolStripMenuItem();
            _trayOpenItem.Click += (_, __) => ShowWindow();
            _trayExitItem = new ToolStripMenuItem();
            _trayExitItem.Click += (_, __) => ExitApp();
            contextMenu.Items.Add(_trayOpenItem);
            contextMenu.Items.Add(_trayExitItem);

            _notifyIcon.ContextMenuStrip = contextMenu;
            _notifyIcon.DoubleClick += (_, __) => ShowWindow();

            UpdateTrayLocalization();

            this.StateChanged += (_, __) =>
            {
                if (this.WindowState == WindowState.Minimized)
                    this.Hide();
            };

            this.Closing += (s, e) =>
            {
                if (_isExit) return;

                bool shiftHeld = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
                if (shiftHeld)
                {
                    ExitApp();
                    return;
                }

                e.Cancel = true;
                this.Hide();
            };
        }

        private void UpdateTrayLocalization()
        {
            if (_notifyIcon == null) return;
            _notifyIcon.Text = LocalizationService.GetString("TrayTooltip");
            if (_trayOpenItem != null) _trayOpenItem.Text = LocalizationService.GetString("TrayOpen");
            if (_trayExitItem != null) _trayExitItem.Text = LocalizationService.GetString("TrayExit");
        }

        private void ShowWindow()
        {
            this.Show();
            if (this.WindowState == WindowState.Minimized)
                this.WindowState = WindowState.Normal;
            this.Activate();
            this.Topmost = true;
            this.Topmost = false;
            this.Focus();
        }

        private void ExitApp()
        {
            _isExit = true;
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.ContextMenuStrip?.Dispose();
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }
            _trayIcon?.Dispose();
            _trayIcon = null;
            LocalizationService.LanguageChanged -= UpdateTrayLocalization;
            Application.Current.Shutdown();
        }
        #endregion

        #region Настройки

        private void MethodComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressSettingsHandlers) return;
            if (MethodComboBox.SelectedItem is not ComboBoxItem cbi) return;

            var tag = cbi.Tag?.ToString();
            if (Enum.TryParse<SwitchMethod>(tag, out var method))
            {
                var app = (App)Application.Current;
                app.Settings.SwitchMethod = method;
                SettingsService.Save(app.Settings);
            }
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressSettingsHandlers) return;
            if (LanguageComboBox.SelectedItem is not ComboBoxItem cbi) return;

            var tag = cbi.Tag?.ToString();
            if (string.IsNullOrEmpty(tag)) return;

            try
            {
                LocalizationService.SetLanguage(tag);
            }
            catch
            {
                return;
            }

            var app = (App)Application.Current;
            app.Settings.Language = tag;
            SettingsService.Save(app.Settings);
        }
        #endregion

        #region Автозагрузка

        private void AutoStartCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (_suppressSettingsHandlers) return;
            SetAutoStart(true);
        }

        private void AutoStartCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_suppressSettingsHandlers) return;
            SetAutoStart(false);
        }

        private static string GetExePath() => Environment.ProcessPath ?? AppContext.BaseDirectory;

        private void SetAutoStart(bool enable)
        {
            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Run", true);
                if (key == null) return;

                if (enable)
                {
                    key.SetValue(AppName, $"\"{GetExePath()}\"");
                }
                else
                {
                    key.DeleteValue(AppName, false);
                }
            }
            catch
            {
            }
        }

        private bool IsAutoStartEnabled()
        {
            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Run", false);
                if (key == null) return false;

                string? value = key.GetValue(AppName) as string;
                if (string.IsNullOrEmpty(value)) return false;

                string exePath = GetExePath();
                return value.Trim('"').Equals(exePath, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
        #endregion

        private sealed class RelayCommand : ICommand
        {
            private readonly Action _execute;
            public RelayCommand(Action execute) => _execute = execute;
            public event EventHandler? CanExecuteChanged { add { } remove { } }
            public bool CanExecute(object? parameter) => true;
            public void Execute(object? parameter) => _execute();
        }
    }
}
