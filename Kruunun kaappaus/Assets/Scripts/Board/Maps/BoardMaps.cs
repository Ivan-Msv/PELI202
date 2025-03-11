using System;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.UI;


[CreateAssetMenu(menuName = "Board/Maplist")]
public class MapList : ScriptableObject
{
    public MapSettings[] mapList;
}

[Serializable]
public struct MapSettings
{
    public string sceneName;
    public Sprite mapImage;
}

public class BoardMaps : MonoBehaviour
{
    [SerializeField] private MapList mapList;
    [SerializeField] private GameObject mapSelectionPanel;
    [SerializeField] private GameObject mapPrefab;
    [SerializeField] private Toggle randomizeTilesToggle;
    [SerializeField] private int selectedMapIndex;
    [SerializeField] private bool randomizeMap;

    void Awake()
    {
        for (int i = 0; i < mapList.mapList.Length; i++)
        {
            var mapObject = Instantiate(mapPrefab, mapSelectionPanel.transform);
            var index = i;
            mapObject.GetComponentInChildren<Button>().onClick.AddListener(() => { UpdateSelectedIndex(index); });
            mapObject.GetComponent<SpriteRenderer>().sprite = mapList.mapList[i].mapImage;
        }

        randomizeTilesToggle.isOn = randomizeMap;
        randomizeTilesToggle.onValueChanged.AddListener(enabled => { randomizeMap = enabled; });
    }

    private void UpdateSelectedIndex(int mapIndex)
    {
        selectedMapIndex = mapIndex;

        foreach (Transform child in mapSelectionPanel.transform)
        {
            bool isSelected = mapSelectionPanel.transform.GetChild(mapIndex) == child;
            child.GetComponentInChildren<Outline>().enabled = isSelected;
        }
    }

    public string GetCurrentMap()
    {
        return mapList.mapList[selectedMapIndex].sceneName;
    }

    public bool RandomizeMap()
    {
        return randomizeMap;
    }
}
