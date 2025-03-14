using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public class MinigameTile : BoardTile
{
    [SerializeField] private TileMapListScriptable levelList;

    public override void SetupTile()
    {
        tileSprite = GameManager.instance.minigameTile.tileSprite;
        tileName = GameManager.instance.minigameTile.name;
        minimapSprite = GameManager.instance.minigameTile.minimapSprite;

        var minigameTileComponent = GameManager.instance.minigameTile.GetComponent<MinigameTile>();
        levelList = minigameTileComponent.levelList;

        anim.Play("MinigameTile_Idle");
    }
    public override void InvokeTile()
    {
        BoardPath.instance.SetCameraPositionAndActiveRpc(true);
        GameManager.instance.EnableAnimationRpc(true);
        SelectRandomChallenge(transform.GetSiblingIndex());
    }

    public void SelectRandomChallenge(int currentIndex)
    {
        int playersOnTile = 0;
        List<int> ghosts = new List<int>();
        List<int> players = new List<int>();

        for (int i = 0; i < GameManager.instance.availablePlayers.Count; i++)
        {
            if (currentIndex == GameManager.instance.availablePlayers[i].GetComponentInParent<MainPlayerInfo>().currentBoardPosition.Value)
            {
                playersOnTile++;
                players.Add(i);
            }
            else
            {
                ghosts.Add(i);
            }
        }

        GameManager.instance.FromGhostToPlayerServerRpc(ghosts.ToArray(), players.ToArray());

        string newScene = string.Empty;
        bool foundMatch = false;
        foreach (var list in levelList.mapSettings)
        {
            if (list.playerLimit == playersOnTile)
            {
                newScene = list.sceneList[Random.Range(0, list.sceneList.Length)];
                foundMatch = true;
                break;
            }
        }

        if (!foundMatch)
        {
            Debug.LogError("Didn't find any match for current players! Taking last list");
            var lastList = levelList.mapSettings[levelList.mapSettings.Length];
            newScene = lastList.sceneList[Random.Range(0, lastList.sceneList.Length)];
        }

        GameManager.instance.LoadSceneRpc(newScene);
    }

    public void SelectCustomMinigame(int playerAmount)
    {
        List<int> ghosts = new();
        List<int> players = new();
        List<int> potentialPlayers = new();

        for (int i = 0; i < GameManager.instance.availablePlayers.Count; i++)
        {
            if (GameManager.instance.currentPlayer == GameManager.instance.availablePlayers[i])
            {
                players.Add(i);
                continue;
            }

            potentialPlayers.Add(i);
        }

        for (int i = 0; i < playerAmount-1; i++)
        {
            if (i > potentialPlayers.Count || potentialPlayers.Count < 1)
            {
                break;
            }

            var randomIndex = Random.Range(0, potentialPlayers.Count);
            players.Add(potentialPlayers[randomIndex]);
            potentialPlayers.Remove(potentialPlayers[randomIndex]);
        }

        foreach (var leftover in potentialPlayers)
        {
            ghosts.Add(leftover);
        }

        GameManager.instance.FromGhostToPlayerServerRpc(ghosts.ToArray(), players.ToArray());


        string newScene = string.Empty;
        bool foundMatch = false;
        foreach (var list in levelList.mapSettings)
        {
            if (list.playerLimit == playerAmount)
            {
                newScene = list.sceneList[Random.Range(0, list.sceneList.Length)];
                foundMatch = true;
                break;
            }
        }

        if (!foundMatch)
        {
            Debug.LogError("Didn't find any match for current players! Taking last list");
            var lastList = levelList.mapSettings[levelList.mapSettings.Length];
            newScene = lastList.sceneList[Random.Range(0, lastList.sceneList.Length)];
        }

        GameManager.instance.LoadSceneRpc(newScene);
    }
}
