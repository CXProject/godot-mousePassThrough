#if GODOT_WINDOWS
using Godot;

/// <summary>
/// window平台的穿透功能实现
/// </summary>
public class WindowsPassthroughProvider : IPassthroughProvider
{
        private const uint WsExTransparent = 0x00000020;                // Makes the window "clickable through"
                                                                        // check https://learn.microsoft.com/en-us/windows/win32/winmsg/extended-window-styles 
                                                                        // 缓存句柄

        public Window Window { get; private set; }
        private bool? _appliedClickthrough;

        public void Initialize(Window window)
        {
                if (GodotObject.IsInstanceValid(Window) && Window != window)
                        Win32APIBridge.UnregisterWindowHandle(Window);
                Window = window;
                _appliedClickthrough = null;
                Win32APIBridge.RegisterWindowHandle(window);
                // We can set the properties already from here
                SetClickthrough(false);
        }

        public void Dispose()
        {
                if (GodotObject.IsInstanceValid(Window))
                        Win32APIBridge.UnregisterWindowHandle(Window);
                Window = null;
                _appliedClickthrough = null;
        }

        // 同godot提供的polygon api不同，这里只提供要么都穿透，要么都不穿透。
        public void SetClickthrough(bool clickthrough)
        {
                if (!GodotObject.IsInstanceValid(Window)) return;
                if (_appliedClickthrough != clickthrough)
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
                        _appliedClickthrough = clickthrough;
                }

                // Godot 修改置顶等窗口标志后，可能需要恢复穿透样式。
                Win32APIBridge.RefreshStyle(Window);
        }
}
#endif
