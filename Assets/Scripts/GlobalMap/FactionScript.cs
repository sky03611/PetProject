using MicroWorldNS;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FactionScript : SerializedSingleton<FactionScript>
{
    public List<Faction> factions = new List<Faction>();
    public List<Sprite> factionFlags;

    private void Start()
    {
        RandomizeFactions();
    }

    public Sprite GetFlag (Faction faction)
    {
        return factionFlags[faction.flagID];
    }

    private void RandomizeFactions()
    {
        List<Faction> tmpFactions = new List<Faction>();
        tmpFactions.AddRange(TypeArrayDeserializer.LoadResourceFromJson<Faction>("Factions/Factions"));

        List<int> assignedFlagIDs = new List<int>();

        int randFaction;
        for (int i = 0; i < 3; i++)
        {
            randFaction = Random.Range(0, tmpFactions.Count);
            Faction selectedFaction = tmpFactions[randFaction];
            tmpFactions.RemoveAt(randFaction);

            int randFlagID;
            do
            {
                randFlagID = Random.Range(0, factionFlags.Count);
            }
            while (assignedFlagIDs.Contains(randFlagID));

            selectedFaction.flagID = randFlagID;
            assignedFlagIDs.Add(randFlagID);

            factions.Add(selectedFaction);
        }
        StartCoroutine(AssignFactionsToTowns());
    }

    private IEnumerator AssignFactionsToTowns()
    {
        yield return new WaitForEndOfFrame();

        List<TownManager> towns = new List<TownManager>(GlobalTownManager.Instance.towns);

        float minX = Mathf.Infinity;
        float maxX = -Mathf.Infinity;

        foreach (var town in towns)
        {
            float xPos = town.transform.position.x;
            if (xPos < minX) minX = xPos;
            if (xPos > maxX) maxX = xPos;
        }

        // Calculate the boundaries for the three parts (left, middle, right)
        float mapWidth = maxX - minX;

        float leftBoundary = minX + mapWidth * 0.3f; 
        float rightBoundary = minX + mapWidth * 0.7f;

        int factionIndex = 0;

        foreach (var town in towns)
        {
            float xPos = town.transform.position.x;

            if (xPos < leftBoundary)
            {
                factionIndex = 0;
            }
            else if (xPos >= leftBoundary && xPos < rightBoundary)
            {
                factionIndex = 2;
            }
            else
            {
                factionIndex = 1;
            }

            town.SetFaction(factionIndex);
        }
    }

}