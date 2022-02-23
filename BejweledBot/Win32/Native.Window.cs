using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace BejweledBot.Win32
{
    internal static partial class NativeInterface
    {
        [DllImport( "gdi32.dll" )]
        internal static extern bool BitBlt( IntPtr hdcDest, int xDest, int yDest,
                                            int wDest, int hDest, IntPtr hdcSource, int xSrc, int ySrc,
                                            CopyPixelOperation rop );

        [DllImport( "user32.dll" )]
        internal static extern bool ReleaseDC( IntPtr hWnd, IntPtr hDc );

        [DllImport( "gdi32.dll" )]
        internal static extern IntPtr DeleteDC( IntPtr hDc );

        [DllImport( "gdi32.dll" )]
        internal static extern IntPtr DeleteObject( IntPtr hDc );

        [DllImport( "gdi32.dll" )]
        internal static extern IntPtr CreateCompatibleBitmap( IntPtr hdc, int nWidth, int nHeight );

        [DllImport( "gdi32.dll" )]
        internal static extern IntPtr CreateCompatibleDC( IntPtr hdc );

        [DllImport( "gdi32.dll" )]
        internal static extern IntPtr SelectObject( IntPtr hdc, IntPtr bmp );

        [DllImport( "user32.dll" )]
        internal static extern IntPtr GetDesktopWindow( );

        internal static IntPtr FindWindowByCaption( string windowName ) =>
            FindWindowByCaption( IntPtr.Zero, windowName );

        [DllImport( "user32.dll", EntryPoint = "FindWindow", SetLastError = true )]
        internal static extern IntPtr FindWindowByCaption( IntPtr ZeroOnly, string lpWindowName );

        [DllImport( "user32.dll" )]
        internal static extern IntPtr GetWindowDC( IntPtr ptr );

        [DllImport( "user32.dll", SetLastError = true )]
        internal static extern bool GetWindowRect( IntPtr hwnd, out RECT lpRect );

        [DllImport( "user32.dll" )]
        internal static extern bool GetClientRect( IntPtr hWnd, out RECT lpRect );

        [DllImport( "user32.dll", SetLastError = true )]
        internal static extern IntPtr SetFocus( IntPtr hWnd );

        [DllImport( "user32.dll", SetLastError = true )]
        internal static extern bool BringWindowToTop( IntPtr hWnd );

        [DllImport( "user32.dll", SetLastError = true )]
        [return: MarshalAs( UnmanagedType.Bool )]
        internal static extern bool SystemParametersInfo( SPI uiAction, uint uiParam, ref RECT pvParam, SPIF fWinIni );

        [DllImport( "user32.dll", SetLastError = true )]
        [return: MarshalAs( UnmanagedType.Bool )]
        internal static extern bool SystemParametersInfo( SPI uiAction, uint uiParam, IntPtr pvParam, SPIF fWinIni );

// For setting a string parameter
        [DllImport( "user32.dll", CharSet = CharSet.Auto, SetLastError = true )]
        [return: MarshalAs( UnmanagedType.Bool )]
        internal static extern bool SystemParametersInfo( uint uiAction, uint uiParam, String pvParam, SPIF fWinIni );

// For reading a string parameter
        [DllImport( "user32.dll", CharSet = CharSet.Auto, SetLastError = true )]
        [return: MarshalAs( UnmanagedType.Bool )]
        internal static extern bool SystemParametersInfo( uint uiAction, uint uiParam, StringBuilder pvParam,
                                                          SPIF fWinIni );
    }
}