using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(EnemyBehaviour))]
 [RequireComponent(typeof(Movement))]
public class EnemyController : MonoBehaviour
{
    public Animator animator;
    [SerializeField] private string walkingBoolName = "IsWalking";
    [SerializeField] private string runningBoolName = "IsRunning";
    [SerializeField] private float walkEnterSpeed = 0.08f;
    [SerializeField] private float walkExitSpeed = 0.04f;

    [Header("Attack")]
    public float attackRange = 1.5f;
    public float attackDamage = 10f;
    public float attackCooldown = 1.5f;
    private float lastAttackTime;

    private EnemyBehaviour enemyBehaviour;
    private Movement movement;
    private int walkingBoolHash;
    private int runningBoolHash;
    private Vector3 lastPosition;
    private bool walkingState;

    [Header("Screamer")]
    public VideoClip screamerClip;

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

    public void TryAttack()
    {
        PlayerController player = PlayerController.playerController;
        if (player == null)
            return;
        
        if (Vector3.Distance(transform.position, player.transform.position) <= attackRange)
        {
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                player.TakeDamage(attackDamage);
                lastAttackTime = Time.time;
            }
        }
    }
}
