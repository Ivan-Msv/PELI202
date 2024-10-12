using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine;

public class PlayerManager : NetworkBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        StartConnection();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // testaa ping
        }
    }

    private void StartConnection()
    {
        var allPlayers = GameObject.FindObjectsOfType<PlayerLobbyInfo>();
        var currentPlayer = allPlayers.FirstOrDefault(player => player.name == AuthenticationService.Instance.PlayerId);

        if (currentPlayer.isHost)
        {
            NetworkManager.StartHost();
            Debug.Log("Started host");
        }
        else
        {
            NetworkManager.StartClient();
            Debug.Log("Started client");
        }

        Destroy(allPlayers[0].transform.parent.gameObject);
    }
}
