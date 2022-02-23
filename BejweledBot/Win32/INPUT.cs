using System.Runtime.InteropServices;

namespace BejweledBot.Win32
{
    [StructLayout( LayoutKind.Sequential )]
    internal struct INPUT
    {
        internal InputType type;
        internal InputUnion U;

        internal static readonly int SIZE;
        static INPUT() {
            SIZE = Marshal.SizeOf( typeof( INPUT ) );
        }
    }
}