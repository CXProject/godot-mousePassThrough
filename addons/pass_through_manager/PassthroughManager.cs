using Godot;
using System.Collections.Generic;

/// <summary>
///  鼠标穿透功能提供者接口
/// </summary>
public interface IPassthroughProvider
{
	public void Initialize(Window window);
	public void SetClickthrough(bool clickthrough);
}

/// <summary>
///  鼠标穿透管理
/// </summary>
public partial class PassthroughManager : Node
{
	private Dictionary<ulong, IQuadTreeItem> _clickAreas = new Dictionary<ulong, IQuadTreeItem>();
	private IPassthroughProvider _provider;
	public QuadTree QuadTree { get; private set; }
	public static PassthroughManager Instance { get; private set; }

	[Signal] public delegate void QuadTreeUpdateEventHandler();

	public override void _Ready()
	{
		Instance = this;
	}

	/// <summary>
	/// 重置或重建PassthroughManager
	/// </summary>
	/// <param name="window"></param>
	/// <param name="maxDepth"></param>
	/// <param name="maxItemCount"></param>
	public void Initialize(Window window, int maxDepth = 7, int maxItemCount = 1, bool keepExistingAreas = true)
	{
#if GODOT_WINDOWS
		_provider = new WindowsPassthroughProvider();
#else
		_provider = new DefaultPassthroughProvider();
#endif
		_provider.Initialize(window);
		_provider.SetClickthrough(true);
		QuadTree = new QuadTree(window.GetVisibleRect(), maxDepth, maxItemCount);
		if (keepExistingAreas)
		{
			foreach (var item in _clickAreas.Values)
			{
				QuadTree.Insert(item);
			}
		}
		else
		{
			_clickAreas.Clear();
		}

		_forceClickableNodes.Clear();
		_isUpdated = true;
	}

	public void RegisterPolygon2DClickArea(Polygon2D poly)
	{
		var instanceId = poly.GetInstanceId();
		if (_clickAreas.ContainsKey(instanceId)) return;
		_clickAreas[instanceId] = new Polygon2DItem(poly);
		if (QuadTree == null) return;
		QuadTree.Insert(_clickAreas[instanceId]);
		_isUpdated = true;
	}

	public void RegisterCollisionPolygon2DClickArea(CollisionPolygon2D poly)
	{
		var instanceId = poly.GetInstanceId();
		if (_clickAreas.ContainsKey(instanceId))
		{
			//刷新区域
			UpdateClickArea(poly);
			return;
		}
		_clickAreas[instanceId] = new CollisionPloygon2DItem(poly);
		if (QuadTree == null) return;
		QuadTree.Insert(_clickAreas[instanceId]);
		_isUpdated = true;
	}

	public void UpdateAllClickArea()
	{
		foreach (var item in _clickAreas.Values)
		{
			item.Update();
			if (QuadTree == null) continue;
			if (QuadTree.Update(item))
			{
				_isUpdated = true;
			}
		}
	}

	/// <summary>
	/// 更新一个鼠标可点击区域
	/// </summary>
	public void UpdateClickArea(Node2D root)
	{
		var instanceId = root.GetInstanceId();
		if (_clickAreas.ContainsKey(instanceId) == false) return;
		_clickAreas[instanceId].Update();
		if (QuadTree == null) return;
		if (QuadTree.Update(_clickAreas[instanceId]))
		{
			_isUpdated = true;
		}
	}

	public void RegisterCollisionShape2DClickArea(CollisionShape2D shape)
	{
		var instanceId = shape.GetInstanceId();
		if (_clickAreas.ContainsKey(instanceId)) return;
		_clickAreas[instanceId] = new CollisionShape2DItem(shape);
		if (QuadTree == null) return;
		QuadTree.Insert(_clickAreas[instanceId]);
		_isUpdated = true;
	}

	/// <summary>
	/// 注销一个鼠标可点击区域
	/// </summary>
	public void UnregisterClickArea(Node2D root)
	{
		var instanceId = root.GetInstanceId();
		if (_clickAreas.ContainsKey(instanceId))
		{
			var area = _clickAreas[instanceId];
			_clickAreas.Remove(instanceId);
			if (QuadTree == null) return;
			QuadTree.Remove(area);
			_isUpdated = true;
		}
	}

	public override void _Process(double delta)
	{
		if (QuadTree == null) return;
		OnQuadTreeUpdate();
		if (ForceClickable) return;
		var mousePos = GetViewport().GetMousePosition();
		var item = QuadTree.GetHitItem(mousePos);
		_provider.SetClickthrough(item == null);
	}

	private HashSet<ulong> _forceClickableNodes = new HashSet<ulong>();

	private void SetForceClickable(bool clickable, Node node)
	{
		var ins = node.GetInstanceId();
		if (clickable && _forceClickableNodes.Contains(ins)) return;
		if (!clickable && _forceClickableNodes.Contains(ins) == false) return;

		if (clickable)
		{
			_forceClickableNodes.Add(ins);
		}
		else
		{
			_forceClickableNodes.Remove(ins);
		}
		_provider.SetClickthrough(!ForceClickable);
	}

	public bool ForceClickable
	{
		get => _forceClickableNodes.Count > 0;
	}

	private bool _isUpdated = false;
	private void OnQuadTreeUpdate()
	{
		if (_isUpdated == false) return;
		EmitSignal(SignalName.QuadTreeUpdate);
		_isUpdated = false;
	}


}
