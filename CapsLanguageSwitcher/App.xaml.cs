using System.Windows;

using Application = System.Windows.Application;

namespace CapsLanguageSwitcher
{
    public partial class App : Application
    {
        private KeyboardHook _hook;
        private string _version = "0.1.1";

        /// <summary>
        /// Метод OnStartup переопределяет поведение запуска приложения, устанавливая обработчик события нажатия клавиши Caps для переключения языка и отключения клавиши Caps Lоск
        /// </summary>
        /// <param name="e"></param>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _hook = new KeyboardHook();
            _hook.CapsLockPressed += () =>
            {
                LanguageSwitcher.SwitchLanguage();
                LanguageSwitcher.EnsureCapsOff();
            };
        }

        /// <summary>
        /// Выполняет операции очистки при выходе из приложения.
        /// </summary>
        protected override void OnExit(ExitEventArgs e)
        {
            _hook?.Unhook();
            base.OnExit(e);
        }

        /// <summary>
        /// Открывает окно
        /// </summary>
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var mw = new MainWindow(_version);
            mw.Show();
        }
    }

}
