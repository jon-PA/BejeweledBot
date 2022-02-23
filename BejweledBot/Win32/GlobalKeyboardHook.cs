using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BejweledBot.Win32
{
    public class GlobalKeyboardHook : IDisposable
    {
        private IntPtr _hookID = IntPtr.Zero;

        Win32.NativeInterface.LowLevelKeyboardProc _delegateRef;

        public event KeyPressHandler OnKeyPress;

        public delegate void KeyPressHandler( VirtualKeyShort vk );

        public GlobalKeyboardHook( )
        {
            // Dont remove the following
            _delegateRef = new Win32.NativeInterface.LowLevelKeyboardProc( HookCallback );
            _hookID      = SetHook( _delegateRef );
        }

        internal static IntPtr SetHook( Win32.NativeInterface.LowLevelKeyboardProc proc )
        {
            using( Process curProcess = Process.GetCurrentProcess( ) )
            using( ProcessModule curModule = curProcess.MainModule )
            {
                return Win32.NativeInterface.SetWindowsHookEx( Win32.NativeInterface.WH_KEYBOARD_LL, proc,
                    Win32.NativeInterface.GetModuleHandle( curModule.ModuleName ), 0 );
            }
        }

        private IntPtr HookCallback(
            int nCode, IntPtr wParam, IntPtr lParam )
        {
            if( nCode >= 0 && wParam == (IntPtr)Win32.NativeInterface.WM_KEYDOWN )
            {
                int vkCode = Marshal.ReadInt32( lParam );
                OnKeyPress?.Invoke( (VirtualKeyShort)vkCode );
            }

            return Win32.NativeInterface.CallNextHookEx( _hookID, nCode, wParam, lParam );
        }

        public void Dispose( )
        {
            Win32.NativeInterface.UnhookWindowsHookEx( _hookID );
        }

        ~GlobalKeyboardHook( )
        {
            Dispose( );
        }
    }
}