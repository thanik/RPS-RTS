using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.AI;

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
    private int currentObjectID = 0;
    private bool processActionTrigger = false;
    private List<NetworkObject> processActionQueue = new List<NetworkObject>();
    public List<NetworkObject> netObjToBeDestroyed = new List<NetworkObject>();

    private List<NetworkObjectSnapshot> latestObjsSnapshot = new List<NetworkObjectSnapshot>();

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
            udpClient.BeginReceive(new AsyncCallback(ReceiveServerCallback), null);
        }

        if (processActionTrigger)
        {
            foreach (NetworkObject netObj in processActionQueue)
            {
                if (netObj.gameObject)
                {
                    if (netObj.currentAction == NetworkObjectAction.GUARD || netObj.currentAction == NetworkObjectAction.NOTHING || netObj.currentAction == NetworkObjectAction.ATTACK)
                    {
                        netObj.GetComponent<NavMeshAgent>().isStopped = true;
                    }
                    else
                    {
                        netObj.GetComponent<NavMeshAgent>().SetDestination(netObj.positionTarget);
                        netObj.GetComponent<NavMeshAgent>().isStopped = false;
                    }
                }
            }
            processActionQueue.Clear();
            processActionTrigger = false;
        }
    }

    void FixedUpdate()
    {
        if (GameManagement.Instance.gameMode == GameMode.SERVER || GameManagement.Instance.gameMode == GameMode.LISTEN)
        {
            //udpClient.BeginReceive(new AsyncCallback(ReceiveServerCallback), null);
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
                List<NetworkObjectSnapshot> objSnaps = gatherDiffSnapshot();
                //if (objSnaps.Count > 0)
                //{
                //    Debug.Log("Diff Update snapshot: " + objSnaps.Count);
                //}

                // calculate max object ID
                int maxObjectID = 0;
                foreach (NetworkObject netObj in netObjs)
                {
                    if (netObj.objectID > maxObjectID)
                    {
                        maxObjectID = netObj.objectID;
                    }
                }

                byte[] sendBytes = NetworkMessageEncoderDecoder.Encode(new NetworkMessage(NetworkMessageType.UPDATE, new SnapshotUpdatePayload { gameTime = networkGameTime, netObjects = objSnaps, playerData = GameManagement.Instance.playerData, maxObjectID = maxObjectID, totalObjectCount = netObjs.Count })); ;
                foreach (NetworkClient client in netClients)
                {
                    udpClient.BeginSend(sendBytes, sendBytes.Length, (IPEndPoint)endPoint.Create(client.socketAddress), new AsyncCallback(SendCallback), null);
                }

                // destroy object in queue
                foreach (NetworkObject netObj in netObjToBeDestroyed)
                {

                    //netObjs.Remove(netObj);
                    //if (netObj.TryGetComponent<StatusBarManager>(out StatusBarManager statusBarManager))
                    //{
                    //    Destroy(statusBarManager.statusBar);
                    //}
                    //Destroy(netObj.gameObject);
                    netObj.gameObject.GetComponent<Rigidbody2D>().simulated = false;
                    netObj.gameObject.GetComponent<BoxCollider2D>().enabled = false;
                    netObj.gameObject.GetComponent<SpriteRenderer>().enabled = false;
                    netObj.gameObject.GetComponent<NavMeshAgent>().enabled = false;
                    netObj.gameObject.GetComponentInChildren<CircleCollider2D>().enabled = false;
                }
                netObjToBeDestroyed.Clear();
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

    public void sendFullUpdate(NetworkClient client, bool setLatestSnapshots)
    {
        List<NetworkObjectSnapshot> netSnapObjs = gatherFullSnapshot();
        Debug.Log("Full Update snapshot: " + netSnapObjs.Count);
        if (setLatestSnapshots)
        {
            latestObjsSnapshot = netSnapObjs;
        }

        // calculate max object ID
        int maxObjectID = 0;
        foreach (NetworkObject netObj in netObjs)
        {
            if (netObj.objectID > maxObjectID)
            {
                maxObjectID = netObj.objectID;
            }
        }

        byte[] sendBytes = NetworkMessageEncoderDecoder.Encode(new NetworkMessage(NetworkMessageType.FULLUPDATE, new SnapshotUpdatePayload { gameTime = networkGameTime, netObjects = netSnapObjs, playerData = GameManagement.Instance.playerData, maxObjectID = maxObjectID, totalObjectCount = netObjs.Count })); ;
        udpClient.BeginSend(sendBytes, sendBytes.Length, (IPEndPoint)endPoint.Create(client.socketAddress), new AsyncCallback(SendCallback), null);
    }

    private List<NetworkObjectSnapshot> gatherFullSnapshot()
    {
        List<NetworkObjectSnapshot> netSnapObjs = new List<NetworkObjectSnapshot>();
        foreach(NetworkObject netObj in netObjs)
        {
            NetworkObjectSnapshot newObjSnap = new NetworkObjectSnapshot(netObj);
            netSnapObjs.Add(newObjSnap);
        }
        return netSnapObjs;
    }

    private List<NetworkObjectSnapshot> gatherDiffSnapshot()
    {
        List<NetworkObjectSnapshot> newSnapObjs = gatherFullSnapshot();
        List<NetworkObjectSnapshot> diffSnapObjs = newSnapObjs.Except(latestObjsSnapshot, new NetworkObjectSnapshotComparer()).ToList();
        latestObjsSnapshot = newSnapObjs;
        return diffSnapObjs;
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
            if (GameManagement.Instance.gameState == NetworkGameState.LOBBY)
            {
                GameObject.Find("PageManager").GetComponent<PageManager>().changePage(0);
            }
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
            else if (receivedMsg.msgType == NetworkMessageType.UNITSACTIONS && receivedMsg.payload is UnitsActionsPayload)
            {
                if (GameManagement.Instance.gameState == NetworkGameState.PLAYING)
                {
                    NetworkClient sender = NetworkMessageEncoderDecoder.findClientByAddress(endPoint, netClients);
                    UnitsActionsPayload payload = (UnitsActionsPayload)receivedMsg.payload;
                    if (payload.actions != null)
                    {
                        foreach (NetworkActionSnapshot action in payload.actions)
                        {
                            foreach (NetworkObject netObj in netObjs)
                            {
                                if (netObj.objectID == action.objectID && sender.clientID == netObj.clientOwnerID)
                                {
                                    netObj.currentAction = action.action;
                                    if (netObj.objectType == NetworkObjectType.UNIT)
                                    {
                                        netObj.objectIDTarget = action.objectIDTarget;
                                        netObj.positionTarget = action.positionTarget;
                                        processActionQueue.Add(netObj);
                                    }
                                }
                            }
                        }
                        processActionTrigger = true;
                    }
                    if(GameManagement.Instance.playerData.TryGetValue(sender.clientID, out PlayerData playerData))
                    {
                        if(payload.addPaperTrainingQueue)
                        {
                            playerData.paperTrainingQueue++;
                        }
                        else if(payload.addRockTrainingQueue)
                        {
                            playerData.rockTrainingQueue++;
                        }
                        else if(payload.addScissorsTrainingQueue)
                        {
                            playerData.scissorsTrainingQueue++;
                        }
                    }
                   
                }
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
            //udpClient.BeginReceive(new AsyncCallback(ReceiveServerCallback), null);
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
        yield return new WaitForSeconds(2f);
        StartCoroutine(SendServerTimestamp());
    }

    public void sendGameOverResult(int winClientID)
    {
        try
        {
            List<NetworkObjectSnapshot> objSnaps = gatherDiffSnapshot();
            //if (objSnaps.Count > 0)
            //{
            //    Debug.Log("Diff Update snapshot: " + objSnaps.Count);
            //}

            // calculate max object ID
            int maxObjectID = 0;
            foreach (NetworkObject netObj in netObjs)
            {
                if (netObj.objectID > maxObjectID)
                {
                    maxObjectID = netObj.objectID;
                }
            }

            byte[] sendBytes = NetworkMessageEncoderDecoder.Encode(new NetworkMessage(NetworkMessageType.UPDATE, new SnapshotUpdatePayload { gameTime = networkGameTime, netObjects = objSnaps, playerData = GameManagement.Instance.playerData, maxObjectID = maxObjectID, totalObjectCount = netObjs.Count })); ;
            byte[] sendBytes2 = NetworkMessageEncoderDecoder.Encode(new NetworkMessage(NetworkMessageType.GAMEOVER, new GameOverPayload { winClientID = winClientID }));

            foreach (NetworkClient client in netClients)
            {
                udpClient.BeginSend(sendBytes, sendBytes.Length, (IPEndPoint)endPoint.Create(client.socketAddress), new AsyncCallback(SendCallback), null);
                udpClient.BeginSend(sendBytes2, sendBytes2.Length, (IPEndPoint)endPoint.Create(client.socketAddress), new AsyncCallback(SendCallback), null);

            }

            //foreach (NetworkClient client in netClients)
            //{
            //}
        }
        catch(Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    public int addNetworkObject(NetworkObject netObj)
    {
        if (netObjs.Contains(netObj))
        {
            return netObjs[netObjs.IndexOf(netObj)].objectID;
        }
        else
        {
            int newId = currentObjectID;
            netObjs.Add(netObj);
            currentObjectID++;
            return newId;
        }
    }
}
