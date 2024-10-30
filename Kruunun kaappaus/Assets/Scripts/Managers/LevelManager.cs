using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public enum LevelType
{
    Challenge, Minigame
}

public class LevelManager : MonoBehaviour
{
    public static LevelManager instance;
    [SerializeField] private GameObject heartCanvas;
    [SerializeField] private GameObject heartPrefab;

    [Header("Main")]
    public Vector2[] playerSpawnPoint = new Vector2[1];
    public Vector2[] ghostSpawnPoints = new Vector2[3];
    public LevelType currentLevelType;
    [Space]

    [Header("Jos Haaste")]
    public NetworkVariable<int> lives = new(writePerm: NetworkVariableWritePermission.Owner, value: 3);
    [Space]
    [Header("Jos minipeli")]
    public float levelDurationSeconds;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    private void Start()
    {
        SpawnHearts();
    }

    public void LoseHeart()
    {
        lives.Value--;
        Destroy(heartCanvas.transform.GetChild(0).gameObject);
    }

    private void SpawnHearts()
    {
        for (int i = 0;  i < lives.Value; i++)
        {
            Instantiate(heartPrefab, heartCanvas.transform);
        }
    }
}
