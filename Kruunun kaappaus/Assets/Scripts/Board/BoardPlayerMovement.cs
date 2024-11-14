using System.Collections;
using UnityEngine;

public class BoardPlayerMovement : MonoBehaviour
{
    public BoardPath currentPath;
    [SerializeField] private float moveSpeed;
    private bool alreadyMoving;

    void Awake()
    {
        currentPath = GetComponent<BoardPath>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            MovePlayer(GameManager.instance.currentPlayer, 1);
        }
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

        while (steps > 0)
        {
            int index = (player.currentPosition + 1) % currentPath.tiles.Count;
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

    //private int GetNextIndexAndDirection(BoardPlayerInfo player)
    //{
    //    int index;

    //    if (player.currentPosition == 0)
    //    {
    //        player.movingForward = true;
    //    }
    //    else if (player.currentPosition == currentPath.tiles.Count - 1 && currentPath.hasDeadEnd)
    //    {
    //        player.movingForward = false;
    //    }

    //    switch (player.movingForward)
    //    {
    //        case true:
    //            index = (player.currentPosition + 1) % currentPath.tiles.Count;
    //            break;
    //        case false:
    //            index = (player.currentPosition - 1 + currentPath.tiles.Count) % currentPath.tiles.Count;
    //            break;
    //    }

    //    return index;
    //}
}
