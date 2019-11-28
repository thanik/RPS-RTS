using MessagePack;

[MessagePackObject]
public class PlayerData
{
    [Key(0)]
    public int clientID { get; set; }
    [Key(1)]
    public int overallHealth { get; set; }
    [Key(2)]
    public int numberOfUnits { get; set; }
    [Key(3)]
    public int rockBuildingObjectID { get; set; }
    [Key(4)]
    public int paperBuildingObjectID { get; set; }
    [Key(5)]
    public int scissorsBuildingObjectID { get; set; }
    [Key(6)]
    public int rockTrainingQueue { get; set; } = 0;
    [Key(7)]
    public int paperTrainingQueue { get; set; } = 0;
    [Key(8)]
    public int scissorsTrainingQueue { get; set; } = 0;
}