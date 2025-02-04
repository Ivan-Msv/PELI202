using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

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
public class AudioManager : NetworkBehaviour
{
    public static AudioManager instance;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioSource soundAtPositionPrefab;
    [SerializeField] private AudioClip[] soundList;

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

    [Rpc(SendTo.Everyone)]
    public void PlaySoundRpc(SoundType sound)
    {
        //instance.audioSource.PlayOneShot(instance.soundList[(int)sound]/*, volume*/);
        instance.audioSource.PlayOneShot(instance.soundList[(int)sound]);
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

    public void VolumeSliders(float soundVolume, float musicVolume)
    {
        this.soundVolume = soundVolume;
        this.musicVolume = musicVolume;
    }
}
