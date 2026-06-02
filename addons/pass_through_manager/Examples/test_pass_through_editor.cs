using Godot;

public partial class test_pass_through_editor : Node2D
{
#if DEBUG
	public override void _Ready()
	{
		PassthroughManager.Instance.QuadTreeUpdate += RefreshQuadTreeDraw;
	}

	public override void _ExitTree()
	{
		PassthroughManager.Instance.QuadTreeUpdate -= RefreshQuadTreeDraw;
	}

	private bool _refresh;
	public void RefreshQuadTreeDraw()
	{
		QueueRedraw();
	}

	public override void _Draw()
	{
		GlobalPosition = new Vector2(0, 0);
		if (PassthroughManager.Instance.QuadTree?.Root != null)
			DrawInternal(PassthroughManager.Instance.QuadTree.Root);
		
		foreach (var item in PassthroughManager.Instance.ScreenSpaceItems)
		{
			var screenRect = item.Bounds;
			var cam = PassthroughManager.Instance.Camera;
			Rect2 drawRect;
			
			if (cam != null)
			{
				var viewSize = GetViewport().GetVisibleRect().Size;
				var camCenter = cam.GetScreenCenterPosition();
				var zoom = cam.Zoom;

				if (cam.AnchorMode == Camera2D.AnchorModeEnum.DragCenter)
				{
					var worldPos = camCenter + (screenRect.Position - viewSize / 2) / zoom;
					var worldSize = screenRect.Size / zoom;
					drawRect = new Rect2(worldPos / GlobalScale - GlobalPosition, worldSize / GlobalScale);
				}
				else
				{
					var worldPos = camCenter + screenRect.Position / zoom;
					var worldSize = screenRect.Size / zoom;
					drawRect = new Rect2(worldPos / GlobalScale - GlobalPosition, worldSize / GlobalScale);
				}
			}
			else
			{
				drawRect = new Rect2(screenRect.Position / GlobalScale - GlobalPosition, screenRect.Size / GlobalScale);
			}

			DrawRect(drawRect, Colors.Yellow, false, 5);
		}
	}

	private void DrawInternal(QuadTreeNode node)
	{
		var rect = new Rect2(node.Rect.Position / GlobalScale - GlobalPosition, node.Rect.Size / GlobalScale);
		DrawString(ThemeDB.FallbackFont, rect.Position + rect.Size / 2, $"Pos:{node.Rect.Position} Size:{node.Rect.Size}", fontSize: 16);
		DrawRect(rect, Colors.Green, false, 5);
		foreach (var item in node.quadTreeItems)
		{
			DrawRect(item.Bounds, Colors.Red, false, 5);
		}
		foreach (var child in node.children)
		{
			DrawInternal(child);
		}
	}

#endif
}
