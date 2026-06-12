using UnityEngine;

public class FirstAidKit : MonoBehaviour
{
    public float healthAmount = 10f;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;
        
        if (PlayerController.playerController.RestoreHealth(healthAmount))
            Destroy(gameObject);
    }
}
