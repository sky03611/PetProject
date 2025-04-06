using UnityEditor.EditorTools;
using UnityEngine;

public class VillagerUIManager : MonoBehaviour
{
    public VillagerUI villagerUI;
    public VillagerUI villagerUIPrefab;
    public Canvas objectCanvas;
    private VillagerScript villager;
    [SerializeField, Range(-10f, 10f)] private float uiOffset = -5f;
    private CanvasGroup villagerUICanvasGroup;

    Vector3 villagerPosition, adjustedPosition;
    float scaleFactor;

    public void Clear()
    {
        if (villagerUI != null)
        {
            Destroy(villagerUI.gameObject);
        }
    }

    private void Awake()
    {
        villager = GetComponent<VillagerScript>();
    }

    public void Initialize()
    {
        objectCanvas = GlobalInterfaceHandler.Instance.objectCanvas;
        if (villagerUI == null && villagerUIPrefab != null)
        {
            villagerUI = Instantiate(villagerUIPrefab, objectCanvas.transform);
        }
        villagerUI.gameObject.SetActive(true);
        villagerUICanvasGroup = villagerUI.GetComponent<CanvasGroup>();
    }

    private void Update()
    {
        UpdatePosition();
    }

    public void UpdatePosition()
    {
        if (villagerUI != null)
        {
            villagerPosition = villager.transform.position;
            villagerUI.rect.position = new Vector3(villagerPosition.x, villagerPosition.y + 1, villagerPosition.z + uiOffset);

            AdjustUIScaleBasedOnZoom();

            villagerUI.UpdatePartySize(villager);

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
        villagerUI.rect.localScale = new Vector3(scaleFactor, scaleFactor, 1);
    }
}