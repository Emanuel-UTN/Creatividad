using UnityEngine;

[RequireComponent(typeof(EnemyBehaviour))]
 [RequireComponent(typeof(Movement))]
public class EnemyController : MonoBehaviour
{
    public Animator animator;
    [SerializeField] private string walkingBoolName = "IsWalking";
    [SerializeField] private string runningBoolName = "IsRunning";
    [SerializeField] private float walkEnterSpeed = 0.08f;
    [SerializeField] private float walkExitSpeed = 0.04f;

    private EnemyBehaviour enemyBehaviour;
    private Movement movement;
    private int walkingBoolHash;
    private int runningBoolHash;
    private Vector3 lastPosition;
    private bool walkingState;

    void Start()
    {
        enemyBehaviour = GetComponent<EnemyBehaviour>();
        movement = GetComponent<Movement>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        walkingBoolHash = Animator.StringToHash(walkingBoolName);
        runningBoolHash = Animator.StringToHash(runningBoolName);
        lastPosition = transform.position;
    }

    void Update()
    {
        if (animator == null || enemyBehaviour == null || movement == null)
            return;

        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        Vector3 delta = transform.position - lastPosition;
        lastPosition = transform.position;

        float planarSpeed = new Vector2(delta.x, delta.z).magnitude / dt;
        bool isRunning = enemyBehaviour.IsRunning;

        if (isRunning)
        {
            walkingState = false;
        }
        else if (walkingState)
        {
            if (planarSpeed <= Mathf.Max(0f, walkExitSpeed))
                walkingState = false;
        }
        else
        {
            if (planarSpeed >= Mathf.Max(0f, walkEnterSpeed))
                walkingState = true;
        }

        animator.SetBool(walkingBoolHash, walkingState);
        animator.SetBool(runningBoolHash, isRunning);
    }
}
