using Godot;
using System.Collections.Generic;

/// <summary>
///  鼠标穿透功能提供者接口
/// </summary>
public interface IPassthroughProvider
{
	public Window Window { get; }
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
	public Camera2D Camera { get; set; }

	private int _maxDepth;
	private int _maxItemCount;

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
	public void Initialize(Window window, Camera2D camera = null, int maxDepth = 7, int maxItemCount = 1, bool keepExistingAreas = true)
	{
#if GODOT_WINDOWS
		_provider = new WindowsPassthroughProvider();
#else
		_provider = new DefaultPassthroughProvider();
#endif
		_provider.Initialize(window);
		_provider.SetClickthrough(true);

		_maxDepth = maxDepth;
		_maxItemCount = maxItemCount;
		Camera = camera;
		BuildQuadTree(keepExistingAreas);
		_forceClickableNodes.Clear();
	}

	private void BuildQuadTree(bool keepExistingAreas)
	{
		QuadTree = new QuadTree(_maxDepth, _maxItemCount);
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

		_isUpdated = true;
	}

	public void SetMainCamera(Camera2D camera)
	{
		Camera = camera;
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
		if (QuadTree != null) QuadTree.ShrinkRoot();
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
			QuadTree.ShrinkRoot();
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
			QuadTree.ShrinkRoot();
			_isUpdated = true;
		}
	}

	public override void _Process(double delta)
	{
		if (QuadTree == null) return;
		OnQuadTreeUpdate();
		if (ForceClickable) return;

		var mouseScreenPos = GetViewport().GetMousePosition();
		Vector2 worldMousePos;
		Rect2 viewWorldRect;

		if (Camera != null)
		{
			var viewSize = GetViewport().GetVisibleRect().Size;
			var camCenter = Camera.GetScreenCenterPosition();
			var zoom = Camera.Zoom;

			if (Camera.AnchorMode == Camera2D.AnchorModeEnum.DragCenter)
			{
				worldMousePos = camCenter + (mouseScreenPos - viewSize / 2) / zoom;
				var worldSize = viewSize / zoom;
				viewWorldRect = new Rect2(camCenter - worldSize / 2, worldSize);
			}
			else // FixedTopLeft
			{
				worldMousePos = camCenter + mouseScreenPos / zoom;
				var worldSize = viewSize / zoom;
				viewWorldRect = new Rect2(camCenter, worldSize);
			}
		}
		else
		{
			// 无相机时使用窗口可视矩形作为世界范围
			viewWorldRect = _provider.Window.GetVisibleRect();
			worldMousePos = mouseScreenPos;
		}

		// 1. 用视口矩形从四叉树中获取候选物体集合
		var candidates = QuadTree.QueryByRect(viewWorldRect);

		// 2. 在候选集中做精确命中检测
		IQuadTreeItem hitItem = null;
		foreach (var item in candidates)
		{
			if (item.IsHit(worldMousePos))
			{
				hitItem = item;
				break;
			}
		}

		_provider.SetClickthrough(hitItem == null);
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
