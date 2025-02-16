using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public enum SoundType
{
    Open,
    Click,
    Close,
    Error,
    Rolldice,
    Jump,
    CoinPickUp,
    CrownPickUp,
    FootSteps,
    BoardSelect,
    Explosion,
    Cannon
}
public enum MusicType
{
    LobbyMusic,
    BoardMusic,
    MinigameMusic,
    ChallengeMusic
}
public class AudioManager : NetworkBehaviour
{
    public static AudioManager instance;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioSource soundAtPositionPrefab;
    [SerializeField] private AudioClip[] soundList;
    [SerializeField] private AudioClip[] musicList;

    [Header("Music Settings")]
    [SerializeField] private MusicType currentMusicType;
    [SerializeField] private float musicBlendSpeed;
    private AudioSource[] musicSources;

    [Header("Audio Volume")]
    [SerializeField] private bool musicMute;
    [SerializeField] private bool soundMute;
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
        CreateAndStartMusic();
    }

    private void Update()
    {
        BlendMusic();
    }

    public void ChangeMusic(MusicType type)
    {
        currentMusicType = type;
    }

    private void CreateAndStartMusic()
    {
        musicSources = new AudioSource[musicList.Length];
        var musicContainer = new GameObject("Music Container");
        musicContainer.transform.parent = transform;

        for (int i = 0; i < musicSources.Length; i++)
        {
            var musicObject = new GameObject($"{(MusicType)i}");
            musicObject.transform.parent = musicContainer.transform;
            musicSources[i] = musicObject.AddComponent<AudioSource>();

            musicSources[i].clip = musicList[i];
            musicSources[i].loop = true;

            musicSources[i].volume = 0;
            musicSources[i].Play();
        }
    }

    private void BlendMusic()
    {
        for (int i = 0; i < musicSources.Length; i++)
        {
            musicSources[i].mute = musicMute;

            // Blend the volume based on current music type
            if ((MusicType)i == currentMusicType)
            {
                musicSources[i].volume = Mathf.Lerp(musicSources[i].volume, MusicVolume, musicBlendSpeed * Time.deltaTime);
            }
            else
            {
                musicSources[i].volume = Mathf.Lerp(musicSources[i].volume, 0, musicBlendSpeed * Time.deltaTime);

                // Fixes weird lerping at the end
                if (musicSources[i].volume < 0.1f)
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
        instance.audioSource.PlayOneShot(instance.soundList[(int)sound], soundMute == enabled ? 0 : SoundVolume);
    }
    public void PlaySoundLocal(SoundType sound)
    {
        //Debug.Log("play sound local");
        instance.audioSource.PlayOneShot(instance.soundList[(int)sound], soundMute == enabled ? 0 : SoundVolume);
    }

    /// <param name="parent">Whether to parent the sound to a gameobject or not</param>
    [Rpc(SendTo.Everyone)]
    public void PlaySoundAtPositionRpc(SoundType sound, ulong soundOriginId, bool parent)
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
        objectAudio.volume = soundMute == enabled ? 0 : SoundVolume;

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
        objectAudio.volume = soundMute == enabled ? 0 : SoundVolume;

        objectAudio.Play();
        Destroy(soundObject.gameObject, objectAudio.clip.length * ((Time.timeScale < 0.01f) ? 0.01f : Time.timeScale));
    }

    public void SetSoundVolume(float volume)
    {
        SoundVolume = volume;
    }

    public void SetMusicVolume(float volume)
    {
        MusicVolume = volume;
    }

    public void MuteSound(bool enable)
    {
        soundMute = enable;
    }

    public void MuteMusic(bool enable)
    {
        musicMute = enable;
    }
}
