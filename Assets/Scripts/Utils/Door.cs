using System;
using UnityEngine;
using UnityEngine.InputSystem;

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
    public GameObject lockPrefab;
    private GameObject[] locks;
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

        locks = new GameObject[this.lockCount];
        for (int i = 0; i < this.lockCount; i++)
        {
            Vector3 offset = new Vector3(0, i < 3 ? i * 0.5f : (i - 2) * - 0.5f, 0.1f);
            locks[i] = Instantiate(lockPrefab, transform.position + offset, Quaternion.identity, transform);
            locks[i].transform.localPosition = offset; // Asegura que la posición local se mantenga correcta
        }

        if (PlayerController.playerController != null)
            playerInput = PlayerController.playerController.GetComponent<PlayerInput>();
    }

    public void OnTriggerEnter(Collider other) {
        if (!other.CompareTag("Player")) return;

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

        if (locks == null || unlockedLocks >= locks.Length)
            return;

        if (unlockedLocks < lockCount)
        {
            if (PlayerController.playerController.KeyCount <= 0) {
                locks[unlockedLocks].GetComponent<Animator>().SetTrigger("Block Lock");
                return;
            }
                
            locks[unlockedLocks].GetComponent<Collider>().enabled = true;
            locks[unlockedLocks].GetComponent<Rigidbody>().useGravity = true;
            unlockedLocks++;
            PlayerController.playerController.KeyCount--;
        }

        if (unlockedLocks >= lockCount)
            // All locks are unlocked, open the door
            OpenDoor();
    }

    public void OpenDoor() {
        Collider doorCollider = GetComponent<Collider>();
        Animator doorAnimator = GetComponent<Animator>();

        if (doorCollider != null)
            doorCollider.enabled = false;

        if (doorAnimator != null && doorOpen != null)
            Invoke("PlayAnimation", timeToOpen);
        
        GameController.gameController.OpenDoor(timeToOpen);
    }

    public void PlayAnimation() {
        Animator doorAnimator = GetComponent<Animator>();
        if (doorAnimator != null && doorOpen != null)
            doorAnimator.Play(doorOpen.name);
    }
}
