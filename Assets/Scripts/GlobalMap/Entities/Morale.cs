using UnityEngine;

[System.Serializable]
public class Morale
{
    [Range (-2, 2)]
    public int level;
    public string description;

    public float ProductionMultiplier()
    {
        switch (level)
        {
            case -2:
                return 0.75f;
            case -1:
                return 0.9f;
            case 0:
                return 1;
            case 1:
                return 1.1f;
            case 2:
                return 1.25f;
        }
        return 1;
    }

    public float PricesMultiplier()
    {
        switch (level)
        {
            case -2:
                return 0.75f;
            case -1:
                return 0.9f;
            case 0:
                return 1;
            case 1:
                return 1.1f;
            case 2:
                return 1.25f;
        }
        return 1;
    }
}
