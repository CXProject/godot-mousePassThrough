#if GODOT_WINDOWS
using Godot;
using System;
using System.Runtime.InteropServices;

/// <summary>
/// window平台的穿透功能实现
/// </summary>
public class WindowsPassthroughProvider : IPassthroughProvider
{
        //这个就是我们需要调的方法
        //第一个参数是窗口句柄
        //第二个参数指定要修改窗口的风格/属性的索引
        //第三个参数是设置的值
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);
        // 要设置的是扩展风格（WS_EX_xxx）
        private const int GwlExStyle = -20;
        // 简单理解就是不可穿透
        private const uint WsExLayered = 0x00080000;                        // Makes the window "layered"
                                                                            // 可穿透
        private const uint WsExTransparent = 0x00000020;                // Makes the window "clickable through"
                                                                        // check https://learn.microsoft.com/en-us/windows/win32/winmsg/extended-window-styles 
                                                                        // 缓存句柄
        private IntPtr _hWnd;
        public void Initialize(Window window)
        {
                // We store the window handle
                _hWnd = (IntPtr)DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle, window.GetWindowId());
                // We can set the properties already from here
                SetWindowLong(_hWnd, GwlExStyle, WsExLayered);
        }
        // 同godot提供的polygon api不同，这里只提供要么都穿透，要么都不穿透。
        public void SetClickthrough(bool clickthrough)
        {
                if (clickthrough)
                {
                        // We set the window as layered and click-through
                        SetWindowLong(_hWnd, GwlExStyle, WsExLayered | WsExTransparent);
                }
                else
                {
                        // We only set the window as layered, so it will be clickable
                        SetWindowLong(_hWnd, GwlExStyle, WsExLayered);
                }
        }
}
#endif