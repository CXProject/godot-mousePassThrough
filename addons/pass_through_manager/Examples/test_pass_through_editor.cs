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
		if (PassthroughManager.Instance.QuadTree != null)
			DrawInternal(PassthroughManager.Instance.QuadTree.Root);
	}

	private void DrawInternal(QuadTreeNode node)
	{
		var rect = new Rect2(node.Rect.Position / GlobalScale - GlobalPosition, node.Rect.Size / GlobalScale);
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
