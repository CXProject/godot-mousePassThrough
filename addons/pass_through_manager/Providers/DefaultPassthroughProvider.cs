using Godot;
using System;

/// <summary>
/// 用godot api 实现穿透，适用于 mac linux
/// </summary>
public class DefaultPassthroughProvider : IPassthroughProvider
{
	private Vector2[] _emptyClickArea = { new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0) };
	public Window Window { get; private set; }
	public void Initialize(Window window)
	{
		Window = window;
	}

	public void SetClickthrough(bool clickthrough)
	{
		Window.MousePassthroughPolygon = clickthrough ? _emptyClickArea : null;
	}

}
