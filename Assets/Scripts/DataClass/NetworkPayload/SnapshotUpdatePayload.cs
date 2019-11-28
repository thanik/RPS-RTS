using System.Collections;
using System.Collections.Generic;
using MessagePack;

[MessagePackObject]
public class SnapshotUpdatePayload : INetworkPayload
{
    [Key(0)]
    public float gameTime { get; set; }
    [Key(1)]
    public List<NetworkObjectSnapshot> netObjects { get; set; }
    [Key(2)]
    public Dictionary<int, PlayerData> playerData { get; set; }
    [Key(3)]
    public int maxObjectID { get; set; }
    [Key(4)]
    public int totalObjectCount { get; set; }
}
