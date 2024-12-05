using System;
using System.Collections;
using TMPro;
using Unity.Cinemachine;
using Unity.Collections;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public enum LevelType
{
    Challenge, Minigame
}
public enum LevelState
{
    LoadingPlayers, Starting, InProgress, Ending, Idle
}

public class LevelManager : NetworkBehaviour
{
    public static LevelManager instance;
    [SerializeField] private CinemachineCamera playerCamera;
    [SerializeField] private CinemachineCamera endingCamera;
    [Header("UI")]
    [SerializeField] private GameObject timerGrid;
    [SerializeField] private GameObject blackScreenUI;
    [SerializeField] private TextMeshProUGUI goTimerUI;
    [SerializeField] private TextMeshProUGUI overUI;
    private TextMeshProUGUI timerVisual;
    private float goTimer = 4;

    [Header("Main")]
    private NetworkVariable<int> playersLoaded = new();
    [SerializeField] private float cameraAnimationSeconds;
    public Vector2[] playerSpawnPoint = new Vector2[1];
    public Vector2[] ghostSpawnPoints = new Vector2[3];
    public float levelDurationSeconds;
    public int availableCoins;
    public LevelType currentLevelType;
    public LevelState CurrentGameState { get; private set; }
    public float LevelTimer { get; private set; }

    //Events
    public delegate void OnPlayerValueChangeDelegate();
    public event OnPlayerValueChangeDelegate OnPlayerValueChange;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        timerVisual = timerGrid.GetComponentInChildren<TextMeshProUGUI>();
    }

    private void Start()
    {
        LevelTimer = levelDurationSeconds;
        timerGrid.SetActive(true);
    }

    private void Update()
    {
        GameLoop();
    }
    private void GameLoop()
    {
        switch (CurrentGameState)
        {
            case LevelState.LoadingPlayers:
                PlayerLoadState();
                break;
            case LevelState.Starting:
                ThreeTwoOneGo();
                break;
            case LevelState.InProgress:
                RunTimer();
                if (currentLevelType == LevelType.Minigame && availableCoins == 0)
                {
                    CurrentGameState = LevelState.Ending;
                }
                // Run everything for the game
                break;
            case LevelState.Ending:
                // Play animation and switch to idle (BELOW IS TEMPORARY)
                StartCoroutine(EndingAnimation());
                CurrentGameState = LevelState.Idle;
                break;
        }
    }
    private void PlayerLoadState()
    {
        if (playersLoaded.Value < NetworkManager.Singleton.SpawnManager.GetConnectedPlayers().Count)
        {
            Time.timeScale = 0;
            return;
        }

        Time.timeScale = 1;
        switch (currentLevelType)
        {
            case LevelType.Challenge:
                StartCoroutine(CameraAnimation());
                break;
            case LevelType.Minigame:
                // skippaa kameran animaation sillä ei tarvi nähdä "endgoal"
                goTimerUI.gameObject.SetActive(true);
                break;
        }

        int playerCount = 0;
        int ghostCount = 0;

        foreach (var player in GameObject.FindGameObjectsWithTag("Player"))
        {
            PlayerMovement2D playerComponent = player.GetComponent<PlayerMovement2D>();
            if (player.GetComponentInParent<MainPlayerInfo>().isGhost.Value)
            {
                playerComponent.spawnPoint = ghostSpawnPoints[ghostCount];
                player.transform.position = playerComponent.spawnPoint;
                ghostCount++;
                continue;
            }

            playerComponent.spawnPoint = playerSpawnPoint[playerCount];
            player.transform.position = playerComponent.spawnPoint;
            playerCount++;
        }

        TimerVisual();
        CurrentGameState = LevelState.Starting;
        blackScreenUI.SetActive(false);

        OnPlayerValueChange?.Invoke();
    }
    public void SetCamera(Transform player)
    {
        playerCamera.Follow = player;
    }
    private void RunTimer()
    {
        if (LevelTimer < 1)
        {
            CurrentGameState = LevelState.Ending;
            return;
        }

        LevelTimer -= Time.deltaTime;
        TimerVisual();
    }

    private void TimerVisual()
    {
        var timeInMinutes = Mathf.FloorToInt(LevelTimer / 60);
        var timeInSeconds = Mathf.FloorToInt(LevelTimer - timeInMinutes * 60);
        string timerText = string.Format("Time remaining: {0:00}:{1:00}", timeInMinutes, timeInSeconds);
        timerVisual.text = timerText;
    }
    private IEnumerator CameraAnimation()
    {
        // Odottaa että peli päivittää kaiken
        yield return new WaitForSeconds(0.5f);
        endingCamera.Priority = 1;
        playerCamera.Priority = 0;
        yield return new WaitForSeconds(cameraAnimationSeconds);
        endingCamera.Priority = 0;
        playerCamera.Priority = 1;
        yield return new WaitForSeconds(cameraAnimationSeconds - 1);
        goTimerUI.gameObject.SetActive(true);
    }
    private IEnumerator EndingAnimation()
    {
        float timeToWait = 0;
        switch (currentLevelType)
        {
            case LevelType.Challenge:
                endingCamera.Priority = 1;
                playerCamera.Priority = 0;
                timeToWait = cameraAnimationSeconds + 0.5f;
                break;
            case LevelType.Minigame:
                timeToWait = 1.5f;
                break;
        }
        overUI.gameObject.SetActive(true);
        yield return new WaitForSeconds(timeToWait);

        if (!IsServer)
        {
            yield break;
        }
        NetworkManager.SceneManager.LoadScene("MainBoard", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }
    private void ThreeTwoOneGo()
    {
        if (!goTimerUI.IsActive())
        {
            return;
        }

        if (goTimer <= 0)
        {
            goTimerUI.gameObject.SetActive(false);
            CurrentGameState = LevelState.InProgress;
        }
        goTimer -= Time.deltaTime;
        goTimerUI.text = goTimer <= 1 ? "Go!" : $"{(int)goTimer}";
    }
    [ServerRpc(RequireOwnership = false)]
    public void LoadPlayerServerRpc()
    {
        playersLoaded.Value++;
    }

    [ServerRpc(RequireOwnership = false)]
    public void PlayAnimationServerRpc(ulong objectId, string animation)
    {
        var animationObject = NetworkManager.SpawnManager.SpawnedObjects[objectId];
        animationObject.GetComponent<Animator>().Play(animation);
    }
    [ClientRpc]
    public void UpdateLevelStateClientRpc()
    {
        CurrentGameState = LevelState.Ending;
    }
}
