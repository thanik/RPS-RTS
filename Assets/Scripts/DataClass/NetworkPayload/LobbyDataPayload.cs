using System.Collections;
using System.Collections.Generic;
using MessagePack;

[MessagePackObject]
public class LobbyDataPayload : INetworkPayload
{
    [Key(0)]
    public List<NetworkClient> netClients { get; set; }
    [Key(1)]
    public float gameStartTime { get; set; }

}
