using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class ChallengeTile : BoardTile
{
    public string[] sceneNamesTwoPlayers;
    public string[] sceneNames;
    public override void SetupTile()
    {
        // Tajusin vast nyt kuink huonosti toi on tehty, vihaan sitä 👍 (4/12/2024)
        tileSprite = GameManager.instance.challengeTile.tileSprite;
        tileName = GameManager.instance.challengeTile.name;
        sceneNamesTwoPlayers = GameManager.instance.challengeTile.GetComponent<ChallengeTile>().sceneNamesTwoPlayers;
        sceneNames = GameManager.instance.challengeTile.GetComponent<ChallengeTile>().sceneNames;
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

        if (playersOnTile > 1)
        {
            newScene = sceneNamesTwoPlayers[Random.Range(0, sceneNamesTwoPlayers.Length)];
        }
        else
        {
            newScene = sceneNames[Random.Range(0, sceneNames.Length)];
        }

        GameManager.instance.LoadSceneServerRpc(newScene);
    }
}
