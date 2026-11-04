#if GODOT_WINDOWS
using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class WindowHandler
{
    public IntPtr point;
    public uint forceStyle;
    public uint filterStyle;

    public WindowHandler(IntPtr h)
    {
        point = h;
    }
}

public static class Win32APIBridge
{
    //这个就是我们需要调的方法
    //第一个参数是窗口句柄
    //第二个参数指定要修改窗口的风格/属性的索引
    //第三个参数是设置的值
    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    // 要设置的是扩展风格（WS_EX_xxx）
    private const int GwlExStyle = -20;
    // 简单理解就是不可穿透
    private const uint WsExLayered = 0x00080000;                        // Makes the window "layered"

    private static Dictionary<int, WindowHandler> _hWnds = new Dictionary<int, WindowHandler>();
    public static void RegisterWindowHandle(Window window)
    {
        var id = window.GetWindowId();
        // We store the window handle
        var hWnd = (IntPtr)DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle, id);
        if (_hWnds.TryGetValue(id, out var existing))
        {
            existing.point = hWnd;
            return;
        }
        _hWnds[id] = new WindowHandler(hWnd);
    }

    public static void UnregisterWindowHandle(Window window)
    {
        var id = window.GetWindowId();
        if (!_hWnds.ContainsKey(id)) return;
        _hWnds.Remove(id);
    }

    public static void RegisterForceStyle(Window window, uint style)
    {
        if (_hWnds.TryGetValue(window.GetWindowId(), out var handler) == false) return;
        handler.forceStyle |= style;
    }

    public static void UnRegisterForceStyle(Window window, uint style)
    {
        if (_hWnds.TryGetValue(window.GetWindowId(), out var handler) == false) return;
        handler.forceStyle &= ~style;
    }


    public static void RegisterFilterStyle(Window window, uint style)
    {
        if (_hWnds.TryGetValue(window.GetWindowId(), out var handler) == false) return;
        handler.filterStyle |= style;
    }

    public static void UnRegisterFilterStyle(Window window, uint style)
    {
        if (_hWnds.TryGetValue(window.GetWindowId(), out var handler) == false) return;
        handler.filterStyle &= ~style;
    }



    public static void RefreshStyle(Window window)
    {
        if (!_hWnds.TryGetValue(window.GetWindowId(), out var handler) || handler.point == IntPtr.Zero) return;
        var current = unchecked((uint)GetWindowLong(handler.point, GwlExStyle));
        var desired = ((current | handler.forceStyle) & ~handler.filterStyle) | WsExLayered;
        if (desired != current)
            SetWindowLong(handler.point, GwlExStyle, desired);
    }
}
#endif
