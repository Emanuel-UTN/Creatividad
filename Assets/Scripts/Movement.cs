using UnityEngine;

public class Movement : MonoBehaviour
{
    [Header("Movimiento")]
    public float speed = 5f;
    public float rotationSpeed = 10f;
    public float offset = 2.5f;
    public Transform target;

    [Header("Options")]
    public bool moveToTarget = true;
    public bool rotateTowardsTarget = true;

    void Update()
    {
        if (target == null)
            return;

        Vector3 toTarget = target.position - transform.position;
        float distance = toTarget.magnitude;

        if (moveToTarget && distance > offset)
        {
            Vector3 direction = toTarget / distance;
            float step = Mathf.Min(speed * Time.deltaTime, distance - offset);
            transform.position += direction * step;
        }

        if (rotateTowardsTarget)
        {
            Vector3 flatDirection = new Vector3(toTarget.x, 0f, toTarget.z);
            if (flatDirection.sqrMagnitude > 0.0001f)
            {
                Quaternion desiredRotation = Quaternion.LookRotation(flatDirection.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSpeed * Time.deltaTime);
            }
        }
    }
}
