using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class CameraEffects : MonoBehaviour
{
    public static CameraEffects instance;
    [SerializeField] private CinemachineBasicMultiChannelPerlin shakeComponent;
    [SerializeField] private float shakeIntensity;
    [SerializeField] private float shakeTimeSeconds;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public void ShakeCamera()
    {
        StartCoroutine(CameraShaking());
    }

    private IEnumerator CameraShaking()
    {
        shakeComponent.AmplitudeGain = shakeIntensity;
        yield return new WaitForSeconds(shakeTimeSeconds);
        shakeComponent.AmplitudeGain = 0;
    }
}
