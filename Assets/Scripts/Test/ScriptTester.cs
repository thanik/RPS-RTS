using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ScriptTester : MonoBehaviour
{
    // Start is called before the first frame update
    List<NetworkObjectSnapshot> diffSnapObjs = new List<NetworkObjectSnapshot>();
    List<NetworkObjectSnapshot> diffSnapObjs2 = new List<NetworkObjectSnapshot>();
    public List<NetworkObjectSnapshot> diff;
    void Start()
    {
        diffSnapObjs.Add(new NetworkObjectSnapshot(SnapshotAction.UPDATE, 0, 0, NetworkObjectType.UNIT, NetworkUnitType.PAPER, NetworkObjectAction.NOTHING, new Vector3(1f, 1f), 1, 100, 1, 20f, new Vector3(0.5f, 0.5f)));
        diffSnapObjs.Add(new NetworkObjectSnapshot(SnapshotAction.UPDATE, 1, 0, NetworkObjectType.UNIT, NetworkUnitType.ROCK, NetworkObjectAction.TRAINING, new Vector3(2f, 2f), 2, 100, 3, 20f, new Vector3(0.5f, 0.5f)));

        diffSnapObjs2.Add(new NetworkObjectSnapshot(SnapshotAction.UPDATE, 0, 0, NetworkObjectType.UNIT, NetworkUnitType.PAPER, NetworkObjectAction.NOTHING, new Vector3(1f, 1f), 1, 100, 1, 20f, new Vector3(0.5f, 0.5f)));
        diffSnapObjs2.Add(new NetworkObjectSnapshot(SnapshotAction.UPDATE, 1, 0, NetworkObjectType.UNIT, NetworkUnitType.ROCK, NetworkObjectAction.NOTHING, new Vector3(2f, 2f), 2, 100, 3, 20f, new Vector3(0.5f, 0.5f)));
        //diffSnapObjs2.Add(new NetworkObjectSnapshot(SnapshotAction.DESTROY, 2, 0, NetworkObjectType.UNIT, NetworkUnitType.ROCK, NetworkObjectAction.NOTHING, new Vector3(1f, 1f), 0, 100, 3, 20f, new Vector3(0.5f, 0.5f)));


        diff = diffSnapObjs2.Except(diffSnapObjs, new NetworkObjectSnapshotComparer()).ToList();
        string diffArrayLog = "";
        foreach(NetworkObjectSnapshot n in diff)
        {
            diffArrayLog += n.snapshotAction.ToString() + "\n" + n.objectID + "\n" + n.clientOwnerID + "\n" + n.objectType.ToString() + "\n" + n.unitType.ToString() + "\n" + n.currentAction.ToString() + "\n" + n.positionTarget.ToString() + "\n" + n.objectIDTarget + "\n" + n.health + "\n" + n.objectLevel + "\n" + n.cooldownTime + "\n" + n.currentPosition;
            Debug.Log(diffArrayLog);
            diffArrayLog = "";
        }
       
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
