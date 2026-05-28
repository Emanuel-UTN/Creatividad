using UnityEngine;

public class Valve : PuzzleObject
{
    private ValvePuzzle puzzle;

    private int valveIndex;
    public int Id => valveIndex;
    private Color[] colors = new Color[] { Color.red, Color.green, Color.blue };
    private float initialXRotation;

    [Header("Referencias")]
    public SpriteRenderer colorRenderer;

    [Header("Configuración")]
    public float requiredInteractionTime = 2.5f;
    public float lockDuration = 5f;
    public float blockedJiggleDuration = 0.2f;
    public float blockedJiggleAngle = 18f;
    private float currentInteractionTime = 0f;
    private float lastInteractionTime = 0f;
    private float lockedUntilTime = -1f;
    private float blockedJiggleTime = 0f;
    private bool activated = false;

    void Awake()
    {
        initialXRotation = transform.rotation.eulerAngles.x;
    }

    public void Initialize(ValvePuzzle puzzle, int index)
    {
        this.puzzle = puzzle;
        valveIndex = index;
        initialXRotation = transform.rotation.eulerAngles.x;

        if (colorRenderer != null && index < colors.Length)
            colorRenderer.color = colors[index];
    }

    void Update()
    {
        if (activated)
            return;

        bool locked = Time.time < lockedUntilTime;
        bool interacting = Time.time - lastInteractionTime <= 0.2f;

        if (!locked && !interacting)
            currentInteractionTime -= Time.deltaTime;

        currentInteractionTime = Mathf.Clamp(currentInteractionTime, 0f, requiredInteractionTime);

        UpdateVisuals(locked);
    }

    void UpdateVisuals(bool locked)
    {
        float progress = currentInteractionTime / requiredInteractionTime;

        float xRotation = initialXRotation + progress * 360f * 2.75f;

        if (locked && blockedJiggleTime > 0f)
        {
            float jiggleProgress = 1f - (blockedJiggleTime / blockedJiggleDuration);
            float jiggleOffset = Mathf.Sin(jiggleProgress * Mathf.PI) * blockedJiggleAngle;
            xRotation = initialXRotation + jiggleOffset;

            blockedJiggleTime -= Time.deltaTime;
            if (blockedJiggleTime < 0f)
                blockedJiggleTime = 0f;
        }

        transform.localRotation = Quaternion.Euler(xRotation, 90f, 90f);
    }

    public override void Interact()
    {
        if (activated) return;

        if (Time.time < lockedUntilTime)
        {
            blockedJiggleTime = blockedJiggleDuration;
            return;
        }

        currentInteractionTime += Time.deltaTime;
        lastInteractionTime = Time.time;

        if (currentInteractionTime >= requiredInteractionTime)
            Activate();
    }

    void Activate()
    {
        if (puzzle == null) return;
        activated = true;
        puzzle.ActivateValve(valveIndex);
    }

    public void ResetValve()
    {
        activated = false;
        currentInteractionTime = 0f;
        lastInteractionTime = 0f;
        lockedUntilTime = Time.time + lockDuration;
        blockedJiggleTime = 0f;
        transform.localRotation = Quaternion.Euler(initialXRotation, 90f, 90f);
    }
}