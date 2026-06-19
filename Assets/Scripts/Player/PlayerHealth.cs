using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    private float currentHealth;

    private LowHealthEffects lowHealthEffects;

    void Start()
    {
        currentHealth = maxHealth;
        lowHealthEffects = GameController.gameController.GetComponentInChildren<LowHealthEffects>();
        if (lowHealthEffects != null)
            lowHealthEffects.SetCamera(GetComponentInChildren<Camera>().transform);
    }

    public void TakeDamage(float damage)
    {
        if (GameController.IsPaused || GameController.IsCreativoEnabled)
            return;

        currentHealth -= damage;
        
        if (lowHealthEffects != null)
            lowHealthEffects.SetHealth(currentHealth, maxHealth);
        if (currentHealth <= 0)
            Die();
        
    }

    public bool RestoreHealth(float amount)
    {
        if (GameController.IsPaused || GameController.IsCreativoEnabled)
            return false;

        if (currentHealth + amount > maxHealth)
            return false;

        currentHealth += amount;
        if (lowHealthEffects != null)            
            lowHealthEffects.SetHealth(currentHealth, maxHealth);
        return true;
    }

    private void Die()
    {
        // Handle player death (e.g., play animation, reload scene, etc.)
        Debug.Log("Player has died.");
        GameController.gameController.PlayersDie();
    }
}