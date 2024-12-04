using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class MinigameTile : BoardTile
{
    [SerializeField] private string[] sceneNames1;
    [SerializeField] private string[] sceneNames2;
    [SerializeField] private string[] sceneNames3;
    [SerializeField] private string[] sceneNames4;
    public override void SetupTile()
    {
        tileSprite = GameManager.instance.minigameTile.tileSprite;
        tileName = GameManager.instance.minigameTile.name;
    }
    public override void InvokeTile()
    {
        SelectRandomChallenge(transform.GetSiblingIndex());
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
