using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine;

public class PlayerManager : NetworkBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log(AuthenticationService.Instance.PlayerName);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
