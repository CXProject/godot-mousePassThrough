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

	private Vector2 _cam_pos;
	private Vector2 _cam_zoom;

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
		RebuildData(keepExistingAreas);
		_forceClickableNodes.Clear();
	}

	private void RebuildData(bool keepExistingAreas)
	{
		var rect = _provider.Window.GetVisibleRect();
		if (Camera != null)
		{
			_cam_pos = Camera.GlobalPosition;
			_cam_zoom = Camera.Zoom;
			var size = rect.Size / Camera.Zoom;
			switch (Camera.AnchorMode)
			{
				case Camera2D.AnchorModeEnum.FixedTopLeft:
					rect = new Rect2(Camera.GlobalPosition, size);
					break;
				case Camera2D.AnchorModeEnum.DragCenter:
					rect = new Rect2(Camera.GlobalPosition - size / 2, size);
					break;
			}
		}

		QuadTree = new QuadTree(rect, _maxDepth, _maxItemCount);
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

	public void SetMainCamera(Camera2D camera, bool keepExistingAreas = true)
	{
		Camera = camera;
		RebuildData(keepExistingAreas);
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

		// 获取鼠标在屏幕空间的坐标
		var mousePos = GetViewport().GetMousePosition();

		if (Camera != null)
		{
			if (Camera.AnchorMode == Camera2D.AnchorModeEnum.DragCenter)
			{
				//如果是中心点为相机锚点，则鼠标坐标和相机坐标有个半个视口大小的便宜
				var viewSize = GetViewport().GetVisibleRect().Size;
				mousePos = Camera.GlobalPosition + (mousePos - viewSize / 2) / Camera.Zoom;
			}
			else
			{
				// 如果锚点在左上角就和鼠标坐标原点相同，所以直接使用鼠标位置，并根据相机缩放反向缩放下鼠标坐标
				mousePos = Camera.GlobalPosition + mousePos / Camera.Zoom;
			}

			if (QuadTree.Root.Rect.HasPoint(mousePos) == false)
			{
				if (_cam_pos != Camera.GlobalPosition || _cam_zoom != Camera.Zoom)
				{
					RebuildData(true);
				}
			}

		}

		// 根据鼠标位置查询 QuadTree，判断是否有可点击区域
		var item = QuadTree.GetHitItem(mousePos);
		// 如果没有命中任何区域，设置窗口穿透鼠标事件
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
