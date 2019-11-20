using MessagePack;
using System.Collections;
using System.Collections.Generic;


[MessagePackObject]
public class SyncTimePayload : INetworkPayload
{
    [Key(0)]
    public float serverTime { get; set; }
}