using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using Unity.Entities.UniversalDelegates;
using UnityEngine.Rendering.PostProcessing;

public class TerrainTextureChanger : MonoBehaviour
{
    public List<Terrain> terrains;

    private void Start()
    {
        StartCoroutine(SetupTerrains());
    }

    private IEnumerator SetupTerrains()
    {
        yield return new WaitForSeconds(2f);
        foreach (var t in FindObjectsOfType<Terrain>())
        {
            if (!terrains.Contains (t))
                terrains.Add(t);
        }
    }

    public void UpdateTextures()
    {
        foreach (var t in terrains)
        {
            UpdateTerrainTexture(t);
        }
    }

    void UpdateTerrainTexture(Terrain terrain)
    {
        Material[] sharedMaterialsCopy = terrain.GetComponent<MeshRenderer>().sharedMaterials;
        sharedMaterialsCopy[0].SetTexture("_Top", SeasonsHandler.Instance.GetCurrentMonth().groundTexture);
    }
}