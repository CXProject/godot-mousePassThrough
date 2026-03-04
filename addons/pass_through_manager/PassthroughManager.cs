using Godot;
using System;
using System.Collections.Generic;

/// <summary>
///  鼠标穿透功能提供者接口
/// </summary>
public interface IPassthroughProvider
{
	public void Initialize(Window window);
	public void SetClickthrough(bool clickthrough);
}

public class PolygonItem : IQuadTreeItem
{
	public QuadTreeNode CurrentNode { get; set; }

	public Rect2 Bounds { get; private set; }

	public ulong ItemID { get; private set; }

	private Vector2[] _polygon;
	public bool IsHit(Vector2 pos)
	{
		return Geometry2D.IsPointInPolygon(pos, _polygon);
	}

	public PolygonItem(ulong itemId, Vector2[] polygon)
	{
		ItemID = itemId;
		SetPolygon(polygon);
	}

	public void SetPolygon(Vector2[] polygon)
	{
		_polygon = polygon;
		Bounds = GetPolygonAABB(polygon);
	}

	private Rect2 GetPolygonAABB(Vector2[] poly)
	{
		if (poly.Length == 0)
			return new Rect2();

		var minPos = poly[0];
		var maxPos = poly[0];

		foreach (var p in poly)
		{
			var point = p;
			minPos.X = Math.Min(minPos.X, point.X);
			minPos.Y = Math.Min(minPos.Y, point.Y);
			maxPos.X = Math.Max(maxPos.X, point.X);
			maxPos.Y = Math.Max(maxPos.Y, point.Y);
		}

		return new Rect2(minPos, maxPos - minPos);
	}

}

/// <summary>
///  鼠标穿透管理
/// </summary>
public partial class PassthroughManager : Node
{
	private Dictionary<ulong, PolygonItem> _clickAreas = new Dictionary<ulong, PolygonItem>();
	private IPassthroughProvider _provider;
	public QuadTree QuadTree { get; private set; }
	public static PassthroughManager Instance { get; private set; }

	[Signal] public delegate void QuadTreeUpdateEventHandler();

	public override void _Ready()
	{
		Instance = this;
#if GODOT_WINDOWS
		_provider = new WindowsPassthroughProvider();
#else
		_provider = new DefaultPassthroughProvider();
#endif
		var win = GetWindow();
		_provider.Initialize(win);
		_provider.SetClickthrough(true);
		QuadTree = new QuadTree(win.GetVisibleRect(), 7, 1);
	}

	public void RegisterPolygon2DClickArea(Polygon2D poly)
	{
		var instanceId = poly.GetInstanceId();
		var polygon = poly.GetPolygon();
		for (var i = 0; i < polygon.Length; i++)
		{
			polygon[i] = poly.ToGlobal(polygon[i]);
		}
		RegisterClickArea(instanceId, polygon);
	}

	public void RegisterCollisionPolygon2DClickArea(CollisionPolygon2D poly)
	{
		var instanceId = poly.GetInstanceId();
		var polygon = poly.GetPolygon();
		for (var i = 0; i < polygon.Length; i++)
		{
			polygon[i] = poly.ToGlobal(polygon[i]);
		}
		RegisterClickArea(instanceId, polygon);
	}

	public void UpdatePolygon2DClickArea(Polygon2D poly)
	{
		var instanceId = poly.GetInstanceId();
		var polygon = poly.GetPolygon();
		for (var i = 0; i < polygon.Length; i++)
		{
			polygon[i] = poly.ToGlobal(polygon[i]);
		}
		UpdateClickArea(instanceId, polygon);
	}

	public void UpdateCollisionPolygon2DClickArea(CollisionPolygon2D poly)
	{
		var instanceId = poly.GetInstanceId();
		var polygon = poly.GetPolygon();
		for (var i = 0; i < polygon.Length; i++)
		{
			polygon[i] = poly.ToGlobal(polygon[i]);
		}
		UpdateClickArea(instanceId, polygon);
	}

	public void UnregisterPolygon2DClickArea(Polygon2D poly)
	{
		UnregisterClickArea(poly.GetInstanceId());
	}

	public void UnregisterCollisionPolygon2DClickArea(CollisionPolygon2D poly)
	{
		UnregisterClickArea(poly.GetInstanceId());
	}

	/// <summary>
	/// 注册一个鼠标可点击区域
	/// polygon 一定要是世界坐标下的
	/// </summary>
	/// <param name="instanceId"></param>
	/// <param name="polygon"></param>
	public void RegisterClickArea(ulong instanceId, Vector2[] polygon)
	{
		if (_clickAreas.ContainsKey(instanceId)) return;
		_clickAreas[instanceId] = new PolygonItem(instanceId, polygon);
		QuadTree.Insert(_clickAreas[instanceId]);
		_isUpdated = true;
	}

	/// <summary>
	/// 更新一个鼠标可点击区域
	/// </summary>
	/// <param name="instanceId"></param>
	/// <param name="polygon"></param>
	public void UpdateClickArea(ulong instanceId, Vector2[] polygon)
	{
		if (_clickAreas.ContainsKey(instanceId) == false) return;
		_clickAreas[instanceId].SetPolygon(polygon);

		if (QuadTree.Update(_clickAreas[instanceId]))
		{
			_isUpdated = true;
		}
	}

	/// <summary>
	/// 注销一个鼠标可点击区域
	/// </summary>
	/// <param name="instanceId"></param>
	public void UnregisterClickArea(ulong instanceId)
	{
		if (_clickAreas.ContainsKey(instanceId))
		{
			QuadTree.Remove(_clickAreas[instanceId]);
			_clickAreas.Remove(instanceId);
			_isUpdated = true;
		}
	}

	public override void _Process(double delta)
	{
		OnQuadTreeUpdate();
		if (ForceClickable) return;
		var mousePos = GetViewport().GetMousePosition();
		var item = QuadTree.GetHitItem(mousePos);
		_provider.SetClickthrough(item == null);
	}

	private bool _forceClickable = false;
	public bool ForceClickable
	{
		get => _forceClickable;
		set
		{
			if (_forceClickable == value) return;
			_forceClickable = value;
			_provider.SetClickthrough(!_forceClickable);
		}
	}

	private bool _isUpdated = false;
	private void OnQuadTreeUpdate()
	{
		if (_isUpdated == false) return;
		EmitSignal(SignalName.QuadTreeUpdate);
		_isUpdated = false;
	}


}
