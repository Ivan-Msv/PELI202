using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerLobbyInfo : MonoBehaviour
{
    public string playerName;

    void Start()
    {
        GetComponentInChildren<TextMeshProUGUI>().text = playerName;
    }
}
