using MessagePack;
using System.Collections;
using System.Collections.Generic;


[MessagePackObject]
public class JoinPayload : INetworkPayload
{
    [Key(0)]
    public string nickname { get; set; }
}
