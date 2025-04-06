[System.Serializable]
public class Faction
{
    public string name;
    public string motto;
    public string description;
    public int flagID;

    public int Fame
    {
        get
        {
            return fame;
        }
        set
        {
            if (fame > 1)
            {
                if (value <= 1)
                {
                    fame = 1;
                }
                else if (value > 100)
                {
                    fame = 100;
                }
                else
                {
                    fame = value;
                }
            }
            else
            {
                if (value < 0)
                {
                    fame = 0;
                }
                else
                {
                    fame = value;
                }
            }
        }
    }
    [UnityEngine.SerializeField] private int fame = 50;

    public void AddFame(int count, int limit = -1)
    {
        if (Fame < limit || limit < 0 || count < 0)
        {
            Fame += count;
            if (limit > -1)
            {
                if (Fame > limit)
                    Fame = limit;
            }
            if (count < 0 && Fame < limit && limit != -1)
            {
                Fame = limit;
            }
        }
    }

    public void AddLimitedFame(int count, int limit)
    {
        if ((Fame + count) <= limit)
            Fame += count;
    }

    public void RemoveLimitedFame(int count, int limit = 0)
    {
        if ((Fame - count) >= limit)
            Fame -= count;
    }

    public void RemoveFame(int count)
    {
        Fame -= count;
    }

}