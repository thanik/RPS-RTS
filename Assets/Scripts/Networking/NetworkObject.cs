using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
public enum NetworkObjectType
{
    BUILDING,
    UNIT
}

public enum NetworkUnitType
{
    ROCK,
    PAPER,
    SCISSORS,
    NONE
}
public enum NetworkObjectAction
{
    NOTHING,
    WALKING,
    WALKTHENATTACK,
    ATTACK,
    GUARD,
    TRAINING,
    UPGRADING
}

public class NetworkObject : MonoBehaviour
{
    public int objectID;
    public int clientOwnerID;
    public NetworkObjectType objectType;
    public NetworkUnitType unitType;
    public NetworkObjectAction currentAction;
    public Vector3 positionTarget;
    public int objectIDTarget;
    public int health;
    public int objectLevel;
    public float cooldownTime;
    public List<Vector3> positionQueue = new List<Vector3>();
    public List<float> networkTimeQueue = new List<float>();
    public float lastSnapshotTime = 0f;

    public float magnitudeTarget = 0f;
    private NavMeshAgent agent;
    private GameObject gameObjectTarget;
    public GameObject levelNumber;

    void Awake()
    {
        if (GameManagement.Instance.gameMode == GameMode.SERVER || GameManagement.Instance.gameMode == GameMode.LISTEN)
        {
            // game logic in server only
            //NetworkServerManager.Instance.networkGameTime
            objectID = NetworkServerManager.Instance.addNetworkObject(this);
        }
        else
        {
            NetworkClientManager.Instance.netObjs.Add(this);
            if (objectType == NetworkObjectType.UNIT)
            {
                GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
            }
        }
    }

    void Start()
    {
        
        if (gameObject.TryGetComponent<NavMeshAgent>(out agent))
        {
            agent = GetComponent<NavMeshAgent>();
            agent.updateRotation = false;
            agent.updateUpAxis = false;
            //positionTarget = agent.destination;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (GameManagement.Instance.gameMode == GameMode.SERVER || GameManagement.Instance.gameMode == GameMode.LISTEN)
        {
            // game logic in server only
            //NetworkServerManager.Instance.networkGameTime

            // calculate magnitude from object to target
            foreach (NetworkObject netObj in NetworkServerManager.Instance.netObjs)
            {
                if (netObj.objectID == objectIDTarget)
                {
                    gameObjectTarget = netObj.gameObject;
                    Vector3 directionToTarget = netObj.transform.position - transform.position;
                    magnitudeTarget = directionToTarget.sqrMagnitude;
                    break;
                }
            }

            // action with cooldown
            if (objectType == NetworkObjectType.BUILDING)
            {
                if(health < 0)
                {
                    currentAction = NetworkObjectAction.NOTHING;
                    cooldownTime = 0f;
                }
                else
                { 
                    if (currentAction == NetworkObjectAction.TRAINING)
                    {
                        if (cooldownTime == 0f)
                        {
                            cooldownTime = NetworkServerManager.Instance.networkGameTime + GameManagement.Instance.buildingActionCooldown[objectLevel];
                        }
                        else if (NetworkServerManager.Instance.networkGameTime > cooldownTime)
                        {
                            if (unitType == NetworkUnitType.ROCK)
                            {
                                // spawn new rock unit
                                GameManagement.Instance.playerData.TryGetValue(clientOwnerID, out PlayerData playerData);
                                GameManagement.Instance.instantiateNewUnit(NetworkUnitType.ROCK, playerData, this);
                                playerData.rockTrainingQueue--;
                                if (playerData.rockTrainingQueue > 0)
                                {
                                
                                    cooldownTime = NetworkServerManager.Instance.networkGameTime + GameManagement.Instance.buildingActionCooldown[objectLevel];
                                }
                                else
                                {
                                    currentAction = NetworkObjectAction.NOTHING;
                                    //cooldownTime = 0f;
                                }
                            }
                            else if (unitType == NetworkUnitType.PAPER)
                            {
                                // spawn new paper unit

                                GameManagement.Instance.playerData.TryGetValue(clientOwnerID, out PlayerData playerData);
                                GameManagement.Instance.instantiateNewUnit(NetworkUnitType.PAPER, playerData, this);
                                playerData.paperTrainingQueue--;
                                if (playerData.paperTrainingQueue > 0)
                                {

                                    cooldownTime = NetworkServerManager.Instance.networkGameTime + GameManagement.Instance.buildingActionCooldown[objectLevel];
                                }
                                else
                                {
                                    currentAction = NetworkObjectAction.NOTHING;
                                    //cooldownTime = 0f;
                                }
                            }
                            else if (unitType == NetworkUnitType.SCISSORS)
                            {
                                // spawn new scissors unit

                                GameManagement.Instance.playerData.TryGetValue(clientOwnerID, out PlayerData playerData);
                                GameManagement.Instance.instantiateNewUnit(NetworkUnitType.SCISSORS, playerData, this);
                                playerData.scissorsTrainingQueue--;
                                if (playerData.scissorsTrainingQueue > 0)
                                {

                                    cooldownTime = NetworkServerManager.Instance.networkGameTime + GameManagement.Instance.buildingActionCooldown[objectLevel];
                                }
                                else
                                {
                                    currentAction = NetworkObjectAction.NOTHING;
                                    //cooldownTime = 0f;
                                }
                            }
                        }
                    }
                    else if (currentAction == NetworkObjectAction.UPGRADING)
                    {
                        if (cooldownTime == 0f)
                        {
                            cooldownTime = NetworkServerManager.Instance.networkGameTime + GameManagement.Instance.buildingUpgradingCooldown;
                        }
                        else if (NetworkServerManager.Instance.networkGameTime > cooldownTime)
                        {
                            if (objectLevel < 5)
                            {
                                objectLevel++;
                                if(GameManagement.Instance.maxHealth.TryGetValue(NetworkObjectType.BUILDING, out int maxHealth))
                                {
                                    health = maxHealth;
                                }
                                
                            }
                            currentAction = NetworkObjectAction.NOTHING;
                            //cooldownTime = 0f;
                        }

                    }
                    else if (currentAction == NetworkObjectAction.NOTHING)
                    {
                        cooldownTime = 0f;
                    }
                }
            }
            else if(objectType == NetworkObjectType.UNIT)
            {
                if(gameObjectTarget && currentAction == NetworkObjectAction.WALKTHENATTACK || gameObjectTarget && currentAction == NetworkObjectAction.ATTACK || gameObjectTarget && currentAction == NetworkObjectAction.GUARD)
                {
                    if (magnitudeTarget < 2.5f && gameObjectTarget && gameObjectTarget.GetComponent<NetworkObject>().clientOwnerID != clientOwnerID)
                    {
                        NetworkObject targetNetObj = gameObjectTarget.GetComponent<NetworkObject>();
                        if (cooldownTime == 0f)
                        {
                            cooldownTime = NetworkServerManager.Instance.networkGameTime + GameManagement.Instance.unitActionCooldown[objectLevel];
                        }
                        else if(NetworkServerManager.Instance.networkGameTime > cooldownTime)
                        {
                            // attack

                            targetNetObj.health -= GameManagement.Instance.calculateDamage(unitType, targetNetObj.unitType);

                            if (targetNetObj.health <= 0)
                            {
                                currentAction = NetworkObjectAction.NOTHING;
                                objectIDTarget = 0;
                                positionTarget = transform.position;
                                GetComponent<NavMeshAgent>().SetDestination(transform.position);
                            }
          
                            cooldownTime = NetworkServerManager.Instance.networkGameTime + GameManagement.Instance.unitActionCooldown[objectLevel];
                        }
                    }
                    else if(gameObjectTarget && currentAction == NetworkObjectAction.WALKTHENATTACK)
                    {
                        if (gameObjectTarget)
                        {
                            GetComponent<NavMeshAgent>().SetDestination(gameObjectTarget.transform.position);
                        }
                        cooldownTime = 0f;
                    }
                    else
                    {
                        cooldownTime = 0f;
                    }
                }
                else
                {
                    cooldownTime = 0f;
                }

            }
        }
        else if (GameManagement.Instance.gameMode == GameMode.CLIENT)
        {
            //if (positionQueue.Count > 1 && predictionEnabled.isOn)
            if (positionQueue.Count > 1 && lastSnapshotTime == NetworkClientManager.Instance.lastSnapshotTime)
            {
                float diffTime = (networkTimeQueue[1] - networkTimeQueue[0]);

                if (diffTime != 0)
                {
                    Vector3 velocity = (positionQueue[1] - positionQueue[0]) / diffTime;
                    transform.position = positionQueue[0] + (velocity * (NetworkClientManager.Instance.networkGameTime - networkTimeQueue[0]));
                }
                else
                {
                    transform.position = positionQueue[0];
                }
            }
            else if (positionQueue.Count > 0)
            {
                transform.position = positionQueue[0];
            }
            else
            {
                transform.position = new Vector3(0f, 0f);
            }

            if (health <= 0)
            {
                if (objectType == NetworkObjectType.BUILDING)
                {
                    GetComponent<SpriteRenderer>().sprite = GameManagement.Instance.ruinedBuilding;
                }
                else if(objectType == NetworkObjectType.UNIT)
                {
                    NetworkClientManager.Instance.netObjsToBeRemoved.Add(this);
                    //Destroy(this);
                }
            }

            levelNumber.GetComponent<SpriteRenderer>().sprite = GameManagement.Instance.levelNumberSprites[objectLevel];
        }
    }
}
