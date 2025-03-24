using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;

public class BoardMap : MonoBehaviour
{
    [SerializeField] private CinemachineCamera virtualCam;
    [SerializeField] private Camera zoomCam;
    [SerializeField] private RectTransform mapImage;
    [SerializeField] private bool resetPosOnEnable;
    [SerializeField] private TextMeshProUGUI resetMapTooltip;
    [SerializeField] private TextMeshProUGUI centerPlayerTooltip;
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
        if (!RectTransformUtility.RectangleContainsScreenPoint(mapImage, mouseScreenPos)) { return; }

        if (!mapImage.gameObject.activeInHierarchy) { return; }

        Zoom(Input.GetAxis("Mouse ScrollWheel"));
    }

    private void Hotkeys()
    {
        if (Input.GetAxisRaw("Map_Reset") != 0)
        {
            ResetMapPosition();
        }

        if (Input.GetAxisRaw("Map_Focus") != 0)
        {
            var playerPos = BoardUIManager.instance.localPlayer.transform.position;
            virtualCam.transform.position = new(playerPos.x, playerPos.y, virtualCam.transform.position.z);
        }
    }

    private void LateUpdate()
    {
        mouseScreenPos = Input.mousePosition;

        if (!mapImage.gameObject.activeInHierarchy) { return; }

        MapDrag();
        Hotkeys();
    }

    public void ToggleMap()
    {
        ResetMapPosition();
        //UpdateTooltips();
    }

    private void UpdateTooltips()
    {
        // Do this once you create rebind stuff
        //resetMapTooltip.text = string.Format(resetMapTooltip.text, )
    }

    public void ResetMapPosition()
    {
        if (!resetPosOnEnable) { return; }

        virtualCam.transform.position = startPos;
        virtualCam.Lens.OrthographicSize = startZoom;
    }

    private void MapDrag()
    {
        if (Input.GetMouseButtonUp(0))
        {
            dragging = false;
        }

        RectTransformUtility.ScreenPointToWorldPointInRectangle(mapImage, mouseScreenPos, Camera.main, out Vector3 worldMousePoint);

        if (dragging)
        {
            var difference = worldMousePoint - virtualCam.transform.position;
            var newPos = dragMousePos - (Vector2)difference;
            virtualCam.transform.position = new(newPos.x, newPos.y, virtualCam.transform.position.z);

            dragMousePos = worldMousePoint;
        }

        // This is created because virtual camera doesn't get confined the same way main cam does (???)
        // Cinemachine stuff I suppose
        virtualCam.transform.position = zoomCam.transform.position;

        if (!RectTransformUtility.RectangleContainsScreenPoint(mapImage, mouseScreenPos)) { return; }

        if (Input.GetMouseButtonDown(0))
        {
            dragging = true;
            dragMousePos = worldMousePoint;
        }
    }

    private void Zoom(float zoomAxis)
    {
        if (zoomAxis == 0) { return; }

        virtualCam.Lens.OrthographicSize = Mathf.Clamp(virtualCam.Lens.OrthographicSize - zoomAxis * zoomSensitivity, minZoom, maxZoom);
        virtualCamConfiner.InvalidateBoundingShapeCache();
    }
}
