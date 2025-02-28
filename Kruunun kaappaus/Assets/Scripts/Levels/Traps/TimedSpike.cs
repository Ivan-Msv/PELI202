using Unity.Netcode;
using UnityEngine;

public class TimedSpike : NetworkBehaviour
{
    [SerializeField] private Animator anim;
    [SerializeField] private float tickRateSeconds;
    [SerializeField] private int segments;
    [SerializeField] private int currentSegment;
    private float tickTimer;

    [SerializeField] private int activateSegment, deactivateSegment;

    private void OnValidate()
    {
        // This prevents animations from getting overlapped

        float maxLength = 0;
        foreach (var clip in anim.runtimeAnimatorController.animationClips)
        {
            maxLength = clip.length > maxLength ? clip.length : maxLength;
        }
        tickRateSeconds = Mathf.Clamp(tickRateSeconds, maxLength / segments, Mathf.Infinity);
    }

    private void Update()
    {
        if (!IsServer) { return; }

        tickTimer += Time.deltaTime;
        if (tickTimer >= tickRateSeconds)
        {
            currentSegment %= segments;
            currentSegment++;
            tickTimer = 0;

            UpdateSpike();
        }
    }

    private void UpdateSpike()
    {
        if (currentSegment != activateSegment && currentSegment != deactivateSegment)
        {
            return;
        }

        if (currentSegment == activateSegment)
        {
            anim.Play("Spike_Activate");
        }
        else
        {
            anim.Play("Spike_Deactivate");
        }
    }
}
