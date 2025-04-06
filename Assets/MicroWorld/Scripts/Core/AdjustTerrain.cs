using UnityEngine;

[RequireComponent(typeof(Terrain))]
public class AdjustTerrain : MonoBehaviour
{
    public Terrain terrain;

    void Start()
    {
        terrain.detailObjectDistance = 200f;
    }
}