﻿using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class ChallengeTile : BoardTile
{
    [SerializeField] private string[] sceneNames1;
    [SerializeField] private string[] sceneNames2;
    [SerializeField] private string[] sceneNames3;
    [SerializeField] private string[] sceneNames4;
    public override void SetupTile()
    {
        // Tajusin vast nyt kuink huonosti toi on tehty, vihaan sitä 👍 (4/12/2024)
        // Oisin voinu tehä struct tai jtn mut nyt mun pitää kärsii koska en oo vaihtamas sitä täs vaihees
        tileSprite = GameManager.instance.challengeTile.tileSprite;
        tileName = GameManager.instance.challengeTile.name;
        sceneNames1 = GameManager.instance.challengeTile.GetComponent<ChallengeTile>().sceneNames1;
        sceneNames2 = GameManager.instance.challengeTile.GetComponent<ChallengeTile>().sceneNames2;
        sceneNames3 = GameManager.instance.challengeTile.GetComponent<ChallengeTile>().sceneNames3;
        sceneNames4 = GameManager.instance.challengeTile.GetComponent<ChallengeTile>().sceneNames4;
    }

    public override void InvokeTile()
    {
        var emptyTiles = BoardPath.instance.tiles.FindAll(tile => tile.GetComponent<EmptyTile>());
        var thisIndex = transform.GetSiblingIndex();

        int randomIndex = Random.Range(0, emptyTiles.Count);
        var newIndex = BoardPath.instance.tiles.IndexOf(emptyTiles[randomIndex]);

        BoardPath.instance.ChangeTileIndexServerRpc(thisIndex, (int)Tiles.EmptyTile);
        BoardPath.instance.ChangeTileIndexServerRpc(newIndex, (int)Tiles.ChallengeTile);

        SelectRandomChallenge(thisIndex);
    }

    private void SelectRandomChallenge(int currentIndex)
    {
        string newScene;
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

        switch (playersOnTile)
        {
            case 1:
                newScene = sceneNames1[Random.Range(0, sceneNames1.Length)];
                break;
            case 2:
                newScene = sceneNames2[Random.Range(0, sceneNames2.Length)];
                break;
            case 3:
                newScene = sceneNames3[Random.Range(0, sceneNames3.Length)];
                break;
            case 4:
                newScene = sceneNames4[Random.Range(0, sceneNames4.Length)];
                break;
            default:
                newScene = sceneNames1[Random.Range(0, sceneNames1.Length)];
                break;
        }

        GameManager.instance.LoadSceneServerRpc(newScene);
    }
}
