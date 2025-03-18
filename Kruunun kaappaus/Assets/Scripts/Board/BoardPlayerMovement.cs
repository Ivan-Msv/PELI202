using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Splines;

public class BoardPlayerMovement : NetworkBehaviour
{
    [SerializeField] private float moveSpeed;
    private bool alreadyMoving;

    public void MovePlayer(BoardPlayerInfo player, int steps)
    {
        StartCoroutine(PlayerMovement(player, steps));
    }

    private IEnumerator PlayerMovement(BoardPlayerInfo player, int steps)
    {
        if (alreadyMoving)
        {
            yield break;
        }
        alreadyMoving = true;

        bool forward = steps > 0;
        bool emptyRoll = Mathf.Abs(steps) < 1;

        steps = Mathf.Abs(steps);

        while (steps > 0)
        {
            int index = GetIndexDirection(player.playerInfo.currentBoardPosition.Value, forward);
            Vector3 nextTilePosition = BoardPath.instance.tiles[index].transform.position;
            bool movingLeft = player.transform.position.x > nextTilePosition.x;
            player.FlipSpriteRpc(movingLeft);

            while (player.transform.position != nextTilePosition)
            {
                MovePlayer(player, nextTilePosition);
                yield return null;
            }

            player.GetComponent<Animator>().SetBool("IsMoving", false);
            yield return new WaitForSeconds(0.25f);
            player.playerInfo.currentBoardPosition.Value = index;
            steps--;


            // Keeps moving if not shop tile
            if (!BoardUIManager.instance.LocalPlayerOnShopTile())
            {
                continue;
            }

            // Otherwise opens store, and keeps "moving" until you close the UI
            BoardUIManager.instance.shopUI.OpenStore();
            PlayShopAnimationRpc(index, "Shop_Activate");
            while (BoardUIManager.instance.shopUI.StoreOpen())
            {
                yield return null;
            }
            PlayShopAnimationRpc(index, "Shop_Deactivate");
        }

        alreadyMoving = false;
        BoardPath.instance.SplitPlayersOnTiles();
        var currentTileIndex = GameManager.instance.tilesIndex[player.playerInfo.currentBoardPosition.Value];

        // In case of a tile being "special" in a way of having ui
        // Let the UI handle ending the turn
        bool activateSpecialUI = CheckSpecialTileUI(currentTileIndex);

        switch (activateSpecialUI)
        {
            case true:
                GameManager.instance.ChangeGameStateServerRpc(BoardState.Idle);
                break;
            case false:
                GameManager.instance.ChangeGameStateServerRpc(BoardState.SelectingPlayer);
                break;
        }

        bool challengeTile = currentTileIndex == (int)Tiles.ChallengeTile;

        if (!emptyRoll)
        {
            BoardPath.instance.tiles[player.playerInfo.currentBoardPosition.Value].GetComponent<BoardTile>().InvokeTile();

            // If not a challenge tile, add count
            GameManager.instance.nonChallengeTileCount += challengeTile ? 0 : 1;
        }
    }

    private bool CheckSpecialTileUI(int tileIndex)
    {
        switch ((Tiles)tileIndex)
        {
            case Tiles.TeleportTile:
                return true;
        }

        return false;
    }

    [Rpc(SendTo.Everyone)]
    private void PlayShopAnimationRpc(int shopTileIndex, string animationName)
    {
        var shopAnimator = BoardPath.instance.tiles[shopTileIndex].GetComponent<Animator>();
        shopAnimator.Play(animationName);
    }

    private void MovePlayer(BoardPlayerInfo player, Vector3 nextTilePosition)
    {
        player.transform.position = Vector2.MoveTowards(player.transform.position, nextTilePosition, moveSpeed * Time.deltaTime);

        // Animation
        player.GetComponent<Animator>().SetBool("IsMoving", true);
    }

    // This is such a bad way to make it find next direction...
    // I even made a better one in TeleportTileUI.cs (Line 113 unless changed)
    // But I can't be bothered to change it so I will let it rot here
    private int GetIndexDirection(int currentPosition, bool forward)
    {
        int index;

        switch (forward)
        {
            case true:
                index = (currentPosition + 1) % BoardPath.instance.tiles.Count;
                break;
            case false:
                index = (currentPosition - 1 + BoardPath.instance.tiles.Count) % BoardPath.instance.tiles.Count;
                break;
        }

        return index;
    }
}
