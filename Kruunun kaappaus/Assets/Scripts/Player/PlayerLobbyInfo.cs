using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerLobbyInfo : MonoBehaviour
{
    public string playerName;
    [SerializeField] private TextMeshProUGUI playerNameText;
    public TextMeshProUGUI hostText;


    void Start()
    {
        playerNameText.text = playerName;
    }
}
