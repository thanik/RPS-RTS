using UnityEngine;
using System.Collections;
using MessagePack;

[MessagePackObject]
public class GameOverPayload : INetworkPayload
{
    [Key(0)]
    public int winClientID { get; set; }
}
