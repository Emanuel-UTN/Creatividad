using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class Cupboard : MonoBehaviour
{
    [Header("Cupboard")]
    public Transform hidePoint;
    public Transform exitPoint;
    public Transform enemyApproachPoint;
    public float interactionRadius = 2f;
    public float interactionForwardOffset = 1.1f;

    [Header("Audio")]
    public AudioClip enterCupboardClip;
    public AudioClip exitCupboardClip;
    [Range(0f, 1f)] public float cupboardAudioVolume = 1f;

    private PlayerController nearbyPlayer;
    private PlayerController occupant;
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    public bool HasHiddenPlayer => occupant != null;

    public Vector3 CupboardPosition => hidePoint != null ? hidePoint.position : transform.position;
    public Vector3 EnemyApproachPosition
    {
        get
        {
            if (enemyApproachPoint != null)
                return enemyApproachPoint.position;

            if (exitPoint != null)
                return exitPoint.position;

            return transform.position;
        }
    }

    public bool IsPlayerInInteractionRange(PlayerController player)
    {
        if (player == null)
            return false;

        Vector3 interactionCenter = GetInteractionCenter();
        Vector3 toPlayer = player.transform.position - interactionCenter;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude > interactionRadius * interactionRadius)
            return false;

        Vector3 forward = GetInteractionForward();
        Vector3 fromCupboard = player.transform.position - transform.position;
        fromCupboard.y = 0f;
        if (fromCupboard.sqrMagnitude <= 0.0001f)
            return true;

        return Vector3.Dot(fromCupboard.normalized, forward) > 0f;
    }

    public bool TryHidePlayer(PlayerController player, bool enemyHasDirectVision)
    {
        if (player == null || occupant != null)
            return false;

        if (!IsPlayerInInteractionRange(player))
            return false;

        Vector3 outwardLookDirection = player.transform.position - CupboardPosition;
        outwardLookDirection.y = 0f;
        if (outwardLookDirection.sqrMagnitude <= 0.0001f)
            outwardLookDirection = transform.forward;

        occupant = player;
        player.EnterCupboard(this, CupboardPosition, outwardLookDirection.normalized, enemyHasDirectVision);
        PlayCupboardClip(enterCupboardClip);
        return true;
    }

    public bool TryExitPlayer(PlayerController player)
    {
        if (player == null || occupant != player)
            return false;

        Vector3 targetExit = exitPoint != null ? exitPoint.position : transform.position + transform.forward * 1.2f;
        occupant = null;
        player.ExitCupboard(this, targetExit, false);
        PlayCupboardClip(exitCupboardClip != null ? exitCupboardClip : enterCupboardClip);
        return true;
    }

    public void ForceEjectHiddenPlayer()
    {
        if (occupant == null)
            return;

        Vector3 targetExit = exitPoint != null ? exitPoint.position : transform.position + transform.forward * 1.2f;
        PlayerController player = occupant;
        occupant = null;
        player.ExitCupboard(this, targetExit, true);
        PlayCupboardClip(exitCupboardClip != null ? exitCupboardClip : enterCupboardClip);
    }

    public bool IsEnemyCloseEnoughToEject(Vector3 enemyPosition)
    {
        Vector3 toEnemy = enemyPosition - EnemyApproachPosition;
        toEnemy.y = 0f;
        float allowedDistance = Mathf.Max(0.9f, interactionRadius * 0.75f);
        return toEnemy.sqrMagnitude <= allowedDistance * allowedDistance;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null)
            return;

        nearbyPlayer = player;
        nearbyPlayer.SetNearbyCupboard(this);
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null)
            return;

        if (nearbyPlayer == player)
            nearbyPlayer = null;

        player.ClearNearbyCupboard(this);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 interactionCenter = GetInteractionCenter();

        Gizmos.color = new Color(0.3f, 0.9f, 1f, 0.5f);
        Gizmos.DrawWireSphere(interactionCenter, interactionRadius);
        Gizmos.DrawLine(transform.position, interactionCenter);

        if (hidePoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(hidePoint.position, 0.2f);
        }

        if (exitPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(exitPoint.position, 0.2f);
        }

        if (enemyApproachPoint != null)
        {
            Gizmos.color = new Color(1f, 0.4f, 0f, 1f);
            Gizmos.DrawWireCube(enemyApproachPoint.position, Vector3.one * 0.25f);
        }
    }

    private Vector3 GetInteractionCenter()
    {
        Vector3 origin = exitPoint != null ? exitPoint.position : transform.position;
        Vector3 forward = GetInteractionForward();
        return origin + forward * interactionForwardOffset;
    }

    private Vector3 GetInteractionForward()
    {
        if (exitPoint != null)
        {
            Vector3 forward = exitPoint.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude > 0.0001f)
                return forward.normalized;
        }

        Vector3 fallback = transform.forward;
        fallback.y = 0f;
        if (fallback.sqrMagnitude > 0.0001f)
            return fallback.normalized;

        return Vector3.forward;
    }

    private void PlayCupboardClip(AudioClip clip)
    {
        if (audioSource == null || clip == null)
            return;

        audioSource.PlayOneShot(clip, Mathf.Clamp01(cupboardAudioVolume));
    }
}
