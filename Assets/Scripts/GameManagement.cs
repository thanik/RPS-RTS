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
}
