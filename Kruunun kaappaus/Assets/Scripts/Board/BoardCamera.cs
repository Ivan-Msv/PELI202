using Unity.Cinemachine;
using UnityEngine;

public class BoardCamera : MonoBehaviour
{
    [SerializeField] private CinemachineCamera virtualCamera;
    public bool isDisabled;

    public void UpdateCameraFollow()
    {
        if (isDisabled)
        {
            return;
        }

        virtualCamera.Follow = GameManager.instance.currentPlayer.transform;
        virtualCamera.transform.position = new Vector3(GameManager.instance.currentPlayer.transform.position.x, GameManager.instance.currentPlayer.transform.position.y, virtualCamera.transform.position.z);
    }

    public void ChangeCameraFollow(bool disable, Transform newTransform = null)
    {
        isDisabled = disable;
        virtualCamera.Follow = isDisabled ? newTransform : GameManager.instance.currentPlayer.transform;
    }
}
