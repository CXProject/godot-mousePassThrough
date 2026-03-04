using Godot;

public partial class test_pass_through_editor : Node2D
{
	public override void _Ready()
	{
		PassthroughManager.Instance.QuadTreeUpdate += RefreshQuadTreeDraw;
	}

	private bool _refresh;
	public void RefreshQuadTreeDraw()
	{
		QueueRedraw();
	}

	public override void _Draw()
	{
		GlobalPosition = new Vector2(0, 0);
		DrawInternal(PassthroughManager.Instance.QuadTree.Root);
	}

	private void DrawInternal(QuadTreeNode node)
	{
		var rect = new Rect2(node.Rect.Position / GlobalScale - GlobalPosition, node.Rect.Size / GlobalScale);
		DrawRect(rect, Colors.Green, false, 5);
		// GD.Print(node.Rect.Position, node.quadTreeItems.Count);
		foreach (var child in node.children)
		{
			DrawInternal(child);
		}
	}


}
