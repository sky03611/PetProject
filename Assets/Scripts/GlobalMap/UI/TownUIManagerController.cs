using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TownUIManagerController : Singleton<TownUIManagerController>
{
    [SerializeField] private Canvas worldCanvas;
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (mainCamera.enabled)
        {
            worldCanvas.transform.position = mainCamera.transform.position + mainCamera.transform.forward * 2f;
            worldCanvas.transform.LookAt(worldCanvas.transform.position + mainCamera.transform.rotation * Vector3.forward, mainCamera.transform.rotation * Vector3.up);
        }
    }
}