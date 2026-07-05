using Godot;

public partial class Bullet : ColorRect
{
    private const float TopDestroyY = -32.0f;

    [Export] public float Speed { get; set; } = 620.0f;
    [Export] public int Damage { get; set; } = 10;
    [Export] public float ExplosionRadius { get; set; }

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
                Vector2 hitCenter = new(enemy.CenterX(), enemy.Position.Y + enemy.Size.Y * 0.5f);
                enemy.TakeDamage(Damage);
                DamageNearbyTargets(parent, child, hitCenter);
                QueueFree();
                return;
            }

            if (child is Pickup pickup && !pickup.IsQueuedForDeletion() && bulletRect.Intersects(pickup.GetHitRect()))
            {
                Vector2 hitCenter = pickup.Position + pickup.Size * 0.5f;
                pickup.TakeDamage(Damage);
                DamageNearbyTargets(parent, child, hitCenter);
                QueueFree();
                return;
            }

            if (child is Boss boss && !boss.IsQueuedForDeletion() && bulletRect.Intersects(boss.GetHitRect()))
            {
                Vector2 hitCenter = boss.Position + boss.Size * 0.5f;
                boss.TakeDamage(Damage);
                DamageNearbyTargets(parent, child, hitCenter);
                QueueFree();
                return;
            }
        }
    }

    private void DamageNearbyTargets(Node parent, Node directHit, Vector2 hitCenter)
    {
        if (ExplosionRadius <= 0.0f)
        {
            return;
        }

        foreach (Node child in parent.GetChildren())
        {
            if (ReferenceEquals(child, directHit) || child.IsQueuedForDeletion())
            {
                continue;
            }

            if (child is Enemy enemy && hitCenter.DistanceTo(new Vector2(enemy.CenterX(), enemy.Position.Y + enemy.Size.Y * 0.5f)) <= ExplosionRadius)
            {
                enemy.TakeDamage(Damage);
            }
            else if (child is Pickup pickup && hitCenter.DistanceTo(pickup.Position + pickup.Size * 0.5f) <= ExplosionRadius)
            {
                pickup.TakeDamage(Damage);
            }
            else if (child is Boss boss && hitCenter.DistanceTo(boss.Position + boss.Size * 0.5f) <= ExplosionRadius)
            {
                boss.TakeDamage(Damage);
            }
        }
    }
}
