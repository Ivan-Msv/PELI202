using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class ChallengeTile : BoardTile
{
    [SerializeField] private TileMapListScriptable levelList;

    public override void SetupTile()
    {
        tileSprite = GameManager.instance.challengeTile.tileSprite;
        tileName = GameManager.instance.challengeTile.name;
        minimapSprite = GameManager.instance.challengeTile.minimapSprite;

        var challengeTileComponent = GameManager.instance.challengeTile.GetComponent<ChallengeTile>();
        levelList = challengeTileComponent.levelList;

        anim.Play("CrownTile_Switch");
    }

    public override void InvokeTile()
    {
        var emptyTiles = BoardPath.instance.tiles.FindAll(tile => tile.GetComponent<EmptyTile>());
        var thisIndex = transform.GetSiblingIndex();

        int randomIndex = Random.Range(0, emptyTiles.Count);
        var newIndex = BoardPath.instance.tiles.IndexOf(emptyTiles[randomIndex]);

        BoardPath.instance.TileAnimation(thisIndex, newIndex);
    }

    public void SelectRandomChallenge(int currentIndex)
    {
        int playersOnTile = 0;
        List<int> ghosts = new();
        List<int> players = new();

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

    public void SelectCustomChallenge(int playerAmount)
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

        for (int i = 0; i < playerAmount - 1; i++)
        {
            if (potentialPlayers.Count < 1)
            {
                Debug.LogError("Did not find any players, are you playing alone...?");
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
