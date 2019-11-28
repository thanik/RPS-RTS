using UnityEngine;
using System.Collections;
using MessagePack;

[MessagePackObject]
public class NetworkActionSnapshot
{
    [Key(0)]
    public int objectID { get; set; }
    [Key(1)]
    public NetworkObjectAction action { get; set; }
    [Key(2)]
    public Vector3 positionTarget { get; set; }
    [Key(3)]
    public int objectIDTarget { get; set; }
}
