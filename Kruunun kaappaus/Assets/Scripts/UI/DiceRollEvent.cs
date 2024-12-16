using UnityEngine;

public class DiceRollEvent : MonoBehaviour
{
    public void AnimationEvent()
    {
        StartCoroutine(GameManager.instance.AnimationEventCoroutine());
    }

    public void AssistantAnimationStartEvent()
    {
        BoardUIManager.instance.rerollButton.gameObject.SetActive(true);
        BoardUIManager.instance.confirmRollButton.gameObject.SetActive(true);
        GameManager.instance.DestroyActiveDice();

        BoardUIManager.instance.rerollButton.interactable = false;
        BoardUIManager.instance.confirmRollButton.interactable = false;
    }

    public void AssistantAnimationEndEvent()
    {
        BoardUIManager.instance.rerollButton.interactable = BoardUIManager.instance.localPlayerTurn;
        BoardUIManager.instance.confirmRollButton.interactable = BoardUIManager.instance.localPlayerTurn;
    }
}
