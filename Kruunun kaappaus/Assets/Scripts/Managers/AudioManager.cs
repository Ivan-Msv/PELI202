using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering;

public enum SoundType
{
    MenuButtonHover,
    MenuButtonClick,
    MenuError,
    BoardRollDice,
    BoardShopPurchase,
    PlayerDeath,
    PlayerJump,
    CrownPickup,
    CoinPickup,
    Cannon,
    Explosion
}
public enum MusicType
{
    LobbyMusic,
    BoardMusic,
    MinigameMusic,
    ChallengeMusic
}
public enum MusicLayer
{
    LightLayer,
    MediumLayer,
    HeavyLayer
}

[Serializable]
public struct MusicStruct
{
    public MusicType Type;
    public AudioClip[] musicList;
}

public class AudioManager : NetworkBehaviour
{
    public static AudioManager instance;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioSource soundAtPositionPrefab;
    [SerializeField] private AudioClip[] soundList;
    [SerializeField] private MusicStruct[] musicTypeList;

    [Header("Music Settings")]
    [SerializeField] private MusicType currentMusicType;
    [SerializeField] private MusicLayer currentMusicLayer;
    [SerializeField] private float musicBlendSpeed;
    [SerializeField] private bool loadingScreen;
    [ReadOnlyInspector]
    [SerializeField] private AudioClip[] selectedMusicList;
    private AudioSource[] musicSources;
    public delegate void OnAudioLayerChangeDelegate(MusicLayer newLayer);
    public event OnAudioLayerChangeDelegate OnAudioLayerChanged;

    [Header("Audio Volume")]
    [field: SerializeField] public bool MusicMute { get; private set; }
    [field: SerializeField] public bool SoundMute { get; private set; }

    [Range(0f, 1f)]
    [field: SerializeField] public float MusicVolume { get; private set; } = 0.5f;
    [Range(0f, 1f)]
    [field: SerializeField] public float SoundVolume { get; private set; } = 0.5f;


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
        InitializeSettings();
        CreateAndStartMusic();
    }

    private void Update()
    {
        BlendMusic();
    }

    private void InitializeSettings()
    {
        MusicMute = DataSaving.instance.GetData<bool>("MusicMute");
        SoundMute = DataSaving.instance.GetData<bool>("SoundMute");
        MusicVolume = DataSaving.instance.GetData<float>("MusicVolume");
        SoundVolume = DataSaving.instance.GetData<float>("SoundVolume");
    }

    public void ChangeMusic(MusicType type, bool preserveTiming = false)
    {
        // To prevent song restarts
        if (currentMusicType == type)
        {
            return;
        }

        // Sets the current music type
        currentMusicType = type;

        // Gets the playback time so it doesn't reset on change
        var songTiming = musicSources[(int)currentMusicLayer].time;

        // Finds the corresponding musiclist for the type
        selectedMusicList = musicTypeList.FirstOrDefault(musicStruct => musicStruct.Type == currentMusicType).musicList;

        for (int i = 0; i < musicSources.Length; i++)
        {
            musicSources[i].clip = selectedMusicList[i];

            if (preserveTiming)
            {
                musicSources[i].time = songTiming;
            }

            musicSources[i].Play();
        }
    }

    public void EnableLoading(bool enable)
    {
        loadingScreen = enable;
    }

    public void ChangeMusicLayer(MusicLayer layer, bool invokeTrigger = true)
    {
        currentMusicLayer = layer;
        if (invokeTrigger)
        {
            OnAudioLayerChanged.Invoke(layer);
        }
    }

    public MusicLayer GetCurrentLayer()
    {
        return currentMusicLayer;
    }

    private void CreateAndStartMusic()
    {
        selectedMusicList = musicTypeList.FirstOrDefault(musicStruct => musicStruct.Type == currentMusicType).musicList;
        musicSources = new AudioSource[selectedMusicList.Length];
        var musicContainer = new GameObject("Music Container");
        musicContainer.transform.parent = transform;

        for (int i = 0; i < musicSources.Length; i++)
        {
            var musicObject = new GameObject($"{(MusicType)i}");
            musicObject.transform.parent = musicContainer.transform;
            musicSources[i] = musicObject.AddComponent<AudioSource>();

            musicSources[i].clip = selectedMusicList[i];
            musicSources[i].loop = true;

            musicSources[i].volume = 0;
            musicSources[i].Play();
        }
    }

    private void BlendMusic()
    {
        for (int i = 0; i < musicSources.Length; i++)
        {
            musicSources[i].mute = MusicMute;

            // Blend the volume based on current music type
            if ((MusicLayer)i == currentMusicLayer && !loadingScreen)
            {
                musicSources[i].volume = Mathf.Lerp(musicSources[i].volume, MusicVolume, musicBlendSpeed * Time.deltaTime);
            }
            else
            {
                musicSources[i].volume = Mathf.Lerp(musicSources[i].volume, 0, musicBlendSpeed * Time.deltaTime);

                // Fixes weird lerping at the end
                if (musicSources[i].volume < 0.01f)
                {
                    musicSources[i].volume = 0;
                }
            }

            // Clamp values to update the current volume value
            musicSources[i].volume = Mathf.Clamp(musicSources[i].volume, 0, MusicVolume);
        }
    }

    public void PlaySound(SoundType sound)
    {
        if (NetworkManager.Singleton.IsConnectedClient || NetworkManager.Singleton.IsServer)
        {
            PlaySoundRpc(sound);
        }
        else
        {
            PlaySoundLocal(sound);
        }
    }
    [Rpc(SendTo.Everyone)]
    public void PlaySoundRpc(SoundType sound)
    {
        //Debug.Log("play sound rpc");
        instance.audioSource.PlayOneShot(instance.soundList[(int)sound], SoundMute == enabled ? 0 : SoundVolume);
    }
    public void PlaySoundLocal(SoundType sound)
    {
        //Debug.Log("play sound local");
        instance.audioSource.PlayOneShot(instance.soundList[(int)sound], SoundMute == enabled ? 0 : SoundVolume);
    }

    /// <param name="parent">Whether to parent the sound to a gameobject or not</param>
    [Rpc(SendTo.Everyone)]
    public void PlaySoundAtObjectPositionRpc(SoundType sound, ulong soundOriginId, bool parent)
    {
        var originObject = NetworkManager.SpawnManager.SpawnedObjects[soundOriginId];

        AudioSource soundObject;
        switch (parent)
        {
            case true:
                soundObject = Instantiate(soundAtPositionPrefab, originObject.transform);
                break;
            case false:
                soundObject = Instantiate(soundAtPositionPrefab);
                soundObject.transform.position = originObject.transform.position;
                break;
        }

        soundObject.name = $"{originObject.transform.name}'s {sound} sound";
        var objectAudio = soundObject.GetComponent<AudioSource>();

        objectAudio.clip = soundList[(int)sound];
        objectAudio.volume = SoundVolume;
        objectAudio.mute = SoundMute;

        objectAudio.Play();
        Destroy(soundObject.gameObject, objectAudio.clip.length * ((Time.timeScale < 0.01f) ? 0.01f : Time.timeScale));
    }

    public void PlaySoundAtPositionRpc(SoundType sound, Vector2 position)
    {
        AudioSource soundObject;
        soundObject = Instantiate(soundAtPositionPrefab);
        soundObject.transform.position = position;

        soundObject.name = $"{sound} at {position}";
        var objectAudio = soundObject.GetComponent<AudioSource>();

        objectAudio.clip = soundList[(int)sound];
        objectAudio.volume = SoundVolume;
        objectAudio.mute = SoundMute;

        objectAudio.Play();
        Destroy(soundObject.gameObject, objectAudio.clip.length * ((Time.timeScale < 0.01f) ? 0.01f : Time.timeScale));
    }


    public void PlayLocalSoundAtPosition(SoundType sound, Transform originObject, bool parent)
    {
        AudioSource soundObject;
        switch (parent)
        {
            case true:
                soundObject = Instantiate(soundAtPositionPrefab, originObject.transform);
                break;
            case false:
                soundObject = Instantiate(soundAtPositionPrefab);
                soundObject.transform.position = originObject.transform.position;
                break;
        }

        soundObject.name = $"{originObject.transform.name}'s {sound} local sound";
        var objectAudio = soundObject.GetComponent<AudioSource>();

        objectAudio.clip = soundList[(int)sound];
        objectAudio.volume = SoundVolume;
        objectAudio.mute = SoundMute;

        objectAudio.Play();
        Destroy(soundObject.gameObject, objectAudio.clip.length * ((Time.timeScale < 0.01f) ? 0.01f : Time.timeScale));
    }

    public void SetSoundVolume(float volume)
    {
        SoundVolume = volume;
        DataSaving.instance.SetData("SoundVolume", volume);
    }

    public void SetMusicVolume(float volume)
    {
        MusicVolume = volume;
        DataSaving.instance.SetData("MusicVolume", volume);
    }

    public void MuteSound(bool enable)
    {
        SoundMute = enable;
        DataSaving.instance.SetData("SoundMute", enable);
    }

    public void MuteMusic(bool enable)
    {
        MusicMute = enable;
        DataSaving.instance.SetData("MusicMute", enable);
    }
}
