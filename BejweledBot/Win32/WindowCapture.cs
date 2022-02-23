using System;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.Structure;

namespace BejweledBot.Win32
{
    public class WindowCapture : IDisposable
    {
        
        readonly IntPtr _windowHandle;
        readonly IntPtr _windowDC;
        readonly IntPtr _deviceDC;
        IntPtr? windowBitmap;
        int currentWidth, currentHeight;

        public static bool TryCreateWindowCapture( string windowName, out WindowCapture capture )
        {
            var ptr = Win32.NativeInterface.FindWindowByCaption( windowName );
            if( ptr == IntPtr.Zero )
            {
                capture = null;
                return false;
            }

            capture = new WindowCapture( ptr );
            return true;
        }
        
        public WindowCapture( IntPtr windowHandle )
        {
            _windowHandle = windowHandle;
            _windowDC     = Win32.NativeInterface.GetWindowDC( _windowHandle );
            _deviceDC     = Win32.NativeInterface.CreateCompatibleDC( _windowDC );
        }

        public Image<Bgr, byte> Capture( Rectangle region )
        {
            if( windowBitmap != null && (region.Width != currentWidth || region.Height != currentHeight) )
            {
                Win32.NativeInterface.DeleteObject( windowBitmap.Value );
                windowBitmap = null;
            }

            if( windowBitmap is null )
            {
                currentWidth  = region.Width;
                currentHeight = region.Height;
                windowBitmap  = Win32.NativeInterface.CreateCompatibleBitmap( _windowDC, region.Width, region.Height );
            }


            IntPtr hOldBmp = Win32.NativeInterface.SelectObject( _deviceDC, windowBitmap.Value );

            Win32.NativeInterface.BitBlt( _deviceDC, 0, 0, region.Width, region.Height, _windowDC, region.Left,
                region.Top,
                CopyPixelOperation.SourceCopy );


            using Bitmap bmp = Bitmap.FromHbitmap( windowBitmap.Value );
            var img = bmp.ToImage<Bgr, byte>( );
            
            Win32.NativeInterface.SelectObject( _deviceDC, hOldBmp );

            return img;
        } 

        public Rectangle GetWindowRect( )
        {
            RECT rect = new RECT();
            if( _windowHandle == IntPtr.Zero )
                Win32.NativeInterface.SystemParametersInfo(SPI.SPI_GETWORKAREA, 0, ref rect, 0);
            else 
                Win32.NativeInterface.GetWindowRect( _windowHandle, out rect );

            return new Rectangle( rect.X, rect.Y, rect.Width, rect.Height );
        }

        public void Dispose( )
        {
            if( windowBitmap.HasValue )
                Win32.NativeInterface.DeleteObject( windowBitmap.Value );
            
            Win32.NativeInterface.DeleteDC( _deviceDC );
            Win32.NativeInterface.ReleaseDC( _windowHandle, _windowDC );
        }
    }
}