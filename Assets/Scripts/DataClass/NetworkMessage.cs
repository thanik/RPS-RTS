using MessagePack;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum NetworkMessageType
{
    JOIN, // client send connect msg to server with nickname
    WELCOME, // server return connect msg to client
    ERROR, // server send error to client
    LOBBYDATA, // server send lobby data
    READY, // client send ready status in lobby
    UNREADY, // client send unready status in lobby
    DISCONNECT, // client send disconnect to server
    SHUTDOWN, // server send shutting down to client
    CLIENTTIME, // client send its network game time to server
    SERVERTIME, // server send time back
    SYNCTIME, // server send its network game time to client
    UPDATE, // updating delta game state to clients
    FULLUPDATE, // server send full game state to client
    UNITSACTIONS, // client send unit actions to server
    GAMEOVER
}

[MessagePackObject]
public class NetworkMessage
{
    [Key(0)]
    public NetworkMessageType msgType { get; set; }
    [Key(1)]
    public INetworkPayload payload { get; set; }

    public NetworkMessage(NetworkMessageType msgType, INetworkPayload payload)
    {
        this.msgType = msgType;
        this.payload = payload;
    }

    public override string ToString()
    {
        return "NetworkMessage object: " + msgType.ToString() + ":" + (payload != null ? payload.ToString() : "NULL");
    }
}
