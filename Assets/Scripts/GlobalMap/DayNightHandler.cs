using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DayNightHandler : SerializedSingleton<DayNightHandler>
{
    [SerializeField] private int days, totalDays;
    [SerializeField, Range(15f, 60f)] private float speed = 30;
    [SerializeField] private float Blend;
    [SerializeField] private Light light;

    public int Days
    {
        get
        {
            return days;
        }
        set
        {
            days = value;
            //SeasonsHandler.Instance.CheckSeasonChange(days);
            //InterfaceHandler.Instance.SetDays();
        }
    }
    public float Timer;

    void Update()
    {
        Timer += Time.deltaTime * speed;

        if (Timer > 350 && Timer < 800)
            Blend = 1 - ((Timer - 350) / (800 - 350));
        if (Timer > 2000 && Timer < 2700)
            Blend = ((Timer - 2000) / (2700 - 2000));
        if (Timer < 350 || Timer > 2700)
            Blend = 1;
        if (Timer > 800 && Timer < 2000)
            Blend = 0;
        light.intensity = Mathf.Lerp(0.2f, 1f, 1-Blend);

        if (Timer > 3000)
        {
            OnDayPassed();
        }
    }

    public int TotalDays()
    {
        return totalDays;
    }

    public float GetTimeSpeed()
    {
        return speed;
    }

    private void OnDayPassed()
    {
        Days++;
        totalDays++;
        GlobalTownManager.Instance.OnDayPassed();
        Timer = 0;
        if (Days > SeasonsHandler.Instance.GetCurrentMonth().maxDays)
        {
            SeasonsHandler.Instance.NextMonth();
            Days = 1;
        }
    }
}
