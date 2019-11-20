using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class NetworkServerManager : Singleton<NetworkServerManager>
{
    public List<NetworkObject> netObjs = new List<NetworkObject>();
    public List<NetworkClient> netClients = new List<NetworkClient>();
    public float networkGameTime = 0;

    private UdpClient udpClient;
    private IPEndPoint endPoint;
    private const int MAXPLAYER = 4;
    private Coroutine sendTimestampCoroutine;
    private bool shuttingDown = false;
    private int closedClient = 0;

    void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        if (GameManagement.Instance.gameMode == GameMode.SERVER || GameManagement.Instance.gameMode == GameMode.LISTEN)
        {
            networkGameTime += Time.deltaTime;
        }
    }

    void FixedUpdate()
    {
        if (GameManagement.Instance.gameMode == GameMode.SERVER || GameManagement.Instance.gameMode == GameMode.LISTEN)
        {
            udpClient.BeginReceive(new AsyncCallback(ReceiveServerCallback), null);
            if (GameManagement.Instance.gameState == NetworkGameState.LOBBY)
            {
                byte[] sendBytes = NetworkMessageEncoderDecoder.Encode(new NetworkMessage(NetworkMessageType.LOBBYDATA, new LobbyDataPayload { netClients = netClients, gameStartTime = GameManagement.Instance.gameStartTime }));
                foreach (NetworkClient client in netClients)
                {
                    udpClient.BeginSend(sendBytes, sendBytes.Length, (IPEndPoint)endPoint.Create(client.socketAddress), new AsyncCallback(SendCallback), null);
                }
            }
            else if (GameManagement.Instance.gameState == NetworkGameState.PLAYING)
            {
                /*byte[] sendBytes = Encoding.ASCII.GetBytes("GAME:TIME=" + ((long)(networkGameTime * 1000)) + ":OBJECT_ID_0_POSITION=" + netObjs[0].transform.position.x + "," + netObjs[0].transform.position.y);
                foreach (KeyValuePair<IPEndPoint, int> kvp in netClients)
                {
                    udpClient.BeginSend(sendBytes, sendBytes.Length, kvp.Key, new AsyncCallback(SendCallback), null);
                }*/
            }
        }

    }
    public void setupServer(int portNumber)
    {
        shuttingDown = false;
        endPoint = new IPEndPoint(IPAddress.Any, portNumber);
        udpClient = new UdpClient(endPoint);
        udpClient.BeginReceive(new AsyncCallback(ReceiveServerCallback), null);
        GameManagement.Instance.gameMode = GameMode.SERVER;
        sendTimestampCoroutine = StartCoroutine(SendServerTimestamp());
    }

    public void stopServer()
    {
        if (GameManagement.Instance.gameMode == GameMode.SERVER || GameManagement.Instance.gameMode == GameMode.LISTEN)
        {
            shuttingDown = true;
            StopCoroutine(sendTimestampCoroutine);
            byte[] sendBytes = NetworkMessageEncoderDecoder.Encode(new NetworkMessage(NetworkMessageType.SHUTDOWN, null));
            if (netClients.Count == 0)
            {
                udpClient.Close();
            }
            else
            {
                foreach (NetworkClient client in netClients)
                {
                    udpClient.BeginSend(sendBytes, sendBytes.Length, (IPEndPoint)endPoint.Create(client.socketAddress), new AsyncCallback(ShutdownCallback), null);
                }
            }
            GameManagement.Instance.gameMode = GameMode.NONE;
            networkGameTime = 0f;
            GameObject.Find("PageManager").GetComponent<PageManager>().changePage(0);
        }
    }

    public void ShutdownCallback(IAsyncResult ar)
    {
        udpClient.EndSend(ar);
        closedClient++;
        if (closedClient == netClients.Count)
        {
            closedClient = 0;
            netObjs.Clear();
            netClients.Clear();
            udpClient.Close();
        }
    }

    private void OnDestroy()
    {
        stopServer();
    }

    public void ReceiveServerCallback(IAsyncResult ar)
    {
        if (shuttingDown) return;
        try
        {
            byte[] receivedBytes = udpClient.EndReceive(ar, ref endPoint);
            NetworkMessage receivedMsg = NetworkMessageEncoderDecoder.Decode(receivedBytes);
            //Debug.Log(receivedMsg);
            if (receivedMsg.msgType == NetworkMessageType.JOIN && receivedMsg.payload is JoinPayload)
            {
                JoinPayload payload = (JoinPayload)receivedMsg.payload;
                int newClientID = netClients.Count;
                Debug.Log("New client connection! Nickname: " + payload.nickname);

                if (GameManagement.Instance.gameState == NetworkGameState.LOBBY)
                {
                    if (netClients.Count < MAXPLAYER)
                    {
                        byte[] sendBytes = NetworkMessageEncoderDecoder.Encode(new NetworkMessage(NetworkMessageType.WELCOME, new WelcomePayload { clientID = newClientID }));
                        netClients.Add(new NetworkClient(newClientID, endPoint.Serialize(), payload.nickname));
                        udpClient.BeginSend(sendBytes, sendBytes.Length, endPoint, new AsyncCallback(SendCallback), null);
                    }
                    else
                    {
                        byte[] sendBytes = NetworkMessageEncoderDecoder.Encode(new NetworkMessage(NetworkMessageType.ERROR, new ErrorPayload { errorString = "The server is full." }));
                        udpClient.BeginSend(sendBytes, sendBytes.Length, endPoint, new AsyncCallback(SendCallback), null);
                    }
                }
                else
                {
                    byte[] sendBytes = NetworkMessageEncoderDecoder.Encode(new NetworkMessage(NetworkMessageType.ERROR, new ErrorPayload { errorString = "The game is already in progress." }));
                    udpClient.BeginSend(sendBytes, sendBytes.Length, endPoint, new AsyncCallback(SendCallback), null);
                }
            }
            else if (receivedMsg.msgType == NetworkMessageType.READY)
            {
                if (GameManagement.Instance.gameState == NetworkGameState.LOBBY)
                {
                    Debug.Log("Player ready! " + NetworkMessageEncoderDecoder.findClientByAddress(endPoint, netClients).clientID);
                    NetworkMessageEncoderDecoder.findClientByAddress(endPoint, netClients).isReady = true;
                    GameManagement.Instance.setGameStartTime();
                }
            }
            else if (receivedMsg.msgType == NetworkMessageType.UNREADY)
            {
                if (GameManagement.Instance.gameState == NetworkGameState.LOBBY)
                {
                    Debug.Log("Player unready! " + NetworkMessageEncoderDecoder.findClientByAddress(endPoint, netClients).clientID);
                    NetworkMessageEncoderDecoder.findClientByAddress(endPoint, netClients).isReady = false;
                    GameManagement.Instance.setGameStartTime();
                }
            }
            else if (receivedMsg.msgType == NetworkMessageType.CLIENTTIME && receivedMsg.payload is ClientTimePayload)
            {
                ClientTimePayload payload = (ClientTimePayload)receivedMsg.payload;
                byte[] sendBytes = NetworkMessageEncoderDecoder.Encode(new NetworkMessage(NetworkMessageType.SERVERTIME, new ServerTimePayload { serverTime = networkGameTime, clientTime = payload.clientTime }));
                udpClient.BeginSend(sendBytes, sendBytes.Length, endPoint, new AsyncCallback(SendCallback), null);
            }
            else if (receivedMsg.msgType == NetworkMessageType.DISCONNECT)
            {
                foreach (NetworkClient client in netClients)
                {
                    if (client.socketAddress.Equals(endPoint.Serialize()))
                    {
                        netClients.Remove(client);
                        break;
                    }
                }
            }
                udpClient.BeginReceive(new AsyncCallback(ReceiveServerCallback), null);
            }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }
    public void SendCallback(IAsyncResult ar)
    {
        //Debug.Log($"number of bytes sent: {udpClient.EndSend(ar)}");
        udpClient.EndSend(ar);
    }

    IEnumerator SendServerTimestamp()
    {
        byte[] sendBytes = NetworkMessageEncoderDecoder.Encode(new NetworkMessage(NetworkMessageType.SYNCTIME, new SyncTimePayload { serverTime = networkGameTime }));
        foreach (NetworkClient client in netClients)
        {
            udpClient.BeginSend(sendBytes, sendBytes.Length, (IPEndPoint) endPoint.Create(client.socketAddress), new AsyncCallback(SendCallback), null);
        }
        yield return new WaitForSeconds(5f);
        StartCoroutine(SendServerTimestamp());
    }

    public int addNetworkObject(NetworkObject netObj)
    {
        if (netObjs.Contains(netObj))
        {
            return netObjs.IndexOf(netObj);
        }
        else
        {
            int newId = netObjs.Count;
            netObjs.Add(netObj);
            return newId;
        }
    }
}
