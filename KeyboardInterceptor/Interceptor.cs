using System;
using System.ComponentModel;
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
        private static LowLevelKeyboardProc callbackDelegate;
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
            _hookId = SetHook();
        }

        public void Stop()
        {
            //UnhookWindowsHookEx(_hookId);
            if (callbackDelegate == null) return;
            bool ok = UnhookWindowsHookEx(_hookId);
            if (!ok) throw new Win32Exception();
            callbackDelegate = null;


            
        }        

        private static IntPtr SetHook()
        {
            using (var curProcess = Process.GetCurrentProcess())
            {
                using (var curModule = curProcess.MainModule)
                {
                    callbackDelegate = new LowLevelKeyboardProc(HookCallback);
                    return SetWindowsHookEx(WhKeyboardLl, callbackDelegate, GetModuleHandle(curModule.ModuleName), 0);
                    if (_hookId == IntPtr.Zero)
                    {
                        //Returns the error code returned by the last unmanaged function called using platform invoke that has the DllImportAttribute.SetLastError flag set. 
                        var errorCode = Marshal.GetLastWin32Error();

                        //log.Error("Unable to install keyboard hook.", new Win32Exception(errorCode));
                    }
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
