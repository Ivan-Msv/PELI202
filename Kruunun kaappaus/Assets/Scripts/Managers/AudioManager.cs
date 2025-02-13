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
    [SerializeField] private float soundVolume = 0.5f;
    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 0.5f;

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
    private void Update()
    {
        instance.musicSource.volume = musicVolume;
    }
    public void PlayMusic(MusicType music)
    {
        if (NetworkManager.Singleton.IsConnectedClient || NetworkManager.Singleton.IsServer)
        {
            PlayMusicRpc(music);
        }
        else
        {
            PlayMusicLocal(music);
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
        instance.audioSource.PlayOneShot(instance.soundList[(int)sound], soundVolume);
    }
    public void PlaySoundLocal(SoundType sound)
    {
        //Debug.Log("play sound local");
        instance.audioSource.PlayOneShot(instance.soundList[(int)sound], soundVolume);
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
        objectAudio.volume = soundVolume;

        objectAudio.Play();
        Destroy(soundObject.gameObject, objectAudio.clip.length * ((Time.timeScale < 0.01f) ? 0.01f : Time.timeScale));
    }
    
    [Rpc(SendTo.Everyone)]
    public void PlayMusicRpc(MusicType music)
    {
        instance.musicSource.loop = true;
        instance.musicSource.clip = musicList[(int)music];

        instance.musicSource.Play();
    }
    public void PlayMusicLocal(MusicType music)
    {
        instance.musicSource.loop = true;
        instance.musicSource.clip = musicList[(int)music];
        
        instance.musicSource.Play();
    }
    public void SoundVolumeSliders(float soundVolume )
    {
        this.soundVolume = soundVolume;
        
    }
    public void MusicVolumeSliders(float musicVolume)
    {
        this.musicVolume = musicVolume;
    }
}
