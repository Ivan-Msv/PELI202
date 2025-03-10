using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;

public class BoardMap : MonoBehaviour
{
    [SerializeField] private CinemachineCamera virtualCam;
    [SerializeField] private Camera zoomCam;
    [SerializeField] private RectTransform mapImage;
    [SerializeField] private bool resetPosOnEnable;
    [SerializeField] private bool invertDrag;
    [SerializeField] private float minZoom;
    [SerializeField] private float maxZoom;
    [SerializeField] private float zoomSensitivity;

    [SerializeField] private Vector2 mouseScreenPos;
    [SerializeField] private Vector2 dragMousePos;
    public bool dragging;

    private CinemachineConfiner2D virtualCamConfiner;
    private Vector3 startPos;
    private float startZoom;

    void Start()
    {
        virtualCamConfiner = GetComponent<CinemachineConfiner2D>();

        startPos = transform.position;
        startZoom = virtualCam.Lens.OrthographicSize;
    }

    void Update()
    {
        mouseScreenPos = Input.mousePosition;

        if (!RectTransformUtility.RectangleContainsScreenPoint(mapImage, mouseScreenPos)) { return; }

        if (!mapImage.gameObject.activeInHierarchy) { return; }

        //MapDrag();
        Zoom(Input.GetAxis("Mouse ScrollWheel"));
    }

    public void ResetMapPosition()
    {
        if (!resetPosOnEnable) { return; }

        virtualCam.transform.position = startPos;
        virtualCam.Lens.OrthographicSize = startZoom;
    }

    private void MapDrag()
    {
        if (Input.GetMouseButtonDown(0))
        {
            dragMousePos = mouseScreenPos;
            dragging = true;
        }
        
        if (Input.GetMouseButtonUp(0))
        {
            dragging = false;
        }

        if (dragging)
        {
            Vector2 posDiff = mouseScreenPos - dragMousePos;

            RectTransformUtility.ScreenPointToWorldPointInRectangle(mapImage, posDiff, null, out Vector3 newPoint);

            posDiff = new Vector3(newPoint.x, newPoint.y, virtualCam.transform.position.z) - virtualCam.transform.position;

            transform.position += (Vector3)posDiff;

            dragMousePos = mouseScreenPos;
        }
    }

    private void Zoom(float zoomAxis)
    {
        if (zoomAxis == 0) { return; }

        virtualCam.Lens.OrthographicSize = Mathf.Clamp(virtualCam.Lens.OrthographicSize - zoomAxis * zoomSensitivity, minZoom, maxZoom);
        virtualCamConfiner.InvalidateBoundingShapeCache();
    }
}
