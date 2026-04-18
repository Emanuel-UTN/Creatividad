using UnityEngine;

public class GameController : MonoBehaviour
{
    public static GameController gameController;
    public GameObject player;

    void Awake()
    {
        if (gameController == null)
        {
            gameController = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (gameController != this)
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
