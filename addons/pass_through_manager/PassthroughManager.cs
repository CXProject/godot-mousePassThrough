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
	private List<IQuadTreeItem> _screenSpaceItems = new List<IQuadTreeItem>();
	private IPassthroughProvider _provider;
	public QuadTree QuadTree { get; private set; }
	public IReadOnlyList<IQuadTreeItem> ScreenSpaceItems => _screenSpaceItems;
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
		_screenSpaceItems.Clear();
		if (keepExistingAreas)
		{
			foreach (var item in _clickAreas.Values)
			{
				item.Update();
				if (CheckIsScreenSpace(item.RootNode))
					_screenSpaceItems.Add(item);
				else
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

	/// <summary>
	/// 检测节点所属的 CanvasLayer 类型，同时检查 FollowViewportScale 是否为非标准值
	/// </summary>
	private bool CheckIsScreenSpace(Node2D node)
	{
		var current = node.GetParent();
		while (current != null)
		{
			if (current is CanvasLayer canvasLayer)
			{
				var isScreen = !canvasLayer.FollowViewportEnabled;
				var warn = canvasLayer.FollowViewportEnabled
					&& !Mathf.IsEqualApprox(canvasLayer.FollowViewportScale, 1f);
				if (warn)
				{
					GD.PushWarning($"[PassthroughManager] 节点 '{node.Name}' 位于 CanvasLayer 中，" +
						$"FollowViewportEnabled=true 但 FollowViewportScale={canvasLayer.FollowViewportScale}（非1.0），" +
						$"点击检测可能不准确。详见 README「已知限制」章节。");
				}
				return isScreen;
			}
			current = current.GetParent();
		}
		return false;
	}

	public void RegisterPolygon2DClickArea(Polygon2D poly)
	{
		var instanceId = poly.GetInstanceId();
		if (_clickAreas.ContainsKey(instanceId)) return;
		_clickAreas[instanceId] = new Polygon2DItem(poly);
		if (QuadTree == null) return;
		if (CheckIsScreenSpace(poly))
			_screenSpaceItems.Add(_clickAreas[instanceId]);
		else
			QuadTree.Insert(_clickAreas[instanceId]);
		_isUpdated = true;
	}

	public void RegisterCollisionPolygon2DClickArea(CollisionPolygon2D poly)
	{
		var instanceId = poly.GetInstanceId();
		if (_clickAreas.ContainsKey(instanceId))
		{
			UpdateClickArea(poly);
			return;
		}
		_clickAreas[instanceId] = new CollisionPloygon2DItem(poly);
		if (QuadTree == null) return;
		if (CheckIsScreenSpace(poly))
			_screenSpaceItems.Add(_clickAreas[instanceId]);
		else
			QuadTree.Insert(_clickAreas[instanceId]);
		_isUpdated = true;
	}

	public void UpdateAllClickArea()
	{
		foreach (var item in _clickAreas.Values)
		{
			item.Update();
			if (QuadTree == null) continue;
			if (_screenSpaceItems.Contains(item))
				continue; // 屏幕空间物品不需要四叉树操作
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
		var item = _clickAreas[instanceId];
		item.Update();
		if (_screenSpaceItems.Contains(item) || QuadTree == null)
			return; // 屏幕空间物品只需更新数据
		if (QuadTree.Update(item))
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
		if (CheckIsScreenSpace(shape))
			_screenSpaceItems.Add(_clickAreas[instanceId]);
		else
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
			_screenSpaceItems.Remove(area);
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
			viewWorldRect = _provider.Window.GetVisibleRect();
			worldMousePos = mouseScreenPos;
		}

		// Phase 1: 屏幕空间物品（CanvasLayer FollowViewport=false 或 Scale!=1.0）
		IQuadTreeItem hitItem = null;
		foreach (var item in _screenSpaceItems)
		{
			if (item.IsHit(mouseScreenPos))
			{
				hitItem = item;
				break;
			}
		}

		// Phase 2: 世界空间物品（四叉树两阶段查询）
		if (hitItem == null)
		{
			var candidates = QuadTree.QueryByRect(viewWorldRect);
			foreach (var item in candidates)
			{
				if (item.IsHit(worldMousePos))
				{
					hitItem = item;
					break;
				}
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
