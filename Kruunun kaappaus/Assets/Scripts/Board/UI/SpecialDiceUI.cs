using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpecialDiceUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI selectedText;
    [SerializeField] private Image diceIcon;

    public void AddEvent()
    {
        UpdateIcon(0, BoardUIManager.instance.localParent.specialDiceIndex.Value);
        BoardUIManager.instance.localParent.specialDiceIndex.OnValueChanged += UpdateIcon;
    }

    private void OnDisable()
    {
        BoardUIManager.instance.localParent.specialDiceIndex.OnValueChanged -= UpdateIcon;
    }

    public void OnButtonClick()
    {
        if (BoardUIManager.instance.localParent.specialDiceIndex.Value == 0)
        {
            return;
        }

        if (GameManager.instance.currentState.Value == BoardState.PlayerMoving)
        {
            return;
        }

        // Laittaa boolean vastakohtaan
        BoardUIManager.instance.localParent.specialDiceEnabled.Value = !BoardUIManager.instance.localParent.specialDiceEnabled.Value;

        switch (BoardUIManager.instance.localParent.specialDiceEnabled.Value)
        {
            case true:
                selectedText.gameObject.SetActive(true);
                break;
            case false:
                selectedText.gameObject.SetActive(false);
                break;
        }
    }
    private void UpdateIcon(int previousValue, int newValue)
    {
        var newDiceIcon = GameManager.instance.GetDiceFromIndex(newValue).image;
        if (newValue == 0)
        {
            diceIcon.color = new Color(0, 0, 0, 0);
            selectedText.gameObject.SetActive(false);
            return;
        }

        diceIcon.color = new Color(1, 1, 1, 1);
        diceIcon.sprite = newDiceIcon;
    }
}
