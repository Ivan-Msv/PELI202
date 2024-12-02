using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpecialDiceUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI selectedText;
    [SerializeField] private Image diceIcon;

    private void Start()
    {
        StartCoroutine(AddEventDelayed());
    }

    private IEnumerator AddEventDelayed()
    {
        yield return new WaitForSeconds(3);
        Debug.Log("Added delayed event.");
        BoardUIManager.instance.localParent.specialDiceIndex.OnValueChanged += UpdateIcon;
    }
    public void OnButtonClick()
    {
        switch (BoardUIManager.instance.localParent.specialDiceIndex.Value)
        {
            case 0:
                return;
            case (int)DiceIndex.DefaultDice:
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
        var newDiceIcon = GameManager.instance.GetDiceFromIndex(newValue).GetComponent<SpriteRenderer>().sprite;
        if (newValue == 0 || newValue == 1)
        {
            diceIcon.color = new Color(0, 0, 0, 0);
            selectedText.gameObject.SetActive(false);
            return;
        }

        diceIcon.color = new Color(1, 1, 1, 1);
        diceIcon.sprite = newDiceIcon;
    }
}
