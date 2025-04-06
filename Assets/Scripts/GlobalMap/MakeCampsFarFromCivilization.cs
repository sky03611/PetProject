using MicroWorldNS;
using MicroWorldNS.Spawners;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MakeCampsFarFromCivilization : BaseSpawner, IBuildPhaseHandler
{
    public string CreateCellTypeName = "Camp";
    public string AvoidCellType1 = "Town";
    public string AvoidCellType2 = "Village";
    public int MinCellCount = 1, MaxCellCount = 3;
    public float MinDistanceFromCivilization = 10f;
    public GameObject banditHideout;

    CellType createCellType;

    public override int Order => 680; // after towns, villages, and roads

    public override IEnumerator Prepare(MicroWorld builder)
    {
        yield return base.Prepare(builder);

        createCellType = Builder.MapSpawner.CellTypes.FirstOrDefault(c => c.Name == CreateCellTypeName);
        if (createCellType == null)
            throw new Exception($"Cell type {CreateCellTypeName} should be described in MapSpawner!");

        createCellType.Chance = 0;
    }

    public IEnumerator OnPhaseCompleted(BuildPhase phase)
    {
        if (phase != BuildPhase.CellHeightsCreated)
            yield break;

        int count = UnityEngine.Random.Range(MinCellCount, MaxCellCount + 1);
        var capturedCells = new HashSet<Vector2Int>();

        var avoidCells = Map.AllHex()
            .Where(h => Map[h].Type.Name == AvoidCellType1 || Map[h].Type.Name == AvoidCellType2)
            .ToList();

        var roadCells = Map.AllHex()
            .Where(h => Map[h].HasContent(CellContent.IsRoad))
            .ToList();

        float minDistanceToRoad = 3;
        float maxDistanceToRoad = 6;

        foreach (var hex in Map.AllHex().OrderBy(_ => UnityEngine.Random.Range(0f, 1f)))
        {
            var cell = Map[hex];

            if (cell.Height <= Builder.WaterLevel)
                continue;

            if (!cell.Type.IsPassable || cell.HasContent(CellContent.IsRoad))
                continue;

            if (avoidCells.Any(n => (n - hex).magnitude < MinDistanceFromCivilization))
                continue;

            bool isNearRoad = roadCells.Any(r =>
            {
                float dist = (r - hex).magnitude;
                return dist >= minDistanceToRoad && dist <= maxDistanceToRoad;
            });

            if (!isNearRoad)
                continue;

            if (capturedCells.All(n => (n - hex).magnitude > 6))
            {
                Map[hex].Type = createCellType;
                Map[hex].MicroNoiseScale = 0;
                Map[hex].Height += 2;

                var hideout = Instantiate(
                    banditHideout,
                    CellGeometry.Center(hex),
                    Quaternion.Euler(0, UnityEngine.Random.Range(0f, 359f), 0)
                );
                hideout.transform.position += new Vector3(0, Map[hex].Height, 0);

                capturedCells.Add(hex);

                if (--count <= 0)
                    break;
            }
        }

        yield break;
    }
}