using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class PlayerLobbyInfo : MonoBehaviour
{
    public Dictionary<string, PlayerDataObject> playerData;
    public bool IsHost { get; private set; }
    [SerializeField] private GameObject spriteSelectionMenu;
    [SerializeField] private GameObject spriteShowcase;
    [SerializeField] private GameObject noColorButtonPrefab;
    [SerializeField] private GameObject colorButtonPrefab;
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private Image iconImage;
    [field: SerializeField] public Button SpriteSelectionButton { get; private set; }
    public TextMeshProUGUI hostText;

    private int selectedColorIndex;
    private int selectedPlayerIconIndex;

    private void Awake()
    {
        SpriteSelectionButton.onClick.AddListener(() => { SpriteSelectionScreen(); });
    }
    void Start()
    {
        selectedColorIndex = int.Parse(playerData["PlayerColor"].Value);
        playerNameText.text = playerData["PlayerName"].Value;
        selectedPlayerIconIndex = int.Parse(playerData["PlayerIconIndex"].Value);
        iconImage.sprite = MainMenuUI.instance.PlayerIcons[selectedPlayerIconIndex];
        SpawnColorButtons();
        UpdateSelectionMenuColors(selectedColorIndex);
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
            if (spriteIndex == selectedPlayerIconIndex)
            {
                continue;
            }

            var spriteObject = Instantiate(spriteShowcase, spriteSelectionMenu.transform);
            var spriteComponent = spriteObject.transform.GetChild(1).GetComponent<Image>();
            spriteComponent.sprite = existingSprite;

            // Jos pelaaja valitsee sen spriten
            spriteObject.GetComponent<Button>().onClick.AddListener(() =>
            {
                selectedPlayerIconIndex = spriteIndex;
                LobbyUI.instance.SelectSprite(transform.name, selectedColorIndex, spriteIndex);
                SpriteSelectionScreen();

                // Päivitää itteleni sprite ja värin
                iconImage.sprite = MainMenuUI.instance.PlayerIcons[selectedPlayerIconIndex];
            });
        }
    }
    private void ClearSelectionMenu()
    {
        foreach (Transform child in spriteSelectionMenu.transform)
        {
            if (child.name.Contains("sprite", StringComparison.OrdinalIgnoreCase))
            {
                Destroy(child.gameObject);
            }
        }
    }

    private void SpawnColorButtons()
    {
        foreach (var colors in Enum.GetValues(typeof(Colors)))
        {
            Color currentColor = MainMenuUI.GetColor((int)colors);
            if ((int)colors == (int)Colors.None)
            {
                var noColorButton = Instantiate(noColorButtonPrefab, spriteSelectionMenu.transform.GetChild(0));
                noColorButton.name = $"{(int)colors}";
                noColorButton.GetComponent<Button>().onClick.AddListener(() => { UpdateSelectionMenuColors((int)colors); });
            }
            else
            {
                var colorButton = Instantiate(colorButtonPrefab, spriteSelectionMenu.transform.GetChild(0));
                colorButton.GetComponent<Image>().color = currentColor;
                colorButton.name = $"{(int)colors}";

                colorButton.GetComponent<Button>().onClick.AddListener(() => { UpdateSelectionMenuColors((int)colors); });
            }
        }
    }
    private void UpdateSelectionMenuColors(int colorIndex)
    {
        selectedColorIndex = colorIndex;

        foreach (Transform child in spriteSelectionMenu.transform.GetChild(0).transform)
        {
            bool isSelected = child.name == colorIndex.ToString();
            child.GetComponent<Outline>().enabled = isSelected;
        }

        if (transform.name != AuthenticationService.Instance.PlayerId)
        {
            Destroy(spriteSelectionMenu);
            return;
        }
        LobbyUI.instance.SelectSprite(transform.name, selectedColorIndex, selectedPlayerIconIndex);
    }
} 
