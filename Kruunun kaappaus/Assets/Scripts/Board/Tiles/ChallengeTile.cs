using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class ChallengeTile : BoardTile
{
    [SerializeField] private string[] sceneNames1v1;
    [SerializeField] private string[] sceneNames;
    public override void SetupTile()
    {
        tileSprite = GameManager.instance.challengeTile.tileSprite;
        tileName = GameManager.instance.challengeTile.name;
    }

    public override void InvokeTile()
    {
        var emptyTiles = BoardPath.instance.tiles.FindAll(tile => tile.GetComponent<EmptyTile>());
        var thisIndex = transform.GetSiblingIndex();

        int randomIndex = Random.Range(0, emptyTiles.Count);
        var newIndex = BoardPath.instance.tiles.IndexOf(emptyTiles[randomIndex]);

        BoardPath.instance.ChangeTileIndexServerRpc(thisIndex, (int)Tiles.EmptyTile);
        BoardPath.instance.ChangeTileIndexServerRpc(newIndex, (int)Tiles.ChallengeTile);

        // Change later to challegne selection
        SelectRandomChallenge(thisIndex);
    }

    private void SelectRandomChallenge(int currentIndex)
    {
        string newScene;
        int playersOnTile = 0;

        foreach (var player in GameManager.instance.availablePlayers)
        {
            if (currentIndex == player.GetComponentInParent<MainPlayerInfo>().currentBoardPosition.Value)
            {
                playersOnTile++;
            }
        }

        if (playersOnTile > 1)
        {
            newScene = sceneNames1v1[Random.Range(0, sceneNames1v1.Length)];
        }
        else
        {
            newScene = sceneNames[Random.Range(0, sceneNames.Length)];
        }

        GameManager.instance.LoadSceneServerRpc(newScene);
    }
}
