using Godot;
using System;

public partial class FloatingText3D : Label3D
{
    [Export] public float Lifetime = 5.0f;
    private float FloatSpeed = 0.1f;
    private float _timer = 0f;

    public override void _Process(double delta)
    {
        float fDelta = (float)delta;
        _timer += fDelta;

        // move upwards
        FloatUpwards(fDelta);

        // smoothly fade out the text color
        float alpha = 1.0f - (_timer / Lifetime);
        Color currentModulate = Modulate;
        Color currentOutlineModulate = OutlineModulate;

        currentModulate.A = Mathf.Clamp(alpha, 0f, 1f);
        currentOutlineModulate.A = Mathf.Clamp(alpha, 0f, 1f);
        
        Modulate = currentModulate;
        OutlineModulate = currentOutlineModulate;

        // destory the instance once its lifetime expired
        if (_timer >= Lifetime)
        {
            QueueFree();
        }
    }

    private void FloatUpwards(float fDelta)
    {
        Vector3 currentPos = GlobalPosition;
        currentPos.Y += FloatSpeed * fDelta;
        GlobalPosition = currentPos;
    }
}
