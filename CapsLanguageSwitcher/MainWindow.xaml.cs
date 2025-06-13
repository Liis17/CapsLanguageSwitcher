using Microsoft.Win32;
using System.Diagnostics;
using System.Windows;
using Application = System.Windows.Application;

namespace CapsLanguageSwitcher
{
    public partial class MainWindow : Window
    {
        private NotifyIcon _notifyIcon;
        private bool _isExit;
        private const string AppName = "CapsLanguageSwitcher";
        public MainWindow(string v)
        {
            InitializeComponent();
            InitTray();
            LookAtMyVersion(v);
            Loaded += (s, e) =>
            {
                AutoStartCheckBox.IsChecked = IsAutoStartEnabled();
            };
        }

        /// <summary>
        /// Выводит версию приложения в окне
        /// </summary>
        /// <param name="v">Текущая версия приложения</param>
        private void LookAtMyVersion(string v)
        {
            VersionTextBlock.Text = $"β {v}";
        }

        #region Трей и контекстное меню

        /// <summary>
        /// Инициализирует иконку в трее
        /// </summary>
        private void InitTray()
        {
            _notifyIcon = new NotifyIcon();
            var uri = new Uri("pack://application:,,,/Assets/icon.ico", UriKind.Absolute);
            var streamInfo = Application.GetResourceStream(uri);

            if (streamInfo != null)
            {
                using var iconStream = streamInfo.Stream;
                _notifyIcon.Icon = new Icon(iconStream);
            }
            else
            {
                _notifyIcon.Icon = SystemIcons.Warning;
            }
            _notifyIcon.Visible = true;
            _notifyIcon.Text = "Caps -> Переключатель языка";

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Открыть", null, (_, __) => ShowWindow());
            contextMenu.Items.Add("Выход", null, (_, __) => ExitApp());

            _notifyIcon.ContextMenuStrip = contextMenu;
            _notifyIcon.DoubleClick += (_, __) => ShowWindow();

            this.StateChanged += (_, __) =>
            {
                if (this.WindowState == WindowState.Minimized)
                    this.Hide();
            };

            this.Closing += (s, e) =>
            {
                if (!_isExit)
                {
                    e.Cancel = true;
                    this.Hide();
                }
            };
        }

        /// <summary>
        /// Действие на "Открыть" в контекстном меню трея
        /// </summary>
        private void ShowWindow()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        /// <summary>
        /// Действие на "Выход" в контекстном меню трея
        /// </summary>
        private void ExitApp()
        {
            _isExit = true;
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            Application.Current.Shutdown();
        }
        #endregion

        #region Автозагрузка

        /// <summary>
        /// Активация чекбокса автозапуска
        /// </summary>
        private void AutoStartCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            SetAutoStart(true);
        }

        /// <summary>
        /// Деактивация чекбокса автозапуска
        /// </summary>
        private void AutoStartCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            SetAutoStart(false);
        }

        /// <summary>
        /// Добавляет/удаляет это приложение из автозагрузки
        /// </summary>
        /// <param name="enable">Передать нужное состояние автозагрузки</param>
        private void SetAutoStart(bool enable)
        {
            string exePath = Process.GetCurrentProcess().MainModule.FileName;
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
                       @"Software\Microsoft\Windows\CurrentVersion\Run", true))
            {
                if (enable)
                {
                    key.SetValue(AppName, $"\"{exePath}\"");
                }
                else
                {
                    key.DeleteValue(AppName, false);
                }
            }
        }

        /// <summary>
        /// Проверяет, включена ли автозагрузка для этого приложения
        /// </summary>
        /// <returns>Возвращает true, если автозагрузка включена; в противном случае false.</returns>
        private bool IsAutoStartEnabled()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
                       @"Software\Microsoft\Windows\CurrentVersion\Run", false))
            {
                string value = key?.GetValue(AppName) as string;
                string exePath = Process.GetCurrentProcess().MainModule.FileName;
                return value != null && value.Trim('"').Equals(exePath, StringComparison.OrdinalIgnoreCase);
            }
        }
        #endregion
    }
}