using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace KeyboardInterceptor
{
    public class Interceptor
    {
        private const int WhKeyboardLl = 13;
        private const int WmKeydown = 0x0100;
        private const int WmSysKeydown = 0x0104;
        private static IntPtr _hookId = IntPtr.Zero;        
        private static Action<Key> _keyResolver; 

        public Interceptor(IKeyResolver keyResolver)
        {
            _keyResolver = keyResolver.Resolve;
        }

        public Interceptor(Action<Key> keyResolver)
        {
            _keyResolver = keyResolver;
        }

        public void Start()
        {
            _hookId = SetHook(HookCallback);
        }

        public void Stop()
        {
            UnhookWindowsHookEx(_hookId);
        }        

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (var curProcess = Process.GetCurrentProcess())
            {
                using (var curModule = curProcess.MainModule)
                {
                    return SetWindowsHookEx(WhKeyboardLl, proc, GetModuleHandle(curModule.ModuleName), 0);
                }
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WmKeydown || wParam == (IntPtr)WmSysKeydown))
            {
                var key = (Key)Marshal.ReadInt32(lParam);
                _keyResolver(key);
            }
            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);        
    }
}
