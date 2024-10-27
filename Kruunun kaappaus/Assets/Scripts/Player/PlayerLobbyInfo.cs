using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class PlayerLobbyInfo : MonoBehaviour
{
    public Dictionary<string, PlayerDataObject> playerData;
    public bool IsHost { get; private set; }
    [SerializeField] private GameObject spriteSelectionMenu;
    [SerializeField] private GameObject spriteShowcase;
    [SerializeField] private GameObject colorButtonPrefab;
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private Image iconImage;
    [field: SerializeField] public Button SpriteSelectionButton { get; private set; }
    public TextMeshProUGUI hostText;

    private int selectedColorIndex;

    private void Awake()
    {
        SpriteSelectionButton.onClick.AddListener(() => { SpriteSelectionScreen(); });
    }
    void Start()
    {
        selectedColorIndex = int.Parse(playerData["PlayerColor"].Value);
        SpawnColorButtons();
        playerNameText.text = playerData["PlayerName"].Value;
        iconImage.sprite = MainMenuUI.instance.PlayerIcons[int.Parse(playerData["PlayerIconIndex"].Value)];
        iconImage.color = GetColor(selectedColorIndex);
    }

    public void SetPlayerAsHost(bool apply)
    {
        switch (apply)
        {
            case true:
                IsHost = true;
                hostText.gameObject.SetActive(true);
                break;
            case false:
                IsHost = false;
                hostText.gameObject.SetActive(false);
                break;
        }
    }

    public void UpdateSprite()
    {
        iconImage.sprite = MainMenuUI.instance.PlayerIcons[int.Parse(playerData["PlayerIconIndex"].Value)];
        iconImage.color = GetColor(int.Parse(playerData["PlayerColor"].Value));
    }
    private void SpriteSelectionScreen()
    {
        bool activate = !spriteSelectionMenu.activeSelf;

        switch (activate)
        {
            case true:
                UpdateSelectionMenu();
                break;
            case false:
                ClearSelectionMenu();
                break;
        }

        spriteSelectionMenu.SetActive(activate);
    }
    private void UpdateSelectionMenu()
    {
        foreach (var existingSprite in MainMenuUI.instance.PlayerIcons)
        {
            int spriteIndex = Array.IndexOf(MainMenuUI.instance.PlayerIcons, existingSprite);
            // jos sprite on jo valittu nii ei näytetä sitä
            if (spriteIndex == int.Parse(playerData["PlayerIconIndex"].Value))
            {
                Debug.Log(spriteIndex);
                continue;
            }

            var spriteObject = Instantiate(spriteShowcase, spriteSelectionMenu.transform);
            var spriteComponent = spriteObject.transform.GetChild(0).GetComponent<Image>();
            spriteComponent.sprite = existingSprite;
            spriteComponent.color = GetColor(int.Parse(playerData["PlayerColor"].Value));

            // Jos pelaaja valitsee sen spriten
            spriteObject.GetComponent<Button>().onClick.AddListener(() =>
            {
                LobbyUI.instance.SelectSprite(transform.name, selectedColorIndex, spriteIndex);
                SpriteSelectionScreen();

                // Päivitää itteleni sprite ja värin
                iconImage.sprite = MainMenuUI.instance.PlayerIcons[spriteIndex];
                iconImage.color = GetColor(selectedColorIndex);
            });
        }
    }
    private void ClearSelectionMenu()
    {
        foreach (Transform child in spriteSelectionMenu.transform)
        {
            if (child == spriteSelectionMenu.transform.GetChild(0))
            {
                continue;
            }
            Destroy(child.gameObject);
        }
    }
    private Color GetColor(int colorIndex)
    {
        var newColor = (Colors)colorIndex;
        Color assignedColor;
        switch (newColor)
        {
            case Colors.White:
                assignedColor = Color.white;
                break;
            case Colors.Red:
                assignedColor = Color.red;
                break;
            case Colors.Green:
                assignedColor = Color.green;
                break;
            case Colors.Blue:
                assignedColor = Color.blue;
                break;
            case Colors.Magenta:
                assignedColor = Color.magenta;
                break;
            default:
                assignedColor = Color.white;
                break;
        }

        return assignedColor;
    }

    private void SpawnColorButtons()
    {
        foreach (var colors in Enum.GetValues(typeof(Colors)))
        {
            Color currentColor = GetColor((int)colors);
            var colorButton = Instantiate(colorButtonPrefab, spriteSelectionMenu.transform.GetChild(0));
            colorButton.GetComponent<Image>().color = currentColor;

            colorButton.GetComponent<Button>().onClick.AddListener(() => { UpdateSelectionMenuColors((int)colors); });
        }
    }
    private void UpdateSelectionMenuColors(int colorIndex)
    {
        var addedColor = GetColor(colorIndex);
        foreach (Transform child in spriteSelectionMenu.transform)
        {
            if (child == spriteSelectionMenu.transform.GetChild(0))
            {
                continue;
            }
            // tyhmin juttu ikin
            var childOfChild = child.transform.GetChild(0);
            childOfChild.name = colorIndex.ToString();
            childOfChild.GetComponent<Image>().color = addedColor;
            selectedColorIndex = colorIndex;
        }
    }
} 
