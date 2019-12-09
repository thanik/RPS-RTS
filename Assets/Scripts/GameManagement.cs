using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public enum GameMode
{
    NONE,
    LISTEN,
    SERVER,
    CLIENT
}

public class GameManagement : Singleton<GameManagement>
{
    public NetworkGameState gameState = NetworkGameState.LOBBY;
    public GameMode gameMode = GameMode.NONE;
    public float gameStartTime = 0f;
    public float gameEndTime = 0f;
    public int minimumPlayers = 1;
    public List<Color> playerColorCode = new List<Color>();
    public Dictionary<NetworkObjectType, int> maxHealth = new Dictionary<NetworkObjectType, int>() {
        { NetworkObjectType.BUILDING, 1000 },
        { NetworkObjectType.UNIT, 100 }
    };
    public float[] unitActionCooldown = new float[] {
        1f,
        0.75f,
        0.5f,
        0.25f,
        0.15f
    };
    public float[] buildingActionCooldown = new float[] {
        2.5f,
        2.4f,
        2.3f,
        2.15f,
        2f
    };

    public int maxUnitsPerPlayer = 75;

    public float buildingUpgradingCooldown = 30f;

    public Vector3[,] buildingSpawnPoints = {
        { new Vector3(-18f, 12f), new Vector3(-14f, 12f), new Vector3(-10f, 12f) },
        { new Vector3(-18f, -12f), new Vector3(-14f, -12f), new Vector3(-10f, -12f) },
        { new Vector3(18f, 12f), new Vector3(14f, 12f), new Vector3(10f, 12f) },
        { new Vector3(18f, -12f), new Vector3(14f, -12f), new Vector3(10f, -12f) }
    };

    public List<Sprite> buildingSprites;
    public List<Sprite> unitSprites;
    public Sprite ruinedBuilding;
    public List<Sprite> levelNumberSprites;

    public GameObject unitPrefab;
    public GameObject buildingPrefab;
    public Dictionary<int, PlayerData> playerData = new Dictionary<int, PlayerData>();
    private int numberOfLosingPlayers = 0;

    private GameObject buildingsGroup;
    private GameObject unitsGroup;
    private GameObject gameOverPanel;

    // Start is called before the first frame update
    void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        if (gameMode == GameMode.CLIENT)
        {
            if (gameState == NetworkGameState.LOBBY && gameStartTime > 0 && (gameStartTime - NetworkClientManager.Instance.networkGameTime < 0))
            {
                startGame();
            }
        }
        else
        {
            if (gameState == NetworkGameState.LOBBY && gameStartTime > 0 && (gameStartTime - NetworkServerManager.Instance.networkGameTime < 0))
            {
                startGame();
            }

            if (gameState == NetworkGameState.PLAYING)
            {
                // update playerData
                Dictionary<int, int> totalHealth = new Dictionary<int, int>();
                Dictionary<int, int> numberOfUnits = new Dictionary<int, int>();
                numberOfLosingPlayers = 0;
                foreach (NetworkClient client in NetworkServerManager.Instance.netClients)
                {
                    numberOfUnits[client.clientID] = 0;
                    totalHealth[client.clientID] = 0;
                }
                foreach (NetworkObject netObj in NetworkServerManager.Instance.netObjs)
                {
                    if (netObj.health > 0)
                    {
                        totalHealth[netObj.clientOwnerID] += netObj.health;
                    }

                    if (netObj.objectType == NetworkObjectType.UNIT && netObj.health > 0)
                    {
                        
                        numberOfUnits[netObj.clientOwnerID] += 1;
                    }
                }

                foreach (KeyValuePair<int, PlayerData> playerData in playerData)
                {
                    if (numberOfUnits.ContainsKey(playerData.Key))
                    {
                        playerData.Value.numberOfUnits = numberOfUnits[playerData.Key];
                    }

                    if (totalHealth.ContainsKey(playerData.Key))
                    {
                        playerData.Value.overallHealth = totalHealth[playerData.Key];
                        if (totalHealth[playerData.Key] <= 0)
                        {
                            //Debug.Log("Losing player: " + playerData.Key + ":" + totalHealth[playerData.Key]);
                            numberOfLosingPlayers += 1;
                        }
                    }
                }

                if (NetworkServerManager.Instance.netClients.Count - numberOfLosingPlayers == 1 && gameState == NetworkGameState.PLAYING && gameEndTime == 0f)
                {
                    gameEndTime = NetworkServerManager.Instance.networkGameTime + 1.5f;
                    //Debug.Log("GAME END TRIGGERED: " + NetworkServerManager.Instance.netClients.Count + ":" + numberOfLosingPlayers);
                }

                if (gameEndTime > 0f && NetworkServerManager.Instance.networkGameTime > gameEndTime)
                {
                    gameState = NetworkGameState.END;
                    foreach (KeyValuePair<int, PlayerData> eachPlayerData in playerData)
                    {
                        if (eachPlayerData.Value.overallHealth > 0)
                        {
                            NetworkServerManager.Instance.sendGameOverResult(eachPlayerData.Value.clientID);
                            break;
                        }
                        
                    }
                }
            }
        }

        if (gameState == NetworkGameState.PLAYING)
        {
            if (!gameOverPanel)
            {
                gameOverPanel = GameObject.Find("GameResultPanel");
            }
            else
            {
                gameOverPanel.SetActive(false);
            }
            
        }
        else if (gameState == NetworkGameState.END)
        {
            gameOverPanel.SetActive(true);
        }
    }

    public void startGame()
    {

        gameState = NetworkGameState.PLAYING;

        if (gameMode == GameMode.SERVER || gameMode == GameMode.LISTEN)
        {
            StartCoroutine(loadGameScene());
        }
        else
        {
            SceneManager.LoadScene(1);
        }

    }

    #region Client
    public void instantiateFromFullUpdate(List<NetworkObjectSnapshot> newNetObjs, float snapshotTime)
    {
        if (!buildingsGroup && !unitsGroup)
        {
            buildingsGroup = GameObject.Find("Buildings");
            unitsGroup = GameObject.Find("Units");
        }
        foreach (NetworkObjectSnapshot updateNetObj in newNetObjs)
        {
            GameObject newGameObj;

            if (updateNetObj.objectType == NetworkObjectType.BUILDING)
            {
                newGameObj = Instantiate(buildingPrefab, buildingsGroup.transform);
                switch (updateNetObj.unitType)
                {
                    case NetworkUnitType.ROCK:
                        newGameObj.GetComponent<SpriteRenderer>().sprite = buildingSprites[(updateNetObj.clientOwnerID * 3)];
                        break;
                    case NetworkUnitType.PAPER:
                        newGameObj.GetComponent<SpriteRenderer>().sprite = buildingSprites[(updateNetObj.clientOwnerID * 3) + 1];
                        break;
                    case NetworkUnitType.SCISSORS:
                        newGameObj.GetComponent<SpriteRenderer>().sprite = buildingSprites[(updateNetObj.clientOwnerID * 3) + 2];
                        break;
                }
            }
            else if (updateNetObj.objectType == NetworkObjectType.UNIT)
            {
                newGameObj = Instantiate(unitPrefab, unitsGroup.transform);
                switch (updateNetObj.unitType)
                {
                    case NetworkUnitType.ROCK:
                        newGameObj.GetComponent<SpriteRenderer>().sprite = unitSprites[(updateNetObj.clientOwnerID * 3)];
                        break;
                    case NetworkUnitType.PAPER:
                        newGameObj.GetComponent<SpriteRenderer>().sprite = unitSprites[(updateNetObj.clientOwnerID * 3) + 1];
                        break;
                    case NetworkUnitType.SCISSORS:
                        newGameObj.GetComponent<SpriteRenderer>().sprite = unitSprites[(updateNetObj.clientOwnerID * 3) + 2];
                        break;
                }
            }
            else
            {
                newGameObj = null;
            }

            if (newGameObj)
            {
                newGameObj.transform.position = updateNetObj.currentPosition;
                NetworkObject newNetObj = newGameObj.GetComponent<NetworkObject>();
                newNetObj.health = updateNetObj.health;
                newNetObj.clientOwnerID = updateNetObj.clientOwnerID;
                newNetObj.cooldownTime = updateNetObj.cooldownTime;
                newNetObj.currentAction = updateNetObj.currentAction;
                newNetObj.objectIDTarget = updateNetObj.objectIDTarget;
                newNetObj.positionTarget = updateNetObj.positionTarget;
                newNetObj.objectID = updateNetObj.objectID;
                newNetObj.unitType = updateNetObj.unitType;
                newNetObj.objectType = updateNetObj.objectType;
                newNetObj.objectLevel = updateNetObj.objectLevel;
                newNetObj.positionQueue.Add(updateNetObj.currentPosition);
                newNetObj.networkTimeQueue.Add(snapshotTime);

            }

            if (gameMode == GameMode.LISTEN)
            {
                newGameObj.GetComponent<SpriteRenderer>().enabled = false;
            }
        }
    }

    public void instantiateFromSnapshot(NetworkObjectSnapshot snapshot, float snapshotTime)
    {
        if (!buildingsGroup && !unitsGroup)
        {
            buildingsGroup = GameObject.Find("Buildings");
            unitsGroup = GameObject.Find("Units");
        }
        GameObject newGameObj;

        if (snapshot.objectType == NetworkObjectType.BUILDING)
        {
            newGameObj = Instantiate(buildingPrefab, buildingsGroup.transform);
            switch (snapshot.unitType)
            {
                case NetworkUnitType.ROCK:
                    newGameObj.GetComponent<SpriteRenderer>().sprite = buildingSprites[(snapshot.clientOwnerID * 3)];
                    break;
                case NetworkUnitType.PAPER:
                    newGameObj.GetComponent<SpriteRenderer>().sprite = buildingSprites[(snapshot.clientOwnerID * 3) + 1];
                    break;
                case NetworkUnitType.SCISSORS:
                    newGameObj.GetComponent<SpriteRenderer>().sprite = buildingSprites[(snapshot.clientOwnerID * 3) + 2];
                    break;
            }
        }
        else if (snapshot.objectType == NetworkObjectType.UNIT)
        {
            newGameObj = Instantiate(unitPrefab, unitsGroup.transform);
            switch (snapshot.unitType)
            {
                case NetworkUnitType.ROCK:
                    newGameObj.GetComponent<SpriteRenderer>().sprite = unitSprites[(snapshot.clientOwnerID * 3)];
                    break;
                case NetworkUnitType.PAPER:
                    newGameObj.GetComponent<SpriteRenderer>().sprite = unitSprites[(snapshot.clientOwnerID * 3) + 1];
                    break;
                case NetworkUnitType.SCISSORS:
                    newGameObj.GetComponent<SpriteRenderer>().sprite = unitSprites[(snapshot.clientOwnerID * 3) + 2];
                    break;
            }
        }
        else
        {
            newGameObj = null;
        }

        if (newGameObj)
        {
            //newGameObj.transform.position = snapshot.currentPosition;
            NetworkObject newNetObj = newGameObj.GetComponent<NetworkObject>();
            newNetObj.positionQueue.Insert(0, snapshot.currentPosition);
            newNetObj.networkTimeQueue.Insert(0, snapshotTime);
            newNetObj.lastSnapshotTime = snapshotTime;
            newNetObj.health = snapshot.health;
            newNetObj.clientOwnerID = snapshot.clientOwnerID;
            newNetObj.cooldownTime = snapshot.cooldownTime;
            newNetObj.currentAction = snapshot.currentAction;
            newNetObj.objectIDTarget = snapshot.objectIDTarget;
            newNetObj.positionTarget = snapshot.positionTarget;
            newNetObj.objectID = snapshot.objectID;
            newNetObj.unitType = snapshot.unitType;
            newNetObj.objectType = snapshot.objectType;
            newNetObj.objectLevel = snapshot.objectLevel;

        }

        if (gameMode == GameMode.LISTEN)
        {
            newGameObj.GetComponent<SpriteRenderer>().enabled = false;
        }

    }

    public void showGameOverResult(bool win)
    {
        gameOverPanel.SetActive(true);
        if (win)
        {
            gameOverPanel.transform.Find("resultText").GetComponent<TMP_Text>().text = "You win!";
        }
        else
        {
            gameOverPanel.transform.Find("resultText").GetComponent<TMP_Text>().text = "You lose!";
        }
    }

    public void showGameOverServerShuttingDown()
    {
        gameOverPanel.SetActive(true);
        gameOverPanel.transform.Find("resultText").GetComponent<TMP_Text>().text = "Server is shutting down.";
    }
    
    #endregion

    #region Server

    public int calculateDamage(NetworkUnitType origin, NetworkUnitType target)
    {
        if (origin == NetworkUnitType.ROCK)
        {
            if (target == NetworkUnitType.ROCK)
            {
                return 10;
            }
            else if (target == NetworkUnitType.PAPER)
            {
                return 5;
            }
            else if (target == NetworkUnitType.SCISSORS)
            {
                return 35;
            }
        }
        else if (origin == NetworkUnitType.PAPER)
        {
            if (target == NetworkUnitType.ROCK)
            {
                return 35;
            }
            else if (target == NetworkUnitType.PAPER)
            {
                return 10;
            }
            else if (target == NetworkUnitType.SCISSORS)
            {
                return 5;
            }
        }
        else if (origin == NetworkUnitType.SCISSORS)
        {
            if (target == NetworkUnitType.ROCK)
            {
                return 5;
            }
            else if (target == NetworkUnitType.PAPER)
            {
                return 35;
            }
            else if (target == NetworkUnitType.SCISSORS)
            {
                return 10;
            }
        }
        return 0;
    }

    IEnumerator loadGameScene()
    {
        AsyncOperation loadingScene = SceneManager.LoadSceneAsync(1);
        while (!loadingScene.isDone)
            yield return null;
        setupGame();
        yield return new WaitForEndOfFrame();
        // all nessessary objs are initialized, send full update to every client

        foreach (NetworkClient netClient in NetworkServerManager.Instance.netClients)
        {
            NetworkServerManager.Instance.sendFullUpdate(netClient, true);
        }
    }

    // gameStartTime will be set on server
    public void setGameStartTime()
    {
        bool isAllReady = true;
        foreach (NetworkClient client in NetworkServerManager.Instance.netClients)
        {
            isAllReady = isAllReady && client.isReady;
        }
        if (isAllReady && NetworkServerManager.Instance.netClients.Count >= minimumPlayers)
        {
            GameManagement.Instance.gameStartTime = NetworkServerManager.Instance.networkGameTime + 5f;
        }
        else
        {
            GameManagement.Instance.gameStartTime = 0f;
        }
    }

    // Game will be set up on server
    public void setupGame()
    {
        if (!buildingsGroup && !unitsGroup)
        {
            buildingsGroup = GameObject.Find("Buildings");
            unitsGroup = GameObject.Find("Units");
        }
        foreach (NetworkClient client in NetworkServerManager.Instance.netClients)
        {
            PlayerData currentPlayerData = new PlayerData()
            {
                clientID = client.clientID,
                numberOfUnits = 0
            };

            // spawn players' building
            for (int buildingIndex = 0; buildingIndex < 3; buildingIndex++)
            {
                GameObject building = Instantiate(buildingPrefab, buildingsGroup.transform);
                building.transform.position = buildingSpawnPoints[currentPlayerData.clientID, buildingIndex];
                NetworkObject netObj = building.GetComponent<NetworkObject>();
                int objectID = netObj.objectID;
                if (buildingIndex == 0)
                {
                    // rock
                    currentPlayerData.rockBuildingObjectID = objectID;
                    netObj.unitType = NetworkUnitType.ROCK;
                    building.GetComponent<SpriteRenderer>().sprite = buildingSprites[(client.clientID * 3)];
                }
                else if (buildingIndex == 1)
                {
                    // paper
                    currentPlayerData.paperBuildingObjectID = objectID;
                    netObj.unitType = NetworkUnitType.PAPER;
                    building.GetComponent<SpriteRenderer>().sprite = buildingSprites[(client.clientID * 3) + 1];
                }
                else if (buildingIndex == 2)
                {
                    // scissors
                    currentPlayerData.scissorsBuildingObjectID = objectID;
                    netObj.unitType = NetworkUnitType.SCISSORS;
                    building.GetComponent<SpriteRenderer>().sprite = buildingSprites[(client.clientID * 3) + 2];
                }
                netObj.objectLevel = 0;
                netObj.objectType = NetworkObjectType.BUILDING;
                maxHealth.TryGetValue(NetworkObjectType.BUILDING, out int buildingMaxHealth);
                netObj.health = buildingMaxHealth;
                netObj.clientOwnerID = client.clientID;

                if (client.clientID % 2 == 0)
                {
                    // even
                    netObj.positionTarget = new Vector3(buildingSpawnPoints[currentPlayerData.clientID, buildingIndex].x, buildingSpawnPoints[currentPlayerData.clientID, buildingIndex].y - 1f);
                }
                else
                {
                    // odd
                    netObj.positionTarget = new Vector3(buildingSpawnPoints[currentPlayerData.clientID, buildingIndex].x, buildingSpawnPoints[currentPlayerData.clientID, buildingIndex].y + 1f);
                }

                if (gameMode == GameMode.LISTEN)
                {
                    netObj.GetComponent<SpriteRenderer>().enabled = false;
                }
            }

            playerData.Add(client.clientID, currentPlayerData);
        }

    }

    public void instantiateNewUnit(NetworkUnitType unitType, PlayerData playerData, NetworkObject buildingObj)
    {
        GameObject newUnit = Instantiate(unitPrefab, unitsGroup.transform);
        switch (unitType)
        {
            case NetworkUnitType.ROCK:
                newUnit.GetComponent<SpriteRenderer>().sprite = unitSprites[(playerData.clientID * 3)];
                break;
            case NetworkUnitType.PAPER:
                newUnit.GetComponent<SpriteRenderer>().sprite = unitSprites[(playerData.clientID * 3) + 1];
                break;
            case NetworkUnitType.SCISSORS:
                newUnit.GetComponent<SpriteRenderer>().sprite = unitSprites[(playerData.clientID * 3) + 2];
                break;
        }

        if (newUnit)
        {
            newUnit.transform.position = buildingObj.positionTarget;
            NetworkObject newNetObj = newUnit.GetComponent<NetworkObject>();
            maxHealth.TryGetValue(NetworkObjectType.UNIT, out int unitMaxHealth);
            newNetObj.health = unitMaxHealth;
            newNetObj.clientOwnerID = playerData.clientID;
            newNetObj.cooldownTime = 0f;
            newNetObj.currentAction = NetworkObjectAction.NOTHING;
            newNetObj.unitType = unitType;
            newNetObj.objectType = NetworkObjectType.UNIT;
            newNetObj.objectLevel = buildingObj.objectLevel;
            newNetObj.positionTarget = buildingObj.positionTarget;

        }

        if (gameMode == GameMode.LISTEN)
        {
            newUnit.GetComponent<SpriteRenderer>().enabled = false;
        }
    }
    #endregion
}
