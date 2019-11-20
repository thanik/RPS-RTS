using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class NetworkClientManager : Singleton<NetworkClientManager>
{
    public List<NetworkObject> netObjs = new List<NetworkObject>();
    public List<NetworkClient> netClients = new List<NetworkClient>();
    public bool predictionEnabled = true;
    public int myClientID;

    private bool isConnected = false;
    private bool connectTrigger = false;
    private bool errorTrigger = false;
    private bool disconnectTrigger = false;
    private bool shuttingDown = false;
    private string errorString = "";
    private UdpClient udpClient;
    private IPEndPoint endPoint;
    private Coroutine sendTimestampCoroutine;

    public float networkGameTime = 0;
    public float timeDelta, latency, roundTrip;
    public int bytesIn, bytesOut;

    void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        if (isConnected)
        {
            networkGameTime += Time.deltaTime;
        }

        if (connectTrigger)
        {
            GameObject.Find("PageManager").GetComponent<PageManager>().changePage(3);
            connectTrigger = false;
            
        }

        if (errorTrigger)
        {
            GameObject.Find("PageManager").GetComponent<PageManager>().showError(errorString);
            errorTrigger = false;
        }

        if (disconnectTrigger)
        {
            StopCoroutine(sendTimestampCoroutine);
            GameManagement.Instance.gameMode = GameMode.NONE;
            networkGameTime = 0f;
            netObjs.Clear();
            netClients.Clear();
            GameObject.Find("PageManager").GetComponent<PageManager>().changePage(0);
            disconnectTrigger = false;
        }
    }

    public void ReceiveClientCallback(IAsyncResult ar)
    {
        if (shuttingDown) return;
        try
        {
            byte[] receivedBytes = udpClient.EndReceive(ar, ref endPoint);
            bytesIn = receivedBytes.Length;
            NetworkMessage receivedMsg = NetworkMessageEncoderDecoder.Decode(receivedBytes);
            //Debug.Log(receivedMsg);
            if (GameManagement.Instance.gameState == NetworkGameState.LOBBY)
            {
                if (receivedMsg.msgType == NetworkMessageType.WELCOME && receivedMsg.payload is WelcomePayload)
                {
                    WelcomePayload payload = (WelcomePayload)receivedMsg.payload;
                    Debug.Log("Connected to server! Client ID: " + payload.clientID);
                    myClientID = payload.clientID;
                    isConnected = true;
                    connectTrigger = true;
                    GameManagement.Instance.gameMode = GameMode.CLIENT;
                }
                else if (receivedMsg.msgType == NetworkMessageType.ERROR && receivedMsg.payload is ErrorPayload)
                {
                    ErrorPayload payload = (ErrorPayload)receivedMsg.payload;
                    errorTrigger = true;
                    errorString = payload.errorString;
                }
                else if (receivedMsg.msgType == NetworkMessageType.LOBBYDATA && receivedMsg.payload is LobbyDataPayload)
                {
                    LobbyDataPayload payload = (LobbyDataPayload)receivedMsg.payload;
                    netClients = payload.netClients;
                    GameManagement.Instance.gameStartTime = payload.gameStartTime;
                }
            }

            if (isConnected)
            {
                if (receivedMsg.msgType == NetworkMessageType.SERVERTIME && receivedMsg.payload is ServerTimePayload)
                {
                    ServerTimePayload payload = (ServerTimePayload)receivedMsg.payload;
                    CalculateTimeDelta(payload.serverTime, payload.clientTime);
                }
                else if (receivedMsg.msgType == NetworkMessageType.SYNCTIME && receivedMsg.payload is SyncTimePayload)
                {
                    SyncTimePayload payload = (SyncTimePayload)receivedMsg.payload;
                    SyncClock(payload.serverTime);
                }
                else if (receivedMsg.msgType == NetworkMessageType.SHUTDOWN)
                {
                    errorTrigger = true;
                    errorString = "Server shutting down.";
                    disconnect();
                }
            }

            udpClient.BeginReceive(new AsyncCallback(ReceiveClientCallback), null);
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

    public void SendDisconnectCallback(IAsyncResult ar)
    {
        //Debug.Log($"number of bytes sent: {udpClient.EndSend(ar)}");
        udpClient.EndSend(ar);
        udpClient.Close();
    }

    public void SendClientCallback(IAsyncResult ar)
    {
        bytesOut = udpClient.EndSend(ar);
        udpClient.BeginReceive(new AsyncCallback(ReceiveClientCallback), null);
    }
    public void connectToServer(string nickname, string ipAddress, int port)
    {
        shuttingDown = false;
        udpClient = new UdpClient();
        byte[] sendBytes = NetworkMessageEncoderDecoder.Encode(new NetworkMessage(NetworkMessageType.JOIN, new JoinPayload { nickname = nickname }));
        Debug.Log("Connecting to: " + ipAddress + ":" + port);
        endPoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);
        udpClient.BeginSend(sendBytes, sendBytes.Length, endPoint, new AsyncCallback(SendClientCallback), null);
        sendTimestampCoroutine = StartCoroutine(SendTimestamp());
    }

    public void disconnect()
    {
        if (GameManagement.Instance.gameMode == GameMode.CLIENT)
        {
            
            if (isConnected)
            {
                shuttingDown = true;
                byte[] sendBytes = NetworkMessageEncoderDecoder.Encode(new NetworkMessage(NetworkMessageType.DISCONNECT, null));
                udpClient.BeginSend(sendBytes, sendBytes.Length, endPoint, new AsyncCallback(SendDisconnectCallback), null);
                isConnected = false;
                disconnectTrigger = true;
            }
        }

    }

    IEnumerator SendTimestamp()
    {
        if (isConnected)
        {
            byte[] sendBytes = NetworkMessageEncoderDecoder.Encode(new NetworkMessage(NetworkMessageType.CLIENTTIME, new ClientTimePayload { clientTime = networkGameTime }));
            udpClient.BeginSend(sendBytes, sendBytes.Length, endPoint, new AsyncCallback(SendCallback), null);
        }
        yield return new WaitForSeconds(5f);
        StartCoroutine(SendTimestamp());
        
    }

    void OnDestroy()
    {
        disconnect();
    }

    public void CalculateTimeDelta(float serverTime, float clientTime)
    {
        // calculate the time taken from the packet to be sent from the client and then for the server to return it //
        roundTrip = networkGameTime - clientTime;
        latency = roundTrip / 2; // the latency is half the round-trip time
        // calculate the server-delta from the server time minus the current time
        float serverDelta = serverTime - networkGameTime;
        timeDelta = serverDelta + latency; // the time-delta is the server-delta plus the latency
    }

    public void SyncClock(float serverTime)
    {
        networkGameTime = serverTime + timeDelta; // adjust current time to match clock from server
    }
    public void readyToggle()
    {
        if (netClients[myClientID].isReady)
        {
            byte[] sendBytes = NetworkMessageEncoderDecoder.Encode(new NetworkMessage(NetworkMessageType.UNREADY, null));
            udpClient.BeginSend(sendBytes, sendBytes.Length, endPoint, new AsyncCallback(SendCallback), null);
        }
        else
        {
            byte[] sendBytes = NetworkMessageEncoderDecoder.Encode(new NetworkMessage(NetworkMessageType.READY, null));
            udpClient.BeginSend(sendBytes, sendBytes.Length, endPoint, new AsyncCallback(SendCallback), null);
        }
    }
}
