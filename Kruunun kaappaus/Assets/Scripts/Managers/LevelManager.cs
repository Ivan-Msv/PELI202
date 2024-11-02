using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public enum LevelType
{
    Challenge, Minigame
}

public class LevelManager : NetworkBehaviour
{
    public static LevelManager instance;
    [SerializeField] private GameObject heartGrid;
    [SerializeField] private GameObject heartPrefab;
    [SerializeField] private GameObject timerGrid;
    private TextMeshProUGUI timerVisual;

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
    public float LevelTimer { get; private set; }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        timerVisual = timerGrid.GetComponentInChildren<TextMeshProUGUI>();
        lives.OnValueChanged += OnLoseHeart;
    }

    private void Start()
    {
        switch (currentLevelType)
        {
            case LevelType.Challenge:
                heartGrid.SetActive(true);
                SpawnHearts();
                break;
            case LevelType.Minigame:
                LevelTimer = levelDurationSeconds;
                timerGrid.SetActive(true);
                break;
        }
    }

    private void Update()
    {
        if (currentLevelType == LevelType.Minigame)
        {
            RunTimer();
        }
    }
    private void OnLoseHeart(int oldvalue, int newvalue)
    {
        Destroy(heartGrid.transform.GetChild(0).gameObject);
    }
    private void SpawnHearts()
    {
        for (int i = 0;  i < lives.Value; i++)
        {
            Instantiate(heartPrefab, heartGrid.transform);
        }
    }
    private void RunTimer()
    {
        if (LevelTimer < 1)
        {
            // Do something instead of empty return
            return;
        }

        LevelTimer -= Time.deltaTime;
        var timeInMinutes = Mathf.FloorToInt(LevelTimer / 60);
        var timeInSeconds = Mathf.FloorToInt(LevelTimer - timeInMinutes * 60);
        string timerText = string.Format("Time remaining: {0:00}:{1:00}", timeInMinutes, timeInSeconds);
        timerVisual.text = timerText;
    }


    [ServerRpc(RequireOwnership = false)]
    public void LoseHeartServerRpc()
    {
        lives.Value--;
    }

    [ServerRpc(RequireOwnership = false)]
    public void PlayAnimationServerRpc(ulong objectId, string animation)
    {
        var animationObject = NetworkManager.SpawnManager.SpawnedObjects[objectId];
        animationObject.GetComponent<Animator>().Play(animation);
    }
}
