using UnityEngine;
using Pathfinding;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class PlayerController : Singleton<PlayerController>
{
    [SerializeField] private GameObject movementMarker;
    [SerializeField] private Transform dynamicTarget = null;
    [SerializeField] private int money, reputation;
    [SerializeField] private Inventory inventory;
    [SerializeField] private Sprite thisPortrait;
    [SerializeField] private BanditScript bandit;

    Camera _mainCamera;
    FollowerEntity ai;

    public Sprite GetPortrait()
    {
        return thisPortrait;
    }

    void OnEnable()
    {
        _mainCamera = Camera.main;
        ai = GetComponent<FollowerEntity>();
    }

    private void Start()
    {
        inventory.owner = gameObject;
    }

    private float SetMovementSpeed()
    {
        float basicSpeed = 5f;
        int currentTag = GetCurrentNodeTag();
        if (currentTag == 1) //Road
            basicSpeed *= 1.25f;
        return basicSpeed;
    }

    private int GetCurrentNodeTag()
    {
        var graph = AstarPath.active.graphs[0];
        NNInfo nearestNodeInfo = graph.GetNearest(transform.position);
        return (int)nearestNodeInfo.node.Tag;
    }

    void Update()
    {
        if (EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }
        if (DialogueManager.Instance.isDialogueOpen || InterfaceHandler.Instance.isMenuOpen)
        {
            return;
        }
        if (Input.GetKeyDown(KeyCode.K))
        {
            Instantiate(bandit, transform.position, transform.rotation);
        }
        if (dynamicTarget != null)
        {
            ai.destination = dynamicTarget.transform.position;
            movementMarker.transform.position = dynamicTarget.transform.position;
        }
        if (Input.GetMouseButtonDown (0))
        {
            var mousePosition = Input.mousePosition;

            var ray = _mainCamera.ScreenPointToRay(mousePosition);

            if (Physics.Raycast(ray, out var hit))
            {
                if (hit.transform.gameObject.layer != 4)
                {
                    movementMarker.SetActive(true);
                    if (hit.transform.gameObject.GetComponent<VillagerScript>() == null)
                    {
                        dynamicTarget = null;
                        movementMarker.transform.position = hit.point;
                        ai.destination = hit.point;
                    }
                    else
                    {
                        dynamicTarget = hit.transform.gameObject.transform;
                    }
                }
            }
        }
        if (ai.reachedDestination)
        {
            ResetMarker();
        }
        ai.maxSpeed = SetMovementSpeed();
    }

    public void ResetMarker()
    {
        movementMarker.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (dynamicTarget != null)
        {
            if (other.transform == dynamicTarget.transform)
            {
                var npc = other.GetComponent<VillagerScript>();
                if (npc != null)
                {
                    DialogueManager.Instance.LoadDialogue(npc.GetGreetingsDialogue(), npc);
                    dynamicTarget = null;
                }
            }
        }
        if (other.GetComponent<TownManager>() != null)
        {
            if (Vector3.Distance (movementMarker.transform.position, 
                new Vector3 (other.transform.position.x, movementMarker.transform.position.y, other.transform.position.z)) <= 5)
            {
                InterfaceHandler.Instance.OnSettlementVisit(other.GetComponent<TownManager>());
            }
        }
    }

    public int GetCurrentMoney()
    {
        return money;
    }

    public void ChangeMoney (int amount)
    {
        money += amount;
        if (money < 0)
        {
            money = 0;
        }
    }

    public int GetReputation()
    {
        return reputation;
    }

    public void ChangeReputation(int amount)
    {
        reputation += amount;
    }

    public bool HasEnoughMoney (int amount)
    {
        return money >= amount;
    }

    public Inventory GetInventory()
    {
        return inventory;
    }

    public void SetInventory(Inventory _inventory)
    {
        inventory = new Inventory(_inventory);
    }
}