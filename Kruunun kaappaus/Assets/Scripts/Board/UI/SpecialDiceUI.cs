using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpecialDiceUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI selectedText;
    [SerializeField] private Image diceIcon;

    private void Awake()
    {
        StartCoroutine(AddEventCoroutine());
    }

    public IEnumerator AddEventCoroutine()
    {
        while (BoardUIManager.instance == null) { yield return null; }
        while (BoardUIManager.instance.localParent == null) { yield return null; }

        UpdateIcon(0, BoardUIManager.instance.localParent.specialDiceIndex.Value);
        BoardUIManager.instance.localParent.specialDiceIndex.OnValueChanged += UpdateIcon;
    }

    private void OnDisable()
    {
        BoardUIManager.instance.localParent.specialDiceIndex.OnValueChanged -= UpdateIcon;
    }

    public void OnButtonClick()
    {
        // If for some reason localparent still not initialized, return
        if (BoardUIManager.instance.localParent == null) { return; }

        if (BoardUIManager.instance.localParent.specialDiceIndex.Value == 0)
        {
            return;
        }

        if (GameManager.instance.currentState.Value == BoardState.PlayerMoving)
        {
            return;
        }

        var diceEnabled = BoardUIManager.instance.localParent.specialDiceEnabled.Value;
        // Laittaa boolean vastakohtaan
        BoardUIManager.instance.localParent.specialDiceEnabled.Value = !diceEnabled;
        selectedText.gameObject.SetActive(!diceEnabled);
    }
    private void UpdateIcon(int previousValue, int newValue)
    {
        var specialDiceEnabled = BoardUIManager.instance.localParent.specialDiceEnabled.Value;
        var newDiceIcon = GameManager.instance.GetDiceFromIndex(newValue).image;

        selectedText.gameObject.SetActive(specialDiceEnabled);
        if (newValue == 0)
        {
            diceIcon.color = new Color(0, 0, 0, 0);
            return;
        }

        diceIcon.color = new Color(1, 1, 1, 1);
        diceIcon.sprite = newDiceIcon;
    }
}
