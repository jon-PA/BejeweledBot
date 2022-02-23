using System;

namespace BejweledBot.Win32
{
    [Flags]
    public enum MouseButtonAction : uint
    {
        Move = 0x0001,
        LeftDown = 0x0002,
        LeftUp = 0x0004,
        RightDown = 0x0008,
        RightUp = 0x0010,
        MiddleDown = 0x0020,
        MiddleUp = 0x0040,
        Wheel = 0x0800,
        Absolute = 0x8000,
        WheelHorizontal = 0x01000,
    }
    
    public struct MouseInput
    {
        public int X;
        public int Y;
        /// <summary>
        /// One "Line" is typically 120, backwards would be -120
        /// </summary>
        public int ScrollAmount;
        /// <summary>
        /// See <see cref="MouseButtonAction"/>
        /// </summary>
        public uint ButtonAction;
    }
    
    public struct KeyboardInput
    {
        public VirtualKeyShort KeyCode;
        public bool KeyUp;
    }
    
    public static class Wrapper
    {
        public static void SendKeyInputs( params KeyboardInput[] inputs )
        {
            var transformedInputs = new INPUT[inputs.Length];

            for( int i = 0; i < inputs.Length; i++ )
            {
                transformedInputs[i] = new INPUT( )
                {
                    type = InputType.INPUT_KEYBOARD,
                    U = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = (short)inputs[i].KeyCode,
                            wScan = (short)Win32.NativeInterface.MapVirtualKey( (uint)inputs[i].KeyCode,
                                Win32.NativeInterface.MapVirtualKeyMapTypes.MAPVK_VK_TO_VSC ),
                            dwFlags = inputs[i].KeyUp ? KEYEVENTF.KEYUP : 0
                        }
                    }
                };
            }
            Win32.NativeInterface.SendInput( (uint) transformedInputs.Length, transformedInputs, INPUT.SIZE );
        }
        public static void SendMouseInputs( params MouseInput[] inputs )
        {
            var transformedInputs = new INPUT[inputs.Length];

            for( int i = 0; i < inputs.Length; i++ )
            {
                transformedInputs[i] = new INPUT( )
                {
                    type = InputType.INPUT_MOUSE,
                    U = new InputUnion
                    {
                        mi = new MOUSEINPUT
                        {
                            dx = inputs[i].X,
                            dy = inputs[i].Y,
                            mouseData = inputs[i].ScrollAmount,
                            dwFlags = inputs[i].ButtonAction
                        }
                    }
                };
            }
            Win32.NativeInterface.SendInput( (uint) transformedInputs.Length, transformedInputs, INPUT.SIZE );
        }
    }
}