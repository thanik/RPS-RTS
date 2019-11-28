using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MessagePack;

[MessagePackObject]
public class UnitsActionsPayload : INetworkPayload
{
    [Key(0)]
    public List<NetworkActionSnapshot> actions { get; set; }
    [Key(1)]
    public bool addRockTrainingQueue { get; set; }
    [Key(2)]
    public bool addPaperTrainingQueue { get; set; }
    [Key(3)]
    public bool addScissorsTrainingQueue { get; set; }
}
