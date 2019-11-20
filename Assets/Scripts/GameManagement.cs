using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    public List<Color> playerColorCode = new List<Color>();
    public Dictionary<NetworkObjectType, int> maxHealth = new Dictionary<NetworkObjectType, int>() {
        { NetworkObjectType.BUILDING, 2000 },
        { NetworkObjectType.UNIT, 100 }
    };
    public Dictionary<int, float> unitActionCooldown = new Dictionary<int, float>() {
        { 1, 1f },
        { 2, 0.75f },
        { 3, 0.5f },
        { 4, 0.25f },
        { 5, 0.15f }
    };
    public Dictionary<int, float> buildingActionCooldown = new Dictionary<int, float>() {
        { 1, 2.5f },
        { 2, 2f },
        { 3, 1.5f },
        { 4, 1f },
        { 5, 0.5f }
    };

    // Start is called before the first frame update
    void Start()
    {
        playerColorCode.Add(new Color32(30, 167, 225, 255)); // blue
        playerColorCode.Add(new Color(226f / 255f, 121f / 255f, 82f / 255f, 255f / 255f)); // orange
        playerColorCode.Add(new Color(27f / 255f, 145f / 255f, 77f / 255f, 255f / 255f)); // green
        playerColorCode.Add(new Color(159f / 255f, 74f / 255f, 152f / 255f, 255f / 255f)); // purple
        DontDestroyOnLoad(gameObject);
        Debug.Log(playerColorCode[0]);
        /*Debug.Log(playerColorCode[1]);
        Debug.Log(playerColorCode[2]);
        Debug.Log(playerColorCode[3]);*/
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    #region Server
    // gameStartTime will be set on server
    public void setGameStartTime()
    {
        bool isAllReady = true;
        foreach(NetworkClient client in NetworkServerManager.Instance.netClients)
        {
            isAllReady = isAllReady && client.isReady;
        }
        if (isAllReady && NetworkServerManager.Instance.netClients.Count > 1)
        {
            GameManagement.Instance.gameStartTime = NetworkServerManager.Instance.networkGameTime + 5f;
        }
        else
        {
            GameManagement.Instance.gameStartTime = 0f;
        }
    }

    // Game will be setup on server
    public void setupGame()
    {

    }
    #endregion
}
