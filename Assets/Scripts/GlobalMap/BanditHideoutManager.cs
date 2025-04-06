using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BanditHideoutManager : MonoBehaviour
{
    [Header("Bandit Control")]
    public BanditScript banditPrefab;
    public int maxActiveBandits = 3;
    public float spawnInterval = 15f;
    public float spawnRadius = 10f;

    private MeshRenderer MR;

    [SerializeField] private List<BanditScript> activeBandits = new List<BanditScript>();
    [SerializeField] private float spawnTimer;
    Transform playerTransform;

    private void Start()
    {
        MR = GetComponent<MeshRenderer>();
        spawnTimer = Random.Range (10, 100);
    }

    public bool IsPlayerNearby()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= CameraController.Instance.visibilityDistance)
        {
            return true;
        }
        return false;
    }

    private void Update()
    {
        activeBandits.RemoveAll(b => b == null);

        if (activeBandits.Count < maxActiveBandits)
        {
            spawnTimer -= Time.deltaTime * DayNightHandler.Instance.GetTimeSpeed();
            if (spawnTimer <= 0f)
            {
                SpawnBandit();
                spawnTimer = spawnInterval;
            }
        }

        if (playerTransform != null)
            MR.enabled = IsPlayerNearby();
        else
        {
            try
            {
                playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
            }
            catch
            {

            }
        }
    }

    private void SpawnBandit()
    {
        Vector3 spawnPos = transform.position + Random.insideUnitSphere * spawnRadius;
        spawnPos.y = Terrain.activeTerrain != null
            ? Terrain.activeTerrain.SampleHeight(spawnPos)
            : transform.position.y;

        BanditScript bandit = Instantiate(banditPrefab, spawnPos, Quaternion.identity);
        activeBandits.Add(bandit);
        bandit.InitializeFromHideout(this);
    }

    public void OnBanditDied (BanditScript bandit)
    {
        if (activeBandits.Contains(bandit))
            activeBandits.Remove(bandit);
    }
}