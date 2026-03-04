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
		DrawInternal(PassthroughManager.Instance.QuadTree.Root);
	}

	private void DrawInternal(QuadTreeNode node)
	{
		DrawRect(node.Rect, Colors.Green, false, 5);
		// GD.Print(node.Rect.Position, node.quadTreeItems.Count);
		foreach (var child in node.children)
		{
			DrawInternal(child);
		}
	}


}
