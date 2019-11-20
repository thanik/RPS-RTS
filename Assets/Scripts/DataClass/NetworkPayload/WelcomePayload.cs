using MessagePack;
using System.Collections;
using System.Collections.Generic;

[MessagePackObject]
public class WelcomePayload : INetworkPayload
{
    [Key(0)]
    public int clientID { get; set; }
}
