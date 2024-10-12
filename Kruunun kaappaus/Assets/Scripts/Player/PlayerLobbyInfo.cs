using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerLobbyInfo : MonoBehaviour
{
    public string playerName;
    [field: SerializeField] public bool isHost { get; private set; }
    [SerializeField] private TextMeshProUGUI playerNameText;
    public TextMeshProUGUI hostText;
    void Start()
    {
        playerNameText.text = playerName;
    }

    public void SetPlayerAsHost(bool apply)
    {
        switch (apply)
        {
            case true:
                isHost = true;
                hostText.gameObject.SetActive(true);
                break;
            case false:
                isHost = false;
                hostText.gameObject.SetActive(false);
                break;
        }
    }
}
