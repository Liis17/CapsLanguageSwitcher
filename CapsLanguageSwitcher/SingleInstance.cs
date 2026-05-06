using System.Runtime.InteropServices;

namespace CapsLanguageSwitcher
{
    public static class SingleInstance
    {
        private const string MutexName = "Global\\CapsLanguageSwitcher_{8E1F4A2C-7B3D-4D1A-9E8F-2A6C1B5F4D02}";
        private const string MessageName = "WM_SHOWME_CapsLanguageSwitcher_{8E1F4A2C-7B3D-4D1A-9E8F-2A6C1B5F4D02}";

        public static readonly IntPtr HWND_BROADCAST = (IntPtr)0xFFFF;

        public static readonly uint WM_SHOWME = RegisterWindowMessage(MessageName);

        private static Mutex? _mutex;

        public static bool TryAcquire()
        {
            _mutex = new Mutex(true, MutexName, out bool createdNew);
            if (!createdNew)
            {
                _mutex.Dispose();
                _mutex = null;
                return false;
            }
            return true;
        }

        public static void Release()
        {
            if (_mutex == null) return;
            try { _mutex.ReleaseMutex(); } catch { }
            _mutex.Dispose();
            _mutex = null;
        }

        public static void NotifyExisting()
        {
            PostMessage(HWND_BROADCAST, WM_SHOWME, IntPtr.Zero, IntPtr.Zero);
        }

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern uint RegisterWindowMessage(string lpString);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
    }
}
