using UnityEngine;

public class TownUIManager : MonoBehaviour
{
    public TownUI townUI;
    public TownUI townUIPrefab;
    public Canvas objectCanvas;
    private TownManager townManager;
    [SerializeField, Range(-10f, 10f)] private float nameOffset = 3f;
    private CanvasGroup townUICanvasGroup;

    Vector3 townPosition, adjustedPosition;
    float scaleFactor;

    private void Awake()
    {
        townManager = GetComponent<TownManager>();
    }

    public void SetFaction()
    {
        townUI.SetFaction(townManager.thisTown);
    }

    public void Initialize(TownManager townManager)
    {
        this.townManager = townManager;
        GlobalTownManager.Instance.AddTown(townManager);
        objectCanvas = GlobalInterfaceHandler.Instance.objectCanvas;
        if (townUI == null && townUIPrefab != null)
        {
            townUI = Instantiate(townUIPrefab, objectCanvas.transform);
            townUI.Initialize(townManager.thisTown);
        }

        townUICanvasGroup = townUI.GetComponent<CanvasGroup>();
        townUI.gameObject.SetActive(true);
    }

    private void Update()
    {
        UpdatePosition();
    }

    public void UpdatePosition()
    {
        if (townUI != null)
        {
            townPosition = townManager.transform.position;
            townUI.rect.position = new Vector3(townPosition.x, townPosition.y + 1, townPosition.z + nameOffset);

            AdjustUIScaleBasedOnZoom();

            /*// Check distance to camera for visibility
            float distanceToCamera = Vector3.Distance(Camera.main.transform.position, new Vector3(townManager.transform.position.x, Camera.main.transform.position.y, townManager.transform.position.z));

            // Fade out the UI based on the distance
            float alpha = Mathf.InverseLerp(minDistanceForFullVisibility, maxDistanceForFullVisibility, distanceToCamera);
            townUICanvasGroup.alpha = alpha;

            // Set the UI active or inactive based on the visibility
            townUI.gameObject.SetActive(alpha > 0);*/
        }
    }


    private void AdjustUIScaleBasedOnZoom()
    {
        scaleFactor = Mathf.Lerp(0.6f, 0.1f, 1 - Mathf.InverseLerp(CameraController.Instance.zoomLimit.x, CameraController.Instance.zoomLimit.y, CameraController.Instance.desiredDistance));
        townUI.rect.localScale = new Vector3(scaleFactor, scaleFactor, 1);
    }
}