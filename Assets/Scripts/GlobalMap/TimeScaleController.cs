using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeScaleController : MonoBehaviour
{
    public int currentTime = 1;
    public int oldCurrentTime;

    private void Update()
    {
        if (DialogueManager.Instance.isDialogueOpen || InterfaceHandler.Instance.isMenuOpen)
        {
            Time.timeScale = 0;
        }
        else
        {
            Time.timeScale = currentTime;
            if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Space))
            {
                if (currentTime == 0)
                {
                    currentTime = oldCurrentTime;
                }
                else
                {
                    oldCurrentTime = currentTime;
                    currentTime = 0;
                }
            }
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                currentTime = 1;
            }
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                currentTime = 2;
            }
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                currentTime = 3;
            }
        }
    }
}
