using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManager : Singleton<NetworkManager>
{
    public List<NetworkObject> netObjs = new List<NetworkObject>();
    public Dictionary<IPEndPoint, int> netClients = new Dictionary<IPEndPoint, int>();
    public Text debugText;
    public Toggle predictionEnabled;

    private bool isConnected = false;
    private bool isServer = false;
    public int myClientID;

    private UdpClient udpClient;
    IPEndPoint endPoint;
    private List<Vector3> positionQueue = new List<Vector3>();
    private List<float> gameTimeQueue = new List<float>();

    public float networkGameTime = 0;
    private float timeDelta, latency, roundTrip;

    Vector3 velocity = new Vector3(0f, 0f);
    float diffTime = 0f;


    void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (isServer)
        {
            //udpClient.BeginReceive(new AsyncCallback(ReceiveServerCallback), null);
            byte[] sendBytes = Encoding.ASCII.GetBytes("GAME:TIME=" + ((long)(networkGameTime * 1000)) + ":OBJECT_ID_0_POSITION=" + netObjs[0].transform.position.x + "," + netObjs[0].transform.position.y);
            foreach (KeyValuePair<IPEndPoint, int> kvp in netClients)
            {
                udpClient.BeginSend(sendBytes, sendBytes.Length, kvp.Key, new AsyncCallback(SendCallback), null);
            }

        }
        else if (isConnected)
        {

            //udpClient.BeginReceive(new AsyncCallback(ReceiveClientCallback), null);
        }

    }

    void Update()
    {
        if (debugText)
        {
            float latestGameTime = 0f;
            if (gameTimeQueue.Count != 0)
            {
                latestGameTime = gameTimeQueue[0];
            }
            debugText.text = "Debug\n";
            debugText.text += "Network Game Time: " + networkGameTime + "\n";
            debugText.text += "Latest packet game time: " + latestGameTime.ToString() + "\n";
            debugText.text += "Latency: " + latency.ToString() + "ms\n";
            debugText.text += "Time Delta: " + timeDelta.ToString() + "ms\n";
            debugText.text += "Prediction\n";
            debugText.text += "Velocity: " + velocity.ToString("0.0000") + "\n";
            debugText.text += "diffTime: " + diffTime.ToString() + "\n";
            debugText.text += "Current position: " + netObjs[0].transform.position.ToString() + "\n";
        }

        // gameobject update position to parsed data from network
        if (isConnected)
        {
            //netObjs[0].transform.position = tempPosition;

            // got 2 position, now predict next position

            if (positionQueue.Count > 1 && predictionEnabled.isOn)
            {
                diffTime = (gameTimeQueue[1] - gameTimeQueue[0]);

                if (diffTime != 0)
                {
                    velocity = (positionQueue[1] - positionQueue[0]) / diffTime;
                    netObjs[0].transform.position = positionQueue[0] + (velocity * (networkGameTime - gameTimeQueue[0]));
                }
                else
                {
                    netObjs[0].transform.position = positionQueue[0];
                }
            }
            else if (positionQueue.Count > 0)
            {
                netObjs[0].transform.position = positionQueue[0];
            }
            else
            {
                netObjs[0].transform.position = new Vector3(0f, 0f);
            }
        }

        if (isServer || isConnected)
        {
            networkGameTime += Time.deltaTime;
        }
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

    public void setupServer(int portNumber)
    {
        endPoint = new IPEndPoint(IPAddress.Any, portNumber);
        udpClient = new UdpClient(endPoint);
        isServer = true;
        //netClients.Add(new IPEndPoint(IPAddress.Loopback, portNumber), 0);
        myClientID = 0;
        udpClient.BeginReceive(new AsyncCallback(ReceiveServerCallback), null);
        StartCoroutine(SendServerTimestamp());
    }

    public void sendGameOverResult()
    {

    }

    public void ReceiveServerCallback(IAsyncResult ar)
    {
        try
        {
            byte[] receiveBytes = udpClient.EndReceive(ar, ref endPoint);
            string receiveString = Encoding.ASCII.GetString(receiveBytes);
            Debug.Log(receiveString);
            if (receiveString == "JOIN")
            {
                int clientID = netClients.Count + 1;
                byte[] sendBytes = Encoding.ASCII.GetBytes("WELCOME:" + clientID);
                netClients.Add(endPoint, clientID);
                udpClient.BeginSend(sendBytes, sendBytes.Length, endPoint, new AsyncCallback(SendCallback), null);
            }
            else if (receiveString == "BYE")
            {
                netClients.Remove(endPoint);
            }
            else if (receiveString.StartsWith("CLIENTTIME:"))
            {
                String[] commandSplit = receiveString.Split(':');
                String time = commandSplit[1];
                byte[] sendBytes = Encoding.ASCII.GetBytes("SERVERTIME:" + (long)(networkGameTime * 1000) + ":" + time);
                udpClient.BeginSend(sendBytes, sendBytes.Length, endPoint, new AsyncCallback(SendCallback), null);
            }
            udpClient.BeginReceive(new AsyncCallback(ReceiveServerCallback), null);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    public void ReceiveClientCallback(IAsyncResult ar)
    {
        try
        {
            byte[] receiveBytes = udpClient.EndReceive(ar, ref endPoint);
            string receiveString = Encoding.ASCII.GetString(receiveBytes);
            //Debug.Log("Got response from server: " + receiveString);
            if (receiveString.StartsWith("WELCOME:"))
            {
                String[] commandSplit = receiveString.Split(':');
                Debug.Log(commandSplit[1]);
                myClientID = int.Parse(commandSplit[1]);
                isConnected = true;
            }
            else if (receiveString.StartsWith("GAME:"))
            {
                String[] commandSplit = receiveString.Split(':');
                String time = commandSplit[1];
                String object0_position = commandSplit[2];
                //Debug.Log("Server time: " + time.Split('=')[1]); 
                String[] realPos = object0_position.Split('=')[1].Split(',');
                float gameTime = (float)long.Parse(time.Split('=')[1]) / 1000;

                foreach (NetworkObject netObj in netObjs)
                {
                    if (netObj.objectID == 0)
                    {
                        //Debug.Log("Object0 Position: " + realPos[0] + "," + realPos[1]);
                        //tempPosition = new Vector3(float.Parse(realPos[0]), float.Parse(realPos[1]));

                        if (positionQueue.Count > 2)
                        {
                            positionQueue.RemoveAt(2);
                        }
                        positionQueue.Insert(0, new Vector3(float.Parse(realPos[0]), float.Parse(realPos[1])));

                        if (gameTimeQueue.Count > 2)
                        {
                            gameTimeQueue.RemoveAt(2);
                        }
                        gameTimeQueue.Insert(0, gameTime);

                    }
                }

            }
            else if (receiveString.StartsWith("SERVERTIME:"))
            {
                String[] commandSplit = receiveString.Split(':');
                String serverTime = commandSplit[1];
                String clientTime = commandSplit[2];
                CalculateTimeDelta(float.Parse(serverTime) / 1000, float.Parse(clientTime) / 1000);
            }
            else if (receiveString.StartsWith("SYNCTIME:"))
            {
                String[] commandSplit = receiveString.Split(':');
                String serverTime = commandSplit[1];
                SyncClock(float.Parse(serverTime) / 1000);
            }

            udpClient.BeginReceive(new AsyncCallback(ReceiveClientCallback), null);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }

    }

    public void connectToServer(string ipAddress, int port)
    {
        udpClient = new UdpClient();
        byte[] sendBytes = Encoding.ASCII.GetBytes("JOIN");
        Debug.Log("Connecting to: " + ipAddress + ":" + port);
        endPoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);
        udpClient.BeginSend(sendBytes, sendBytes.Length, endPoint, new AsyncCallback(SendClientCallback), null);
        isServer = false;
        StartCoroutine(SendTimestamp());
    }

    public void SendCallback(IAsyncResult ar)
    {
        //Debug.Log($"number of bytes sent: {udpClient.EndSend(ar)}");
        udpClient.EndSend(ar);
    }

    public void SendClientCallback(IAsyncResult ar)
    {
        Debug.Log($"number of bytes sent: {udpClient.EndSend(ar)}");
        udpClient.BeginReceive(new AsyncCallback(ReceiveClientCallback), null);
    }

    void OnDestroy()
    {
        if (isConnected)
        {
            byte[] sendBytes = Encoding.ASCII.GetBytes("BYE");
            udpClient.BeginSend(sendBytes, sendBytes.Length, endPoint, new AsyncCallback(SendCallback), null);

        }
    }

    IEnumerator SendTimestamp()
    {
        if (isConnected)
        {
            byte[] sendBytes = Encoding.ASCII.GetBytes("CLIENTTIME:" + (long)(networkGameTime * 1000));
            udpClient.BeginSend(sendBytes, sendBytes.Length, endPoint, new AsyncCallback(SendCallback), null);
        }
        yield return new WaitForSeconds(5f);
        StartCoroutine(SendTimestamp());
    }

    IEnumerator SendServerTimestamp()
    {
        byte[] sendBytes = Encoding.ASCII.GetBytes("SYNCTIME:" + ((long)(networkGameTime * 1000)));
        foreach (KeyValuePair<IPEndPoint, int> kvp in netClients)
        {
            udpClient.BeginSend(sendBytes, sendBytes.Length, kvp.Key, new AsyncCallback(SendCallback), null);
        }
        yield return new WaitForSeconds(5f);
        StartCoroutine(SendServerTimestamp());
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
}
