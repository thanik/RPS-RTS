using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class NetworkClientManager : Singleton<NetworkClientManager>
{
    public List<NetworkObject> netObjs = new List<NetworkObject>();
    public List<NetworkObject> netObjsToBeRemoved = new List<NetworkObject>();
    public List<NetworkObjectSnapshot> netObjsToBeInstantiated = new List<NetworkObjectSnapshot>();
    public List<NetworkClient> netClients = new List<NetworkClient>();
    public bool predictionEnabled = true;
    public int myClientID;

    private bool isConnected = false;
    private bool connectTrigger = false;
    private bool errorTrigger = false;
    private bool disconnectTrigger = false;
    private bool shuttingDown = false;
    private bool fullUpdateTrigger = false;
    private bool snapshotUpdateTrigger = false;
    private bool gameOverTrigger = false;
    private string errorString = "";
    private UdpClient udpClient;
    private IPEndPoint endPoint;
    private Coroutine sendTimestampCoroutine;
    private SnapshotUpdatePayload fullUpdatePayload = null;
    private SnapshotUpdatePayload snapshotPayload = null;
    private int maxObjectID = 0;
    private int winClientID = -1;

    public float lastSnapshotTime = 0f;
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
            if (GameManagement.Instance.gameState == NetworkGameState.LOBBY)
            {
                GameObject.Find("PageManager").GetComponent<PageManager>().showError(errorString);
            }
            errorTrigger = false;
        }

        if (disconnectTrigger)
        {
            StopCoroutine(sendTimestampCoroutine);
            GameManagement.Instance.gameMode = GameMode.NONE;
            networkGameTime = 0f;
            netObjs.Clear();
            netClients.Clear();
            if (GameManagement.Instance.gameState == NetworkGameState.LOBBY)
            {
                GameObject.Find("PageManager").GetComponent<PageManager>().changePage(0);
            }
            disconnectTrigger = false;
        }

        if(fullUpdateTrigger)
        {
            fullUpdate();
            fullUpdateTrigger = false;
        }

        if(snapshotUpdateTrigger)
        {
            ProcessSnapshots(snapshotPayload.gameTime, snapshotPayload.maxObjectID, snapshotPayload.netObjects, snapshotPayload.totalObjectCount);
            GameManagement.Instance.playerData = snapshotPayload.playerData;
            snapshotUpdateTrigger = false;
        }

        if(gameOverTrigger)
        {
            GameManagement.Instance.showGameOverResult(winClientID == myClientID);
            disconnect();
            gameOverTrigger = false;
        }

        foreach (NetworkObject removingObject in netObjsToBeRemoved)
        {
            if (removingObject)
            {
                netObjs.Remove(removingObject);
                if (removingObject.gameObject.TryGetComponent<StatusBarManager>(out StatusBarManager statusBarManager))
                {
                    Destroy(statusBarManager.statusBar);
                }
                Destroy(removingObject.gameObject);
            }
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
                    Debug.Log("Got error from server: " + payload.errorString);
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
                else if (receivedMsg.msgType == NetworkMessageType.FULLUPDATE && receivedMsg.payload is SnapshotUpdatePayload)
                {
                    Debug.Log("Got full update data.");
                    fullUpdatePayload = (SnapshotUpdatePayload)receivedMsg.payload;
                    lastSnapshotTime = ((SnapshotUpdatePayload)receivedMsg.payload).gameTime;
                    fullUpdateTrigger = true;
                }
                else if (receivedMsg.msgType == NetworkMessageType.UPDATE && receivedMsg.payload is SnapshotUpdatePayload)
                {
                    snapshotPayload = (SnapshotUpdatePayload)receivedMsg.payload;
                    lastSnapshotTime = ((SnapshotUpdatePayload)receivedMsg.payload).gameTime;
                    snapshotUpdateTrigger = true;

                }
                else if (receivedMsg.msgType == NetworkMessageType.GAMEOVER && receivedMsg.payload is GameOverPayload)
                {
                    GameManagement.Instance.gameState = NetworkGameState.END;
                    winClientID = ((GameOverPayload)receivedMsg.payload).winClientID;
                    gameOverTrigger = true;
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
            byte[] sendBytes = NetworkMessageEncoderDecoder.Encode(new NetworkMessage(NetworkMessageType.CLIENTTIME, new ClientTimePayload { clientTime = networkGameTime, latency = latency }));
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

    public void sendUnitsActions(List<NetworkActionSnapshot> actions, NetworkUnitType unitType)
    {
        bool addRockTrainingQueue = false;
        bool addPaperTrainingQueue = false;
        bool addScissorsTrainingQueue = false;
        switch (unitType)
        {
            case NetworkUnitType.PAPER:
                addPaperTrainingQueue = true;
                break;
            case NetworkUnitType.ROCK:
                addRockTrainingQueue = true;
                break;
            case NetworkUnitType.SCISSORS:
                addScissorsTrainingQueue = true;
                break;
        }
        GameManagement.Instance.playerData.TryGetValue(myClientID, out PlayerData myPlayerData);
        byte[] sendBytes = NetworkMessageEncoderDecoder.Encode(new NetworkMessage(NetworkMessageType.UNITSACTIONS, new UnitsActionsPayload { addPaperTrainingQueue = addPaperTrainingQueue, addRockTrainingQueue = addRockTrainingQueue, addScissorsTrainingQueue = addScissorsTrainingQueue, actions = actions }));
        udpClient.BeginSend(sendBytes, sendBytes.Length, endPoint, new AsyncCallback(SendCallback), null);
    }

    private void fullUpdate()
    {
        if (fullUpdatePayload != null)
        {
            Debug.Log("Start full updating! (" + fullUpdatePayload.netObjects.Count + ")");
            float payloadTime = fullUpdatePayload.gameTime;
            List<NetworkObjectSnapshot> newNetObjs = fullUpdatePayload.netObjects;
            if (netObjs.Count == 0)
            {
                GameManagement.Instance.instantiateFromFullUpdate(newNetObjs, payloadTime);
                foreach (NetworkObject netObj in netObjs)
                {
                    if (netObj.objectID > maxObjectID)
                    {
                        maxObjectID = netObj.objectID;
                    }
                }
            }
            else
            {
                // need to update all objs
            }
            GameManagement.Instance.playerData = fullUpdatePayload.playerData;
        }
    }

    private void ProcessSnapshots(float snapshotTime, int newMaxObjectID, List<NetworkObjectSnapshot> snapshotObjects, int snapshotObjCount)
    {
        netObjsToBeRemoved.Clear();
        foreach (NetworkObjectSnapshot snapObj in snapshotObjects)
        {
            foreach(NetworkObject netObj in netObjs)
            {
                
                if (snapObj.objectID == netObj.objectID)
                {
                    //Debug.Log("Updating object: " + netObj.objectID + " " + snapObj.currentAction);
                    netObj.cooldownTime = snapObj.cooldownTime;
                    netObj.currentAction = snapObj.currentAction;
                    netObj.health = snapObj.health;
                    netObj.objectIDTarget = snapObj.objectIDTarget;
                    netObj.objectLevel = snapObj.objectLevel;
                    netObj.positionTarget = snapObj.positionTarget;

                    if (netObj.positionQueue.Count > 2)
                    {
                        netObj.positionQueue.RemoveAt(2);
                    }
                    netObj.positionQueue.Insert(0, snapObj.currentPosition);

                    if (netObj.networkTimeQueue.Count > 2)
                    {
                        netObj.networkTimeQueue.RemoveAt(2);
                    }
                    netObj.networkTimeQueue.Insert(0, snapshotTime);
                    netObj.lastSnapshotTime = snapshotTime;
                    if (snapObj.snapshotAction == SnapshotAction.DESTROY)
                    {
                        netObjsToBeRemoved.Add(netObj);
                    }
                }
            }

            if (snapObj.objectID > maxObjectID)
            {
                netObjsToBeInstantiated.Add(snapObj);

            }
        }

        foreach (NetworkObjectSnapshot netObjSnap in netObjsToBeInstantiated)
        {
            GameManagement.Instance.instantiateFromSnapshot(netObjSnap, snapshotTime);
        }
        netObjsToBeInstantiated.Clear();
        

        maxObjectID = newMaxObjectID;

        if(snapshotObjCount - netObjsToBeRemoved.Count != netObjs.Count)
        {
            Debug.LogWarning("Object Count OUT OF SYNC DETECTED current: " + netObjs.Count + " expected: " + (snapshotObjCount - netObjsToBeRemoved.Count));
        }
    }
}
