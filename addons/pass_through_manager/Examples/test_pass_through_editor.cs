using Godot;

public partial class test_pass_through_editor : Node2D
{
#if DEBUG
	public override void _Ready()
	{
		if (PassthroughManager.Instance != null)
		{
			PassthroughManager.Instance.QuadTreeUpdate += RefreshQuadTreeDraw;
		}
	}

	public override void _ExitTree()
	{
		if (PassthroughManager.Instance != null)
		{
			PassthroughManager.Instance.QuadTreeUpdate -= RefreshQuadTreeDraw;
		}
	}

	private bool _refresh;
	public void RefreshQuadTreeDraw()
	{
		QueueRedraw();
	}

	public override void _Draw()
	{
		GlobalPosition = new Vector2(0, 0);
		if (PassthroughManager.Instance == null || PassthroughManager.Instance.QuadTree == null)
			return;

		var root = PassthroughManager.Instance.QuadTree.Root;
		if (root == null)
			return;

		DrawInternal(root);
	}

	private void DrawInternal(QuadTreeNode node)
	{
		if (node == null)
			return;

		var rect = new Rect2(node.Rect.Position / GlobalScale - GlobalPosition, node.Rect.Size / GlobalScale);
		if (ThemeDB.FallbackFont != null)
		{
			DrawString(ThemeDB.FallbackFont, rect.Position + rect.Size / 2, $"Pos:{node.Rect.Position} Size:{node.Rect.Size}", fontSize: 16);
		}
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
