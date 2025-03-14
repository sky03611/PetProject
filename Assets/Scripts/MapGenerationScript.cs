using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Pathfinding;

namespace MicroWorldNS
{
    public class MapGenerationScript : MonoBehaviour
    {
        [SerializeField] MicroWorld MicroWorldPrefab;
        [SerializeField] private AstarPath astar;
        [SerializeField] int StartSeed = 1;
        const int KeepWorldsCount = 1;

        Dictionary<int, MicroWorld> worldsBySeed = new Dictionary<int, MicroWorld>();
        MicroWorld currentWorld;

        private IEnumerator Start()
        {
            // build first world
            currentWorld = GetOrBuild(StartSeed);
            MicroWorld.FlushBuild();// force fast build mode

            // wait for the world to be built
            while (!currentWorld.IsBuilt)
                yield return null;

            // activate world
            currentWorld.Terrain.gameObject.SetActive(true);
            astar.Scan();
        }

        private MicroWorld GetOrBuild(int seed)
        {
            if (!worldsBySeed.TryGetValue(seed, out var world))
            {
                // create new MicroWorld
                world = Instantiate(MicroWorldPrefab);

                // assign seed
                world.Seed = seed;
                worldsBySeed[seed] = world;

                // start build
                world.BuildAsync();
            }

            return world;
        }

    }
}