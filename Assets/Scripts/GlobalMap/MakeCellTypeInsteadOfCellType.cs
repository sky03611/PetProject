using MicroWorldNS;
using MicroWorldNS.Spawners;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MakeCellTypeInsteadOfCellType : BaseSpawner, IBuildPhaseHandler
{
    public string OldCellTypeName = "Field";
    public string CreateCellTypeName = "Village";
    public int MinCellCount, MaxCellCount;
    public override int Order => 560;
    public VillageManager village;

    CellType createCellType;

    public override IEnumerator Prepare(MicroWorld builder)
    {
        yield return base.Prepare(builder);

        createCellType = Builder.MapSpawner.CellTypes.FirstOrDefault(c => c.Name == CreateCellTypeName);
        if (createCellType == null)
            throw new Exception($"Cell type {0} should be descripted in MapSpawner!");

        createCellType.Chance = 0;

        yield break;
    }


    public IEnumerator OnPhaseCompleted(BuildPhase phase)
    {
        if (phase != BuildPhase.CellHeightsCreated)
            yield break;

        var count = UnityEngine.Random.Range (MinCellCount, MaxCellCount);
        var capturedCells = new HashSet<Vector2Int>();
        foreach (var hex in Map.AllHex().OrderBy(_ => UnityEngine.Random.Range(0f, 1f)))
            if (Map[hex].Height > Builder.WaterLevel)
                if (Map[hex].Type.IsPassable)
                    if (Map[hex].Type.Name == OldCellTypeName)
                        if (capturedCells.All(n => (n - hex).magnitude > 3))
                        {
                            Map[hex].Type = createCellType;
                            Map[hex].MicroNoiseScale = 0;
                            Map[hex].Height += 2;
                            var v = Instantiate (village, CellGeometry.Center(hex), Quaternion.Euler(0, UnityEngine.Random.Range (0, 359), 0));
                            v.transform.position += new Vector3(0, Map[hex].Height, 0);
                            capturedCells.Add(hex);
                            JsonReader.Instance.AddVillage(v);
                            if (--count <= 0)
                            {
                                JsonReader.Instance.GenerateVillages();
                                yield break;
                            }
                        }

        yield break;
    }
}
