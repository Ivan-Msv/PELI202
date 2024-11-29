using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class ChallengeTile : BoardTile
{
    public override void SetupTile()
    {
        tileSprite = GameManager.instance.challengeTile.tileSprite;
        tileName = GameManager.instance.challengeTile.name;
    }
    public override void InvokeTile()
    {
        InvokeTileServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void InvokeTileServerRpc()
    {
        ReplaceWithEmptyTile();
        ReplaceExistingWhiteTile();
        //temporary loadscene, CHANGE TO LEVEL SELECTION FUNCTION
        //NetworkManager.Singleton.SceneManager.LoadScene("ChallengeLevel1", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    private void ReplaceWithEmptyTile()
    {
        this.AddComponent<EmptyTile>();
        Destroy(this);
    }

    private void ReplaceExistingWhiteTile()
    {
        List<EmptyTile> usableTiles = new List<EmptyTile>();
        foreach (EmptyTile tile in FindObjectsByType<EmptyTile>(FindObjectsSortMode.None))
        {
            // Poista listalta ruutu, joka just muuttui tyhjäksi.
            if (tile.gameObject == gameObject)
            {
                continue;
            }

            usableTiles.Add(tile);
        }

        int randomIndex = Random.Range(0, usableTiles.Count);

        Destroy(usableTiles[randomIndex]);
        usableTiles[randomIndex].AddComponent<ChallengeTile>();
    }
}
