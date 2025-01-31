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
    FootSteps,
    BoardSelect,
    Explosion,
    Cannon
}
public class AudioManager : NetworkBehaviour
{
    [SerializeField] private AudioClip[] soundList;

    public static AudioManager instance;
    public AudioSource audioSource;
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
        audioSource = GetComponent<AudioSource>();
    }
    [Rpc(SendTo.Everyone)]
    public void PlaySoundRpc(SoundType sound)
    {
        //instance.audioSource.PlayOneShot(instance.soundList[(int)sound]/*, volume*/);
        instance.audioSource.PlayOneShot(instance.soundList[(int)sound]);
    }
    [Rpc(SendTo.Everyone)]
    public void PlaySoundAtPosRpc(SoundType sound, Vector3 pos)
    {
        AudioSource.PlayClipAtPoint(instance.soundList[(int)sound], pos );
    }

    public void SoundVolume(float volume)
    {
        audioSource.volume = volume;
    }
}
