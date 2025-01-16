using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Cinemachine;
using Unity.Netcode;
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
    public CoinCounter coinCounter;
    [SerializeField] private CinemachineCamera playerCamera;
    [SerializeField] private CinemachineCamera endingCamera;
    [Header("UI")]
    [SerializeField] private GameObject timerGrid;
    [SerializeField] private GameObject coinCounterUI;
    [SerializeField] private GameObject goTimerUI;
    [SerializeField] private GameObject overUI;
    [SerializeField] private TextMeshProUGUI loadPlayerText;
    [SerializeField] private TextMeshProUGUI goTimerText;
    [SerializeField] private TextMeshProUGUI timerVisual;

    [Header("Main")]
    public NetworkVariable<LevelState> CurrentGameState = new();

    public NetworkVariable<int> availableCoins = new();
    public NetworkVariable<float> LevelTimer = new();
    private NetworkVariable<int> playersLoaded = new();
    public LevelType currentLevelType;
    public float levelDurationSeconds;
    [SerializeField] private float cameraAnimationSeconds;
    public Vector2[] playerSpawnPoint = new Vector2[1];
    public Vector2[] ghostSpawnPoints = new Vector2[3];

    //Events
    public delegate void OnPlayerValueChangeDelegate();
    public event OnPlayerValueChangeDelegate OnPlayerValueChange;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    private void Start()
    {
        if (IsServer)
        {
            LevelTimer.Value = levelDurationSeconds;
        }

        timerGrid.SetActive(true);
    }

    private void Update()
    {
        TimerVisual();
        if (!IsServer)
        {
            return;
        }

        GameLoop();
    }
    private void GameLoop()
    {
        switch (CurrentGameState.Value)
        {
            case LevelState.LoadingPlayers:
                PlayerLoadState();
                break;
            case LevelState.Starting:
                break;
            case LevelState.InProgress:
                RunTimer();

                if (currentLevelType == LevelType.Minigame && availableCoins.Value == 0)
                {
                    CurrentGameState.Value = LevelState.Ending;
                }

                break;
            case LevelState.Ending:
                CurrentGameState.Value = LevelState.Idle;
                EndingStateRpc();
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
                ChallengeAnimationRpc();
                break;
            case LevelType.Minigame:
                MinigameAnimationRpc();
                break;
        }

        int playerCount = 0;
        int ghostCount = 0;

        List<GameObject> players = GameObject.FindGameObjectsWithTag("Player").ToList();
        List<GameObject> shuffledPlayers = new();
        int n = players.Count;

        while (n > 0)
        {
            var randomIndex = UnityEngine.Random.Range(0, n);
            shuffledPlayers.Add(players[randomIndex]);
            players.Remove(players[randomIndex]);
            n--;
        }

        foreach (var player in shuffledPlayers)
        {
            PlayerMovement2D playerComponent = player.GetComponent<PlayerMovement2D>();
            if (player.GetComponentInParent<MainPlayerInfo>().isGhost.Value)
            {
                playerComponent.UpdatePlayerSpawnClientRpc(ghostSpawnPoints[ghostCount]);
                ghostCount++;
                continue;
            }

            playerComponent.UpdatePlayerSpawnClientRpc(playerSpawnPoint[playerCount]);
            playerCount++;
        }

        InvokeRpc();
        CurrentGameState.Value = LevelState.Starting;
    }

    public void SetCamera(Transform player)
    {
        playerCamera.Follow = player;
    }

    [Rpc(SendTo.Everyone)]
    private void ChallengeAnimationRpc()
    {
        coinCounterUI.SetActive(false);
        StartCoroutine(CameraAnimation());
    }

    [Rpc(SendTo.Everyone)]
    private void MinigameAnimationRpc()
    {
        StartCoroutine(ThreeTwoOneCoroutine());
    }

    private void RunTimer()
    {
        if (LevelTimer.Value < 1)
        {
            CurrentGameState.Value = LevelState.Ending;
            return;
        }

        LevelTimer.Value -= Time.deltaTime;
    }

    [Rpc(SendTo.Everyone)]
    private void InvokeRpc()
    {
        loadPlayerText.gameObject.SetActive(false);
        BlackScreen.instance.screenFade.StartFade(BlackScreen.instance.transform, false);
        OnPlayerValueChange?.Invoke();
    }

    private void TimerVisual()
    {
        var timeInMinutes = Mathf.FloorToInt(LevelTimer.Value / 60);
        var timeInSeconds = Mathf.FloorToInt(LevelTimer.Value - timeInMinutes * 60);
        string timerText = string.Format("Time remaining\n{0:00}:{1:00}", timeInMinutes, timeInSeconds);
        timerVisual.text = timerText;
    }

    [Rpc(SendTo.Everyone)]
    private void EndingStateRpc()
    {
        StartCoroutine(EndingAnimation());
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
        StartCoroutine(ThreeTwoOneCoroutine());
    }

    private IEnumerator EndingAnimation()
    {
        float timeToWait = 0;
        switch (currentLevelType)
        {
            case LevelType.Challenge:
                endingCamera.Priority = 1;
                playerCamera.Priority = 0;
                timeToWait = cameraAnimationSeconds > 1 ? cameraAnimationSeconds + 1f : 2f;
                break;
            case LevelType.Minigame:
                timeToWait = 2f;
                break;
        }
        overUI.SetActive(true);
        yield return new WaitForSeconds(timeToWait / 2);

        // (1 / timeToWait) saa nopeuden sekunneista.
        BlackScreen.instance.screenFade.StartFade(BlackScreen.instance.transform, true, 1 / (timeToWait / 2));
        BlackScreen.instance.screenFade.StartFade(overUI.transform, false, 1 / (timeToWait / 2));
        BlackScreen.instance.screenFade.StartFade(overUI.transform.GetChild(0), false, 1 / (timeToWait / 2));
        yield return new WaitForSeconds(timeToWait);

        if (!IsServer)
        {
            yield break;
        }

        NetworkManager.SceneManager.LoadScene("MainBoard", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    private IEnumerator ThreeTwoOneCoroutine()
    {
        goTimerUI.SetActive(true);
        goTimerText.text = "3";
        yield return new WaitForSeconds(1);
        goTimerText.text = "2";
        yield return new WaitForSeconds(1);
        goTimerText.text = "1";
        yield return new WaitForSeconds(1);
        goTimerText.text = "Go!";
        yield return new WaitForSeconds(1);
        goTimerUI.SetActive(false);

        if (IsServer)
        {
            coinCounter.GetAllPlayerDataRpc();
            CurrentGameState.Value = LevelState.InProgress;
        }
    }

    [Rpc(SendTo.Server)]
    public void LoadPlayerServerRpc()
    {
        playersLoaded.Value++;
    }

    [Rpc(SendTo.Server)]
    public void PlayAnimationServerRpc(ulong objectId, string animation)
    {
        var animationObject = NetworkManager.SpawnManager.SpawnedObjects[objectId];
        animationObject.GetComponent<Animator>().Play(animation);
    }
}
