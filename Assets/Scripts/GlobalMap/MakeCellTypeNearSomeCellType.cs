using MicroWorldNS;
using MicroWorldNS.Spawners;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MakeCellTypeNearSomeCellType : BaseSpawner, IBuildPhaseHandler
{
    public string NeighborCellTypeName = "Water";
    public string CreateCellTypeName = "Town";
    public int MinCellCount, MaxCellCount;
    public override int Order => 550;
    public TownManager town;

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
        // spawn createCellType near NeighborCellType cells
        foreach (var hex in Map.AllHex().OrderBy(_ => UnityEngine.Random.Range(0f, 1f)))
            if (Map[hex].Height > Builder.WaterLevel) // above water?
                if (Map[hex].Type.IsPassable)// is not border, passable?
                    if (CellGeometry.NeighborsEx(hex).Any(n => Map[n].Type.Name == NeighborCellTypeName))// is neighbor of water?
                        if (capturedCells.All (n => (n-hex).magnitude > 5))
                        {
                            Map[hex].Type = createCellType;// assign cell type
                            Map[hex].MicroNoiseScale = 0;// make cell flatten
                            Map[hex].Height += 2;// elevate a bit
                            var v = Instantiate (town, CellGeometry.Center(hex), Quaternion.Euler(0, UnityEngine.Random.Range(0, 359), 0));
                            v.transform.position += new Vector3(0, Map[hex].Height, 0);
                            capturedCells.Add(hex);
                            JsonReader.Instance.AddTown(v);
                            if (--count <= 0)
                            {
                                JsonReader.Instance.GenerateTowns();
                                yield break;
                            }
                        }

        yield break;
    }
}
