using MessagePack;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

[MessagePackObject]
public class NetworkClient
{
    [Key(0)]
    public int clientID;
    [IgnoreMember]
    public SocketAddress socketAddress;
    [Key(1)]
    public bool isReady = false;
    [IgnoreMember]
    public int latency;
    [Key(2)]
    public string nickname;

    public NetworkClient(int clientID, SocketAddress socketAddress, string nickname)
    {
        this.clientID = clientID;
        this.socketAddress = socketAddress;
        this.nickname = nickname;
    }

    public NetworkClient(int clientID, bool isReady, string nickname)
    {
        this.clientID = clientID;
        this.isReady = isReady;
        this.nickname = nickname;
    }
}
