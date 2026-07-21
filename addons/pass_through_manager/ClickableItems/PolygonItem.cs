using Godot;
using System;

public class Polygon2DItem : PolygonItemBase
{
	private Polygon2D _polygon2d => root as Polygon2D;

	public Polygon2DItem(Polygon2D polygon) : base(polygon)
	{
	}

	public override void Update()
	{
		SetPolygon(_polygon2d.Polygon);
	}
}

public class CollisionPloygon2DItem : PolygonItemBase
{
	private CollisionPolygon2D _polygon2d => root as CollisionPolygon2D;

	public CollisionPloygon2DItem(CollisionPolygon2D polygon) : base(polygon)
	{
	}

	public override void Update()
	{
		SetPolygon(_polygon2d.Polygon);
	}
}

public abstract class PolygonItemBase : IQuadTreeItem
{
	public QuadTreeNode CurrentNode { get; set; }
	public CanvasItem RootNode => root;

	public Rect2 Bounds { get; protected set; }

	public ulong ItemID { get; protected set; }
	protected Node2D root;

	private Vector2[] _polygon;

	public PolygonItemBase(Node2D r)
	{
		root = r;
		ItemID = root.GetInstanceId();
		Update();
	}

	public bool IsHit(Vector2 pos)
	{
		if (root == null)
		{
			GD.Print("Polygon is null");
			return false;
		}
		return Geometry2D.IsPointInPolygon(pos, _polygon);
	}

	protected void SetPolygon(Vector2[] polygon)
	{
		for (var i = 0; i < polygon.Length; i++)
		{
			polygon[i] = root.ToGlobal(polygon[i]);
		}

		Bounds = GetPolygonAABB(polygon);
		_polygon = polygon;
	}

	protected Rect2 GetPolygonAABB(Vector2[] poly)
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

	public abstract void Update();
}
