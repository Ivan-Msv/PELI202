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
    LobbyMusic2
}
public class AudioManager : NetworkBehaviour
{
    public static AudioManager instance;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioSource soundAtPositionPrefab;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip[] soundList;
    [SerializeField] private AudioClip[] musicList;

    [Header("Audio Volume")]
    [Range(0f, 1f)]
    [field: SerializeField] public float MusicVolume { get; private set; } = 0.5f;
    [Range(0f, 1f)]
    [field: SerializeField] public float SoundVolume { get; private set; } = 0.5f;

    [SerializeField] private bool musicMute;
    [SerializeField] private bool soundMute;

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

    public void PlayMusicLocal(MusicType music)
    {
        instance.musicSource.loop = true;
        instance.musicSource.clip = musicList[(int)music];
        
        instance.musicSource.Play();
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
