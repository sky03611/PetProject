using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Pathfinding;
using Unity.VisualScripting;

namespace MicroWorldNS
{
    public class MapGenerationScript : MonoBehaviour
    {
        [SerializeField] MicroWorld MicroWorldPrefab;
        [SerializeField] private AstarPath astar;
        [SerializeField] private GameObject player;
        [SerializeField] int StartSeed = 1;
        const int KeepWorldsCount = 1;

        Dictionary<int, MicroWorld> worldsBySeed = new Dictionary<int, MicroWorld>();
        MicroWorld currentWorld;

        private IEnumerator Start()
        {
            StartSeed = UnityEngine.Random.Range(46000, 47000);
            currentWorld = GetOrBuild(StartSeed);
            MicroWorld.FlushBuild();

            while (!currentWorld.IsBuilt)
                yield return null;

            currentWorld.Terrain.gameObject.SetActive(true);
            player.SetActive(true);
            astar.Scan();
        }

        private MicroWorld GetOrBuild(int seed)
        {
            if (!worldsBySeed.TryGetValue(seed, out var world))
            {
                world = Instantiate(MicroWorldPrefab);

                world.Seed = seed;
                worldsBySeed[seed] = world;

                world.BuildAsync();
            }

            return world;
        }

    }
}