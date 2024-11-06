using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public BoardPath currentPath;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        
    }

    // Kaikki liittyen peliin tulee tähän
}
