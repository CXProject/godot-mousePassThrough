using Godot;
using System;

public class CollisionShape2DItem : IQuadTreeItem
{
    public QuadTreeNode CurrentNode { get; set; }
    public CanvasItem RootNode => _shape;

    public Rect2 Bounds { get; private set; }

    public ulong ItemID { get; private set; }

    private CollisionShape2D _shape;
    private readonly CircleShape2D _point_shape = new CircleShape2D
    {
        Radius = 0f
    };

    public bool IsHit(Vector2 pos)
    {
        if (_shape == null)
        {
            GD.Print("CollisionShape2D is null");
            return false;
        }

        var point_tf = new Transform2D(0.0f, pos);
        return _shape.Shape.Collide(_shape.GlobalTransform, _point_shape, point_tf);
    }

    public CollisionShape2DItem(CollisionShape2D shape)
    {
        ItemID = shape.GetInstanceId();
        _shape = shape;
        Update();
    }

    public Rect2 GetGlobalRect(CollisionShape2D shape)
    {
        Rect2 localRect = shape.Shape.GetRect();
        // 转换为全局坐标
        Vector2 globalPosition = shape.ToGlobal(localRect.Position);
        // 应用全局缩放（scale 会影响 size）
        Vector2 globalSize = localRect.Size * shape.GlobalScale;
        return new Rect2(globalPosition, globalSize);
    }

    public void Update()
    {
        Bounds = GetGlobalRect(_shape);
    }

}
