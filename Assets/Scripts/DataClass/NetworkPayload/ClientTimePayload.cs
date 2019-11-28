using MessagePack;
using System.Collections;
using System.Collections.Generic;


[MessagePackObject]
public class ClientTimePayload : INetworkPayload
{
    [Key(0)]
    public float clientTime { get; set; }

    [Key(1)]
    public float latency { get; set; }
}
