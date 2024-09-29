using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class NetworkManagerUI : NetworkBehaviour
{
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private Button startButton;

    private void Awake()
    {
        hostButton.onClick.AddListener(() => { NetworkManager.Singleton.StartHost(); });
        clientButton.onClick.AddListener(() => { NetworkManager.Singleton.StartClient(); });
        startButton.onClick.AddListener(() => { NetworkManager.SceneManager.LoadScene("GameScene", LoadSceneMode.Single); });
    }
}
