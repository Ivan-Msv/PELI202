using NUnit.Framework;
using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public struct ItemPool
{
    public string poolName;
    public int maxSlotsPerPool;
    public ShopItem[] poolItems;
}

public class BoardShop : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject shopUI;
    [SerializeField] private RectTransform shopPanel;
    [SerializeField] private GameObject shopInfoPanel;
    [SerializeField] private GameObject shopCatalogueList;
    [SerializeField] private GameObject shopCheckingPanel;
    [SerializeField] private TextMeshProUGUI shopCheckingText;
    [Space]
    [SerializeField] private Image imagePreview;
    [SerializeField] private TextMeshProUGUI itemNamePreview;
    [SerializeField] private TextMeshProUGUI itemDescriptionPreview;
    [SerializeField] private Button returnButton;
    [SerializeField] private Button closeButton;

    [Header("Prefabs")]
    [SerializeField] private GameObject shopCataloguePrefab;
    [SerializeField] private ShopItemUI shopItemPrefab;

    [Header("Items")]
    [SerializeField] private ItemPool[] itemPools;

    private void Awake()
    {
        InitShop();
        closeButton.onClick.AddListener(() => { OpenStore(); });
        returnButton.onClick.AddListener(() => { OpenInfoTab(); });
    }

    private void InitShop()
    {
        foreach (var pool in itemPools)
        {
            var catalogue = Instantiate(shopCataloguePrefab, shopCatalogueList.transform);
            catalogue.name = pool.poolName;
        }

        UpdateItems();
    }
    public void UpdateItems()
    {
        for (int i = 0; i < shopCatalogueList.transform.childCount; i++)
        {
            // Poistetaan vanhat itemit
            foreach (Transform child in shopCatalogueList.transform.GetChild(i))
            {
                // Listan ensimmäinen objekti on teksti, ei poisteta sitä seuraavalla tavalla.
                if (child.transform.GetSiblingIndex() == 0)
                {
                    continue;
                }

                Destroy(child.gameObject);
            }

            List<ShopItem> addedItems = new();
            // Lisätään uudet itemit
            ItemRoll(i, addedItems);
        }
    }

    private void ItemRoll(int i, List<ShopItem> addedItems)
    {
        for (int j = 0; j < itemPools[i].maxSlotsPerPool; j++)
        {
            bool validItem = false;
            int maximumTries = 10;
            while (!validItem)
            {
                var randomIndex = UnityEngine.Random.Range(0, itemPools[i].poolItems.Length);
                var newItem = itemPools[i].poolItems[randomIndex];

                if (maximumTries < 1)
                {
                    Debug.LogError("Reached maximum tries and no substitute found, adding anyway.");
                    ItemInstantiation(i, newItem);
                    break;
                }

                if (addedItems.Contains(newItem))
                {
                    maximumTries--;
                    continue;
                }

                addedItems.Add(newItem);
                ItemInstantiation(i, newItem);
                validItem = true;
            }
        }
    }

    private void ItemInstantiation(int i, ShopItem newItem)
    {
        ShopItemUI newItemPrefab = Instantiate(shopItemPrefab, shopCatalogueList.transform.GetChild(i));
        newItemPrefab.itemImage.sprite = newItem.itemSprite;
        newItemPrefab.cost = newItem.itemCost;

        newItemPrefab.infoButton.onClick.AddListener(() => { OpenInfoTab(newItem); });
        newItemPrefab.purchaseButton.onClick.AddListener(() => { newItem.PurchaseItem(BoardUIManager.instance.localParent); });
    }

    public void OpenStore()
    {
        shopInfoPanel.SetActive(false);
        shopPanel.gameObject.SetActive(!shopUI.activeSelf);
        GameManager.instance.PlayerShopOpenRpc(!shopUI.activeSelf);
        shopUI.SetActive(!shopUI.activeSelf);
    }

    public void ShopOpenInfo(bool open)
    {
        BlackScreen.instance.screenFade.StartFade(shopCheckingPanel.transform, open);
        BlackScreen.instance.screenFade.StartFade(shopCheckingText.transform, open);
        shopCheckingText.text = string.Format(shopCheckingText.text, BoardUIManager.instance.CurrentTurnPlayerName);
    }

    public bool StoreOpen()
    {
        return shopUI.activeSelf;
    }

    private void OpenInfoTab(ShopItem itemInfo = null)
    {
        if (itemInfo == null)
        {
            shopPanel.gameObject.SetActive(true);
            shopInfoPanel.SetActive(false);
            closeButton.gameObject.SetActive(true);
            return;
        }

        shopPanel.gameObject.SetActive(false);
        shopInfoPanel.SetActive(true);
        closeButton.gameObject.SetActive(false);
        imagePreview.sprite = itemInfo.itemSprite;
        itemNamePreview.text = itemInfo.itemName;
        itemDescriptionPreview.text = itemInfo.itemDescription;
    }
}
