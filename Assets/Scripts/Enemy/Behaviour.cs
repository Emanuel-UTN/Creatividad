using UnityEngine;

public abstract class Behaviour
{
    protected readonly Movement movement;
    protected readonly Transform enemyTransform;

    protected Behaviour(Movement movement, Transform enemyTransform)
    {
        this.movement = movement;
        this.enemyTransform = enemyTransform;
    }

    protected void Move(Transform target, float offset)
    {
        if (target == null)
            return;

        movement.offset = offset;
        movement.moveToTarget = true;
        movement.rotateTowardsTarget = true;
        movement.target = target;
    }

    protected void Stop(float offset, bool rotateTowardsTarget)
    {
        movement.offset = offset;
        movement.moveToTarget = false;
        movement.rotateTowardsTarget = rotateTowardsTarget;
    }

    protected bool HasReached(Vector3 destination)
    {
        Vector3 toDestination = destination - enemyTransform.position;
        toDestination.y = 0f;
        float threshold = movement.offset + 0.05f;
        return toDestination.sqrMagnitude <= threshold * threshold;
    }
}
