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
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private Image iconImage;
    [field: SerializeField] public Button SpriteSelectionButton { get; private set; }
    public TextMeshProUGUI hostText;
    private void Awake()
    {
        SpriteSelectionButton.onClick.AddListener(() => { SpriteSelectionScreen(); });
    }
    void Start()
    {
        foreach (var existingSprite in MainMenuUI.instance.PlayerIcons)
        {
            var spriteObject = Instantiate(spriteShowcase, spriteSelectionMenu.transform);
            spriteObject.transform.GetChild(0).GetComponent<Image>().sprite = existingSprite;
            // laittaa nimen indexin perusteella
            int spriteIndex = Array.IndexOf(MainMenuUI.instance.PlayerIcons, existingSprite);

            spriteObject.GetComponent<Button>().onClick.AddListener(() =>
            {
                LobbyUI.instance.SelectSprite(transform.name, spriteIndex);
                iconImage.sprite = existingSprite;
            });
        }
        playerNameText.text = playerData["PlayerName"].Value;
        iconImage.sprite = MainMenuUI.instance.PlayerIcons[int.Parse(playerData["PlayerIconIndex"].Value)];
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
        spriteSelectionMenu.SetActive(activate);
    }
} 
