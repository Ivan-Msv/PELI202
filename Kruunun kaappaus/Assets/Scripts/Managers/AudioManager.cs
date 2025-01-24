using Unity.Netcode;
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
    FootSteps
}
public class AudioManager : NetworkBehaviour
{
    [SerializeField] private AudioClip[] soundList;

    private static AudioManager instance;
    private AudioSource audioSource;
    private void Awake()
    {
        
        if (instance == null) { 
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
        audioSource = GetComponent<AudioSource>();
    }
    public static void PlaySound(SoundType sound, float volume = 1)
    {
        instance.audioSource.PlayOneShot(instance.soundList[(int)sound], volume);
    }
}
