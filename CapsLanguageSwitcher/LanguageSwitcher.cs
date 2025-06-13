using System.Runtime.InteropServices;

namespace CapsLanguageSwitcher
{
    public class LanguageSwitcher
    {
        private const int WM_INPUTLANGCHANGEREQUEST = 0x0050;
        private const int INPUTLANGCHANGE_FORWARD = 0x0002;

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        private const int VK_CAPITAL = 0x14;
        private const uint KEYEVENTF_EXTENDEDKEY = 0x1;
        private const uint KEYEVENTF_KEYUP = 0x2;

        public static void SwitchLanguage()
        {
            IntPtr hwnd = GetForegroundWindow();
            PostMessage(hwnd, WM_INPUTLANGCHANGEREQUEST, (IntPtr)INPUTLANGCHANGE_FORWARD, IntPtr.Zero);
        }

        public static void EnsureCapsOff()
        {
            bool isOn = (GetKeyState(VK_CAPITAL) & 0x0001) != 0;
            if (isOn)
            {
                keybd_event(VK_CAPITAL, 0x45, KEYEVENTF_EXTENDEDKEY, UIntPtr.Zero);
                keybd_event(VK_CAPITAL, 0x45, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, UIntPtr.Zero);
            }
        }
    }
}
