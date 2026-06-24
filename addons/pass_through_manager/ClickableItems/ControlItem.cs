using Godot;

public class ControlItem : PolygonItemBase
{
    private Control? _control => root as Control;

    public ControlItem(Control polygon) : base(polygon)
    {
    }

    public override void Update()
    {
        if (_control == null) return;

        var rect = _control.GetRect();

        Vector2[] polygon =
        [
            rect.Position,
            rect.Position + new Vector2(rect.Size.X, 0),
            rect.Position + rect.Size,
            rect.Position + new Vector2(0, rect.Size.Y)
        ];

        for (var i = 0; i < polygon.Length; i++)
        {
            polygon[i] = polygon[i];
        }

        SetPolygon(polygon);
    }
}