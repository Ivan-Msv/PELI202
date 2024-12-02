using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class BoardPlayerMovement : MonoBehaviour
{
    private BoardPath currentPath;
    [SerializeField] private float moveSpeed;
    private bool alreadyMoving;

    void Awake()
    {
        currentPath = GameManager.instance.currentPath;
    }

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

        steps = Mathf.Abs(steps);

        while (steps > 0)
        {
            int index = GetIndexDirection(player.currentPosition, forward);
            Vector3 nextTilePosition = currentPath.tiles[index].transform.position;
            while (player.transform.position != nextTilePosition)
            {
                player.transform.position = Vector2.MoveTowards(player.transform.position, nextTilePosition, moveSpeed * Time.deltaTime);
                yield return null;
            }
            yield return new WaitForSeconds(0.1f);
            player.currentPosition = index;
            steps--;
        }

        currentPath.tiles[player.currentPosition].GetComponent<BoardTile>().InvokeTile();
        alreadyMoving = false;
    }

    private int GetIndexDirection(int currentPosition, bool forward)
    {
        int index;

        switch (forward)
        {
            case true:
                index = (currentPosition + 1) % currentPath.tiles.Count;
                break;
            case false:
                index = (currentPosition - 1 + currentPath.tiles.Count) % currentPath.tiles.Count;
                break;
        }

        return index;
    }
}
