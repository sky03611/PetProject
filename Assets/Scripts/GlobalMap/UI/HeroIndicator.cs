using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class WayPointMarkerSystem : MonoBehaviour
{
    [SerializeField] private Image _marker;
    [SerializeField][Range(10, 30f)] private float _distanceToDisable = 10;

    [Range(0.5f, 1f)]
    [SerializeField] private float _screenBoundOffset = 1f;
    [SerializeField] private float _offsetBorder = 0;
    [SerializeField] private CameraController cameraController;
    [SerializeField] private Transform hero;

    private Camera _mainCamera;
    private Vector3 _screenCentre;
    private Vector3 _screenBounds;
    private bool _isEnabled;
    private bool _maxDisable;
    private bool _reEnable;
    private Vector3 _targetPosition;



    public void Awake()
    {
        SetUIWayPoint();
        SetPortrait();
    }

    private IEnumerator SetPortrait()
    {
        yield return new WaitForSeconds(0.1f);

        _marker.sprite = PlayerController.Instance.GetPortrait();
    }

    internal bool IsInViewpoint (Vector3 position)
    {
        Vector3 objectViewportPosition = _mainCamera.WorldToViewportPoint(position);
        if (objectViewportPosition.x >= 0 && objectViewportPosition.x <= 1 &&
           objectViewportPosition.y >= 0 && objectViewportPosition.y <= 1 &&
           objectViewportPosition.z > 0)
        {
            return true;
        }
        return false;
    }

    public void SetUIWayPoint()
    {
        _mainCamera = Camera.main;
        _screenCentre = new Vector3(Screen.width, Screen.height, 0) / 2;
        _screenBounds = _screenCentre * _screenBoundOffset;
    }

    internal void EnableTipMarker()
    {
        _marker.gameObject.SetActive(true);
        _isEnabled = true;
        _maxDisable = false;
    }

    public void Update()
    {
        if (InterfaceHandler.Instance.isMenuOpen || DialogueManager.Instance.isDialogueOpen)
        {
            _marker.gameObject.SetActive(false);
            return;
        }
        float dist = Vector3.Distance(transform.position, new Vector3 (hero.position.x, transform.position.y, hero.position.z));
        _targetPosition = new Vector3(hero.position.x, transform.position.y, hero.position.z);
        if (dist > _distanceToDisable && _reEnable && !_maxDisable || CameraController.Instance.ShouldShowHeroMarker())
        {
            _reEnable = false;
            EnableTipMarker();
        }
        if ((dist < _distanceToDisable || IsInViewpoint(hero.position)) && !CameraController.Instance.ShouldShowHeroMarker())
        {
            _marker.gameObject.SetActive(false);
            _isEnabled = false;
            _reEnable = true;
        }

        Vector3 screenPosition = _mainCamera.WorldToScreenPoint(_targetPosition);
        if (IsInViewpoint(hero.position))
        {
            if (!CameraController.Instance.ShouldShowHeroMarker())
            {
                screenPosition.z = 0;
            }
            else
            {
                screenPosition = _mainCamera.WorldToScreenPoint(hero.position);
            }
        }
        else
        {
            float angle = float.MinValue;
            GetArrowIndicatorPositionAndAngle(ref screenPosition, ref angle, _screenCentre, _screenBounds);
        }
        _marker.transform.position = screenPosition;
        if (Input.GetMouseButtonDown(0) && IsPointerOverIndicator())
        {
            if (_marker.gameObject.activeSelf)
            {
                MoveCameraToHero();
            }
        }
    }

    bool IsPointerOverIndicator()
    {
        Vector2 mousePosition = Input.mousePosition;
        Vector2 indicatorPos = _marker.transform.position;

        float distance = Vector2.Distance(mousePosition, indicatorPos);
        return distance <= 50;
    }

    bool IsTargetVisible(Vector3 screenPosition)
    {
        bool isTargetVisible = screenPosition.z > 0 && screenPosition.x > _offsetBorder && screenPosition.x < Screen.width - _offsetBorder && screenPosition.y > _offsetBorder && screenPosition.y < Screen.height - _offsetBorder;
        return isTargetVisible;
    }

    void GetArrowIndicatorPositionAndAngle(ref Vector3 screenPosition, ref float angle, Vector3 screenCentre, Vector3 screenBounds)
    {
        screenPosition -= screenCentre;
        if (screenPosition.z < 0)
        {
            screenPosition *= -1;
        }
        angle = Mathf.Atan2(screenPosition.y, screenPosition.x);
        float slope = Mathf.Tan(angle);
        if (screenPosition.x > 0)
        {
            screenPosition = new Vector3(screenBounds.x, screenBounds.x * slope, 0);
        }
        else
        {
            screenPosition = new Vector3(-screenBounds.x, -screenBounds.x * slope, 0);
        }
        if (screenPosition.y > screenBounds.y)
        {
            screenPosition = new Vector3(screenBounds.y / slope, screenBounds.y, 0);
        }
        else if (screenPosition.y < -screenBounds.y)
        {
            screenPosition = new Vector3(-screenBounds.y / slope, -screenBounds.y, 0);
        }
        screenPosition += screenCentre;
    }

    public void MoveCameraToHero()
    {
        if (cameraController != null)
        {
            cameraController.MoveCameraToHero(hero.position);
        }
    }
}

