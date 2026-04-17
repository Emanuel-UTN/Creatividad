using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager gameManager;
    public GameObject player;

    void Awake()
    {
        if (gameManager == null)
        {
            gameManager = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (gameManager != this)
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        
    }

    void Update()
    {
        
    }
}
