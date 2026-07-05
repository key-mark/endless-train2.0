using Godot;

public partial class Bullet : ColorRect
{
    private const float TopDestroyY = -32.0f;

    [Export] public float Speed { get; set; } = 620.0f;
    [Export] public int Damage { get; set; } = 10;

    public override void _Process(double delta)
    {
        Position += Vector2.Up * Speed * (float)delta;

        TryHitTarget();

        if (Position.Y < TopDestroyY)
        {
            QueueFree();
        }
    }

    private void TryHitTarget()
    {
        if (GetParent() is not Node parent)
        {
            return;
        }

        Rect2 bulletRect = new Rect2(Position, Size);
        foreach (Node child in parent.GetChildren())
        {
            if (child is Enemy enemy && !enemy.IsQueuedForDeletion() && bulletRect.Intersects(enemy.GetHitRect()))
            {
                enemy.TakeDamage(Damage);
                QueueFree();
                return;
            }

            if (child is UpgradeTarget target && !target.IsQueuedForDeletion() && bulletRect.Intersects(target.GetHitRect()))
            {
                target.TakeDamage(Damage);
                QueueFree();
                return;
            }
        }
    }
}
