using System;
using System.Runtime.InteropServices;

namespace BejweledBot.Win32
{
    [StructLayout( LayoutKind.Sequential )]
    internal struct MOUSEINPUT
    {
        internal int dx;
        internal int dy;
        internal int mouseData;
        /// <summary>
        /// See <see cref="MOUSEEVENTF"/>
        /// </summary>
        internal uint dwFlags;
        internal uint time;
        internal UIntPtr dwExtraInfo;
    }
}