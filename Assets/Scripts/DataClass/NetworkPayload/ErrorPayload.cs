using MessagePack;
using System.Collections;
using System.Collections.Generic;


[MessagePackObject]
public class ErrorPayload : INetworkPayload
{
    [Key(0)]
    public string errorString { get; set; }
}