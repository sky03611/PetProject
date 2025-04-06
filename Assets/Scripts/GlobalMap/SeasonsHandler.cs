using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Season { SUMMER, AUTUMN, WINTER, SPRING }

public class SeasonsHandler : Singleton<SeasonsHandler>
{
    public List<Month> months;
    public int currentMonth, oldMonth;
    TerrainTextureChanger ttc;

    private void Awake()
    {
        ttc = GetComponent<TerrainTextureChanger>();
    }

    public Season GetCurrentSeason()
    {
        return GetCurrentMonth().season;
    }

    public void NextMonth()
    {
        oldMonth = currentMonth;
        currentMonth++;
        if (currentMonth >= months.Count)
        {
            currentMonth = 0;
        }
        if (GetCurrentSeason() != months[oldMonth].season)
        {
            ttc.UpdateTextures();
        }
    }

    public Month GetCurrentMonth ()
    {
        return months[currentMonth];
    }
}

[System.Serializable]
public class Month
{
    public Season season;
    public int maxDays;
    public Texture2D groundTexture;
}
