using UnityEngine;

public class DiceRollEvent : MonoBehaviour
{
    public void AnimationEvent()
    {
        StartCoroutine(GameManager.instance.AnimationEventCoroutine());
    }
}
