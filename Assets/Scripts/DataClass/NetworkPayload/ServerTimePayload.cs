using MessagePack;
using System.Collections;
using System.Collections.Generic;


[MessagePackObject]
public class ServerTimePayload : INetworkPayload
{
    [Key(0)]
    public float clientTime { get; set; }
    [Key(1)]
    public float serverTime { get; set; }
}
