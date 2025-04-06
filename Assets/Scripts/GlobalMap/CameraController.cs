using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;
using Pathfinding;

[RequireComponent(typeof(Camera))]
public class CameraController : Singleton<CameraController>
{
    public float ScreenEdgeBorderThickness = 5.0f;

    [SerializeField] private Transform player;

    [Header("Movement Speeds")]
    [Space]
    public float zoomSpeed;
    public float panSpeed;
    public float cameraMoveSpeed = 2.0f;

    [Header("Movement Limits")]
    [Space]
    public bool enableMovementLimits;
    public Vector2 heightLimit;
    public Vector2 lenghtLimit;
    public Vector2 widthLimit;
    public Vector2 zoomLimit;

    private Vector3 initialPos;
    private Vector3 panMovement;
    private Vector3 pos;
    private Quaternion rot;
    private bool rotationActive = false;
    private Vector3 lastMousePosition;
    private Quaternion initialRot;

    [Header("Rotation")]
    [Space]
    public bool rotationEnabled;
    public float rotateSpeed;

    [Header("Height Controller")]
    public float desiredDistance = 15;
    Queue<float> heightSamples = new Queue<float>();
    int sampleSize = 5;
    float smoothVelocity = 0f;
    float newY, averagedHeight;

    public Vector3 initialOffset = new Vector3(0, 5, -10);
    private bool isMovingToHero = false, unremoveable = false;

    private float cursorEdgeTime = 0f;
    private const float timeThreshold = 0.5f;

    [SerializeField] private Transform hero;

    public float visibilityDistance = 40;

    void Start()
    {
        initialPos = transform.position;
        initialRot = transform.rotation;
        StartCoroutine(SetHeroPosition());
    }

    public Vector3 GetHeroPosition(float externalY = 0)
    {
        if (externalY == 0)
            return hero.position;
        return new Vector3(hero.position.x, externalY, hero.position.z);
    }

    public IEnumerator SetHeroPosition()
    {
        yield return new WaitForSeconds(0.1f);
        GlobalTownManager.Instance.towns[0].isVisited = true;
        Vector3 position = ClosestPointToRoad(GlobalTownManager.Instance.towns[0].transform.position);
        hero.position = position;
        hero.GetComponent<FollowerEntity>().SetDestination(hero.position);
        MoveCameraToHero(hero.position, true);
    }

    private Vector3 ClosestPointToRoad(Vector3 fromPosition)
    {
        var selectedGraph = AstarPath.active.graphs[1];
        NNInfo nearestNodeInfo = selectedGraph.GetNearest(fromPosition);

        return nearestNodeInfo.position;
    }

    private float DistanceToGround()
    {
        RaycastHit hit;
        float distance = 0;
        float forwardOffset = 5f;
        Vector3 forwardXZ = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
        Vector3 origin = transform.position + forwardXZ * forwardOffset;
        if (Physics.Raycast(origin, -Vector3.up, out hit))
        {
            distance = hit.distance;
        }
        return distance;
    }

    void Update()
    {
        if (DialogueManager.Instance.isDialogueOpen || InterfaceHandler.Instance.isMenuOpen)
            return;

        MovementController();
        HeightController();
        ZoomController();
        RotationController();

        if (enableMovementLimits)
        {
            pos = transform.position;
            pos.y = Mathf.Clamp(pos.y, heightLimit.x, heightLimit.y);
            pos.z = Mathf.Clamp(pos.z, lenghtLimit.x, lenghtLimit.y);
            pos.x = Mathf.Clamp(pos.x, widthLimit.x, widthLimit.y);
            transform.position = pos;
        }
    }

    private void MovementController()
    {
        panMovement = Vector3.zero;

        bool isNearEdge = false;

        if (Input.mousePosition.y >= Screen.height - ScreenEdgeBorderThickness)
        {
            isNearEdge = true;
            cursorEdgeTime += Time.unscaledDeltaTime;
            if (cursorEdgeTime >= timeThreshold)
            {
                panMovement += new Vector3(transform.forward.x, 0, transform.forward.z) * panSpeed * Time.unscaledDeltaTime;
            }
        }
        else if (Input.mousePosition.y <= ScreenEdgeBorderThickness)
        {
            isNearEdge = true;
            cursorEdgeTime += Time.unscaledDeltaTime;
            if (cursorEdgeTime >= timeThreshold)
            {
                panMovement -= new Vector3(transform.forward.x, 0, transform.forward.z) * panSpeed * Time.unscaledDeltaTime;
            }
        }

        if (Input.mousePosition.x <= ScreenEdgeBorderThickness)
        {
            isNearEdge = true;
            cursorEdgeTime += Time.unscaledDeltaTime;
            if (cursorEdgeTime >= timeThreshold)
            {
                panMovement += -transform.right * panSpeed * Time.unscaledDeltaTime;
            }
        }
        else if (Input.mousePosition.x >= Screen.width - ScreenEdgeBorderThickness)
        {
            isNearEdge = true;
            cursorEdgeTime += Time.unscaledDeltaTime;
            if (cursorEdgeTime >= timeThreshold)
            {
                panMovement += transform.right * panSpeed * Time.unscaledDeltaTime;
            }
        }

        if (!isNearEdge)
        {
            cursorEdgeTime = 0f;
        }

        if (Input.GetKey(KeyCode.LeftShift))
        {
            transform.Translate(panMovement * 2, Space.World);
        }
        else
        {
            transform.Translate(panMovement, Space.World);
        }

        if (Input.GetKey(KeyCode.W))
        {
            panMovement += new Vector3(transform.forward.x, 0, transform.forward.z) * panSpeed * Time.unscaledDeltaTime;
        }
        if (Input.GetKey(KeyCode.S))
        {
            panMovement -= new Vector3(transform.forward.x, 0, transform.forward.z) * panSpeed * Time.unscaledDeltaTime;
        }
        if (Input.GetKey(KeyCode.A))
        {
            panMovement += -transform.right * panSpeed * Time.unscaledDeltaTime;
        }
        if (Input.GetKey(KeyCode.D))
        {
            panMovement += transform.right * panSpeed * Time.unscaledDeltaTime;
        }

        if (Input.GetKey(KeyCode.Q))
        {
            transform.Rotate(new Vector3(0, -1, 0) * Time.unscaledDeltaTime * 30, Space.World);
        }
        if (Input.GetKey(KeyCode.E))
        {
            transform.Rotate(new Vector3(0, 1, 0) * Time.unscaledDeltaTime * 30, Space.World);
        }

        if (Input.GetKey(KeyCode.LeftShift))
        {
            transform.Translate(panMovement * 2, Space.World);
        }
        else
        {
            transform.Translate(panMovement, Space.World);
        }
    }

    private void HeightController()
    {
        if (isMovingToHero)
            return;

        heightSamples.Enqueue(transform.position.y + (desiredDistance - DistanceToGround()));
        if (heightSamples.Count > sampleSize)
        {
            heightSamples.Dequeue();
        }

        averagedHeight = heightSamples.Average();

        newY = Mathf.SmoothDamp(transform.position.y, averagedHeight, ref smoothVelocity, 0.2f, Mathf.Infinity, Time.unscaledDeltaTime);
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

    }

    public bool ShouldShowHeroMarker()
    {
        if (Mathf.Abs(desiredDistance - zoomLimit.x) >= Mathf.Abs(zoomLimit.y - desiredDistance))
            return true;
        return false;
    }

    private void ZoomController()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        desiredDistance = Mathf.Clamp(desiredDistance - scroll * zoomSpeed, zoomLimit.x, zoomLimit.y);
    }

    private void RotationController()
    {
        if (rotationEnabled)
        {
            if (Input.GetMouseButton(1))
            {
                rotationActive = true;
                Vector3 mouseDelta;
                if (lastMousePosition.x >= 0 &&
                    lastMousePosition.y >= 0 &&
                    lastMousePosition.x <= Screen.width &&
                    lastMousePosition.y <= Screen.height)
                    mouseDelta = Input.mousePosition - lastMousePosition;
                else
                {
                    mouseDelta = Vector3.zero;
                }

                var rotation = Vector3.up * Time.unscaledDeltaTime * rotateSpeed * mouseDelta.x;
                rotation += Vector3.left * Time.unscaledDeltaTime * rotateSpeed * mouseDelta.y;

                transform.Rotate(rotation, Space.World);

                Vector3 currentRotation = transform.eulerAngles;

                float clampedPitch = Mathf.Clamp(currentRotation.x, 40f, 65f);

                transform.rotation = Quaternion.Euler(clampedPitch, currentRotation.y, 0);
            }

            if (Input.GetMouseButtonUp(1))
            {
                rotationActive = false;
            }

            lastMousePosition = Input.mousePosition;
        }
    }

    public void MoveCameraToHero(Vector3 heroPosition, bool _unremoveable = false)
    {
        isMovingToHero = true;
        desiredDistance = 15;
        unremoveable = _unremoveable;
        MoveCameraToHeroWithTween(heroPosition);
    }

    private void MoveCameraToHeroWithTween(Vector3 targetHeroPosition)
    {
        Quaternion targetRotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);

        Vector3 offset = targetRotation * initialOffset;

        Vector3 desiredPosition = new Vector3(targetHeroPosition.x - offset.x, hero.transform.position.y, targetHeroPosition.z - offset.z);

        float distanceFromHero = 25f;
        desiredPosition -= transform.forward * distanceFromHero;

        float moveDuration = 1.0f;
        transform.DOMove(desiredPosition, moveDuration).OnKill(() =>
        {
            isMovingToHero = false;
            unremoveable = false;
        });
        heightSamples.Clear();
    }
}