#if GODOT_WINDOWS
using Godot;
using System;
using System.Runtime.InteropServices;

/// <summary>
/// window平台的穿透功能实现
/// </summary>
public class WindowsPassthroughProvider : IPassthroughProvider
{
        private const uint WsExTransparent = 0x00000020;                // Makes the window "clickable through"
                                                                        // check https://learn.microsoft.com/en-us/windows/win32/winmsg/extended-window-styles 
                                                                        // 缓存句柄

        public Window Window { get; private set; }
        public void Initialize(Window window)
        {
                Window = window;
                Win32APIBridge.RegisterWindowHandle(window);
                // We can set the properties already from here
                SetClickthrough(false);
        }

        public void Dispose()
        {
                Win32APIBridge.UnregisterWindowHandle(Window);
        }

        // 同godot提供的polygon api不同，这里只提供要么都穿透，要么都不穿透。
        public void SetClickthrough(bool clickthrough)
        {

                if (clickthrough)
                {
                        Win32APIBridge.RegisterForceStyle(Window, WsExTransparent);
                        Win32APIBridge.UnRegisterFilterStyle(Window, WsExTransparent);
                }
                else
                {
                        Win32APIBridge.UnRegisterForceStyle(Window, WsExTransparent);
                        Win32APIBridge.RegisterFilterStyle(Window, WsExTransparent);
                }
                Win32APIBridge.RefreshStyle(Window);
        }
}
#endif