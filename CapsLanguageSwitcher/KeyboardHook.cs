using System.ComponentModel;
using System.Runtime.InteropServices;

namespace CapsLanguageSwitcher
{
    public class KeyboardHook
    {
        public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private readonly LowLevelKeyboardProc _proc;
        private IntPtr _hookID = IntPtr.Zero;

        public event Action? CapsLockPressed;
        public event Action<Exception>? HookFailed;

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int VK_CAPITAL = 0x14;

        public bool IsInstalled => _hookID != IntPtr.Zero;

        public KeyboardHook()
        {
            _proc = HookCallback;
        }

        public bool Install()
        {
            if (_hookID != IntPtr.Zero) return true;

            try
            {
                IntPtr moduleHandle = GetModuleHandle(null);
                _hookID = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, moduleHandle, 0);

                if (_hookID == IntPtr.Zero)
                {
                    int err = Marshal.GetLastWin32Error();
                    HookFailed?.Invoke(new Win32Exception(err, $"SetWindowsHookEx failed (code {err})."));
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                HookFailed?.Invoke(ex);
                _hookID = IntPtr.Zero;
                return false;
            }
        }

        public void Unhook()
        {
            if (_hookID == IntPtr.Zero) return;
            try { UnhookWindowsHookEx(_hookID); } catch { }
            _hookID = IntPtr.Zero;
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                if (vkCode == VK_CAPITAL)
                {
                    try { CapsLockPressed?.Invoke(); } catch { }
                    return (IntPtr)1;
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr GetModuleHandle(string? lpModuleName);
    }
}
