using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class TimerStartBlock : NetworkBehaviour
{
    [SerializeField] private Animator[] timerBlocks;
    [SerializeField] private Animator[] pressurePlates;
    [SerializeField] private float givenTimeSeconds;

    private bool timerActive;

    public void TriggerTimer()
    {
        if (timerActive) { return; }
        foreach (var plate in pressurePlates)
        {
            plate.Play("PressurePlate_Activate");
        }

        StartCoroutine(TimerCoroutine());
    }

    private IEnumerator TimerCoroutine()
    {
        timerActive = true;

        foreach (var block in timerBlocks)
        {
            block.Play("Timer_Block_Activate");
        }

        // Takes first block... hopefully its not null or different animation length!
        while (!timerBlocks[0].GetCurrentAnimatorStateInfo(0).IsName("Timer_Block_Activate"))
        {
            yield return null; // Waits until the animator changed to activation
        }

        while (timerBlocks[0].GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
        {
            yield return null; // Waits until the animation has played
        }

        foreach (var block in timerBlocks)
        {
            block.SetFloat("SpeedMultiplier", 1 / givenTimeSeconds);
            block.Play("Timer_Block_Running");
        }


        // Does the same thing as above because for some reason
        // Just waiting for giventimeSeconds resulted in the animation sometimes being either too fast or too slow
        while (!timerBlocks[0].GetCurrentAnimatorStateInfo(0).IsName("Timer_Block_Running"))
        {
            yield return null;
        }

        while (timerBlocks[0].GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
        {
            yield return null;
        }

        timerActive = false;

        foreach (var block in timerBlocks)
        {
            block.Play("Timer_Block_Deactivate");
        }

        foreach (var plate in pressurePlates)
        {
            plate.Play("PressurePlate_Deactivate");
        }
    }
}
