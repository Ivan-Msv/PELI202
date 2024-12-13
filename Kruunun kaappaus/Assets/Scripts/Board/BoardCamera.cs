using Unity.Cinemachine;
using UnityEngine;

public class BoardCamera : MonoBehaviour
{
    public bool isDisabled;
    [field:SerializeField] public CinemachineCamera VirtualCamera { get; private set; }

    public void UpdateCameraFollow()
    {
        if (isDisabled)
        {
            return;
        }

        VirtualCamera.Follow = GameManager.instance.currentPlayer.transform;
        VirtualCamera.transform.position = new Vector3(GameManager.instance.currentPlayer.transform.position.x, GameManager.instance.currentPlayer.transform.position.y, VirtualCamera.transform.position.z);
    }

    public void ChangeCameraFollow(bool disable, Transform newTransform = null)
    {
        isDisabled = disable;
        VirtualCamera.Follow = isDisabled ? newTransform : GameManager.instance.currentPlayer.transform;
    }
}
