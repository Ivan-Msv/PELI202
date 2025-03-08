using System.Collections;
using System.Collections.Generic;
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
        var challengeTileComponent = GameManager.instance.challengeTile.GetComponent<ChallengeTile>();
        sceneNames1 = challengeTileComponent.sceneNames1;
        sceneNames2 = challengeTileComponent.sceneNames2;
        sceneNames3 = challengeTileComponent.sceneNames3;
        sceneNames4 = challengeTileComponent.sceneNames4;

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
        string newScene;
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


        string newScene;
        switch (playerAmount)
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

        GameManager.instance.LoadSceneRpc(newScene);
    }
}
