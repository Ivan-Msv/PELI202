using Unity.Netcode;
using UnityEngine;

public enum SoundType
{
    Open,
    Click,
    Close,
    Error,
    Rolldice
}
public class AudioManager : NetworkBehaviour
{
    [SerializeField] private AudioClip[] soundList;

    private static AudioManager instance;
    private AudioSource audioSource;
    private void Awake()
    {
        if (instance == null) { instance = this; }
    }
    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }
    public static void PlaySound(SoundType sound, float volume = 1)
    {
        instance.audioSource.PlayOneShot(instance.soundList[(int)sound], volume);
    }
}
