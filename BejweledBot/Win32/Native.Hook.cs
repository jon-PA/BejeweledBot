using System;
using System.Runtime.InteropServices;

namespace BejweledBot.Win32
{
    internal static partial class NativeInterface
    {

        internal const int WH_KEYBOARD_LL = 13;
        internal const int WM_KEYDOWN = 0x0100;
        
        internal delegate IntPtr LowLevelKeyboardProc(
            int nCode, IntPtr wParam, IntPtr lParam );

        [DllImport( "user32.dll", CharSet = CharSet.Auto, SetLastError = true )]
        internal static extern IntPtr SetWindowsHookEx( int idHook,
                                                       LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId );

        [DllImport( "user32.dll", CharSet = CharSet.Auto, SetLastError = true )]
        [return: MarshalAs( UnmanagedType.Bool )]
        internal static extern bool UnhookWindowsHookEx( IntPtr hhk );

        [DllImport( "user32.dll", CharSet = CharSet.Auto, SetLastError = true )]
        internal static extern IntPtr CallNextHookEx( IntPtr hhk, int nCode,
                                                      IntPtr wParam, IntPtr lParam );

        [DllImport( "kernel32.dll", CharSet = CharSet.Auto, SetLastError = true )]
        internal static extern IntPtr GetModuleHandle( string lpModuleName );
    }
}