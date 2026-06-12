using System;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class Door : MonoBehaviour
{
    [SerializeField]
    private PlayerInput playerInput;
    private InputAction interactAction;
    private bool isPlayerInRange = false;
    private bool isInitialized = false;
    public AnimationClip doorOpen;
    public float timeToOpen = 60f;

    [Header("Lock Settings")]
    public TMP_Text lockText;
    public Light lockLight;
    private int unlockedLocks = 0;
    [Range(1, 5)]
    public int lockCount = 3;

    public void Start()
    {
        if (!isInitialized)
            Initiate(lockCount);
    }

    public void Initiate(int lockCount = 0)
    {
        if (lockCount > 0)
            this.lockCount = lockCount;

        if (isInitialized)
            return;

        isInitialized = true;

        if (lockText != null)
            lockText.text = lockCount.ToString();

        if (PlayerController.playerController != null)
            playerInput = PlayerController.playerController.GetComponent<PlayerInput>();
    }

    public void OnTriggerEnter(Collider other) {
        if (!other.CompareTag("Player")) return;

        if (playerInput == null && PlayerController.playerController != null)
            playerInput = PlayerController.playerController.GetComponent<PlayerInput>();

        interactAction = playerInput.actions.FindAction("Interact", false);
        if (interactAction == null) return;
        
        
        isPlayerInRange = true;
        interactAction.performed += OnUnlock;
    }

    public void OnTriggerExit(Collider other) {
        if (!other.CompareTag("Player")) return;

        if (interactAction != null)
        {
            interactAction.performed -= OnUnlock;
            interactAction = null;
        }

        isPlayerInRange = false;
    }

    private void OnDisable()
    {
        if (interactAction != null)
        {
            interactAction.performed -= OnUnlock;
            interactAction = null;
        }
    }

    public void OnUnlock(InputAction.CallbackContext context) {
        if (!isPlayerInRange || playerInput == null)
            return;

        if (unlockedLocks < lockCount)
        {
            if (PlayerController.playerController.KeyCount <= 0) {
                StartCoroutine(LockFlicker());
                return;
            }
                
            unlockedLocks++;
            PlayerController.playerController.KeyCount--;

            if (lockText != null)
                lockText.text = (lockCount - unlockedLocks).ToString();
        }

        if (unlockedLocks >= lockCount)
            // All locks are unlocked, open the door
            OpenDoor();
    }

    private System.Collections.IEnumerator LockFlicker()
    {
        float originalIntensity = 0.75f;
        float duration = 1.75f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float flicker = Mathf.Sin(elapsed * 10f) * 0.5f + 1f;
            lockLight.intensity = originalIntensity * flicker;
            yield return null;
        }

        lockLight.intensity = originalIntensity;
    }

    public void OpenDoor() {
        Collider doorCollider = GetComponent<Collider>();
        Animator doorAnimator = GetComponent<Animator>();

        if (doorCollider != null)
            doorCollider.enabled = false;

        if (doorAnimator != null && doorOpen != null)
            Invoke("PlayAnimation", timeToOpen);
        
        if (lockText != null)
            lockText.color = Color.green;
        
        if (lockLight != null)
            lockLight.color = Color.green;
        
        GameController.gameController.OpenDoor(timeToOpen);
    }

    public void PlayAnimation() {
        Animator doorAnimator = GetComponent<Animator>();
        if (doorAnimator != null && doorOpen != null)
            doorAnimator.Play(doorOpen.name);
    }
}
