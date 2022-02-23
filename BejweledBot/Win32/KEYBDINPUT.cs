using System;
using System.Runtime.InteropServices;

namespace BejweledBot.Win32
{
    [StructLayout( LayoutKind.Sequential )]
    internal struct KEYBDINPUT
    {
        /// <summary>
        /// See <see cref="VirtualKeyShort"/>
        /// </summary>
        internal short wVk;
        /// <summary>
        /// See <see cref="ScanCodeShort"/>
        /// </summary>
        internal short wScan;
        internal KEYEVENTF dwFlags;
        internal int time;
        internal UIntPtr dwExtraInfo;
    }
}