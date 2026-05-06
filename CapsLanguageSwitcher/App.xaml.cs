using System.Reflection;
using System.Windows;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace CapsLanguageSwitcher
{
    public partial class App : Application
    {
        private KeyboardHook? _hook;
        private AppSettings _settings = new();

        public AppSettings Settings => _settings;
        public KeyboardHook? Hook => _hook;

        public string AppVersion
        {
            get
            {
                var v = Assembly.GetExecutingAssembly().GetName().Version;
                return v == null ? "0.0.0" : $"{v.Major}.{v.Minor}.{v.Build}";
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            if (!SingleInstance.TryAcquire())
            {
                SingleInstance.NotifyExisting();
                Shutdown();
                return;
            }

            _settings = SettingsService.Load();

            try
            {
                LocalizationService.SetLanguage(_settings.Language);
            }
            catch
            {
                LocalizationService.SetLanguage(LocalizationService.DefaultLanguage);
            }

            base.OnStartup(e);

            _hook = new KeyboardHook();
            _hook.HookFailed += OnHookFailed;
            _hook.CapsLockPressed += OnCapsLockPressed;

            if (!_hook.Install())
            {
            }
        }

        private void OnCapsLockPressed()
        {
            var method = _settings.SwitchMethod;
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    LanguageSwitcher.Switch(method);
                    LanguageSwitcher.EnsureCapsOff();
                }
                catch
                {
                }
            });
        }

        private void OnHookFailed(Exception ex)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var title = LocalizationService.GetString("HookErrorTitle");
                var body = LocalizationService.GetString("HookErrorBody");
                MessageBox.Show($"{body}\n\n{ex.Message}", title, MessageBoxButton.OK, MessageBoxImage.Warning);
            }));
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _hook?.Unhook();
            SingleInstance.Release();
            base.OnExit(e);
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var mw = new MainWindow();
            mw.Show();
        }
    }
}
