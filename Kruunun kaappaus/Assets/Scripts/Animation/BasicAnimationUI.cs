using System.Collections;
using UnityEngine;

public class BasicAnimationUI : MonoBehaviour
{
    [SerializeField] private Animator anim;
    [SerializeField] private bool activateOnEnable;

    private void OnEnable()
    {
        if (!activateOnEnable) { return; }

        anim.Play("Activate");
    }


    // If you want to start some kind of animation, not sure if I will ever use this though
    public void PlayCustomAnimation(string animation)
    {
        anim.Play(animation);
    }

    // I thought of this when trying to make unity animations, even if I hide it behind other UI its still not "disabled"
    public void DisableUI(bool mirrorPrevious = true)
    {
        // If you have scripts that try to disable multiple elements that are not necessarily active, return
        if (!isActiveAndEnabled) { return; }

        // I copied this from chatmanager (which I did first) so for info on how it works
        // Just check whatever I wrote there

        var animatorState = anim.GetCurrentAnimatorStateInfo(0);
        var startLength = 1 - animatorState.normalizedTime;

        // If animation isn't mirrored by default (like toggle enable/disable) you can turn it off here
        if (animatorState.normalizedTime >= 1 || !mirrorPrevious)
        {
            startLength = 0;
        }

        StartCoroutine(DisableCoroutine(startLength));
    }

    private IEnumerator DisableCoroutine(float startLength)
    {
        anim.Play("Deactivate", 0, startLength);

        // Wait one frame to ensure the animator actually seeing the playback
        yield return null;

        yield return new WaitForSeconds(anim.GetCurrentAnimatorStateInfo(0).length);
        gameObject.SetActive(false);
    }
}
