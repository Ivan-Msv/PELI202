using System.Collections;
using Unity.Netcode;
using UnityEngine;

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
            while (player.transform.position != nextTilePosition)
            {
                player.transform.position = Vector2.MoveTowards(player.transform.position, nextTilePosition, moveSpeed * Time.deltaTime);
                player.GetComponent<Animator>().SetBool("IsMoving", true);
                yield return null;
            }

            player.GetComponent<Animator>().SetBool("IsMoving", false);
            yield return new WaitForSeconds(0.25f);
            player.playerInfo.currentBoardPosition.Value = index;
            steps--;
        }

        if (!emptyRoll)
        {
            BoardPath.instance.tiles[player.playerInfo.currentBoardPosition.Value].GetComponent<BoardTile>().InvokeTile();
        }
        alreadyMoving = false;
        GameManager.instance.ChangeGameStateServerRpc(BoardState.SelectingPlayer);
        BoardPath.instance.SplitPlayersOnTiles();
    }

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
