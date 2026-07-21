using Godot;

/// <summary>
/// 将自适应 UI 背景矩形包装成可被 PassthroughManager 命中的四叉树元素。
/// </summary>
public class ControlItem : IQuadTreeItem
{
    public QuadTreeNode CurrentNode { get; set; }
    public CanvasItem RootNode => _control;
    public Rect2 Bounds { get; private set; }
    public ulong ItemID { get; private set; }

    private readonly Control _control;

    public ControlItem(Control control)
    {
        _control = control;
        ItemID = control.GetInstanceId();
        Update();
    }

    public bool IsHit(Vector2 pos)
    {
        if (_control == null)
        {
            GD.Print("Control is null");
            return false;
        }

        return Bounds.HasPoint(pos);
    }

    public void Update()
    {
        if (_control == null)
        {
            Bounds = new Rect2();
            return;
        }

        Bounds = _control.GetGlobalRect();
    }
}
