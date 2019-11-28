using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class trainButton : MonoBehaviour
{
    public NetworkUnitType unitType;
    private Button button;

    void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(onClick);
    }

    void Update()
    {
        if (GameManagement.Instance.playerData != null && GameManagement.Instance.playerData.TryGetValue(NetworkClientManager.Instance.myClientID, out PlayerData myPlayerData))
        {
            int futureNumberOfUnit = myPlayerData.numberOfUnits + myPlayerData.paperTrainingQueue + myPlayerData.rockTrainingQueue + myPlayerData.scissorsTrainingQueue;
            if (unitType == NetworkUnitType.PAPER)
            {
                GetComponentInChildren<TMP_Text>().text = myPlayerData.paperTrainingQueue.ToString();
                foreach (NetworkObject netObj in NetworkClientManager.Instance.netObjs)
                {
                    if (netObj.objectID == myPlayerData.paperBuildingObjectID)
                    {  
                        button.interactable = (netObj.currentAction != NetworkObjectAction.UPGRADING && netObj.health > 0 && futureNumberOfUnit < GameManagement.Instance.maxUnitsPerPlayer);
                        break;
                    }
                    
                }
            }
            else if (unitType == NetworkUnitType.ROCK)
            {
                GetComponentInChildren<TMP_Text>().text = myPlayerData.rockTrainingQueue.ToString();
                foreach (NetworkObject netObj in NetworkClientManager.Instance.netObjs)
                {
                    if (netObj.objectID == myPlayerData.rockBuildingObjectID)
                    {
                        button.interactable = (netObj.currentAction != NetworkObjectAction.UPGRADING && netObj.health > 0 && futureNumberOfUnit < GameManagement.Instance.maxUnitsPerPlayer);
                        break;
                    }
                    
                }
            }
            else if (unitType == NetworkUnitType.SCISSORS)
            {
                GetComponentInChildren<TMP_Text>().text = myPlayerData.scissorsTrainingQueue.ToString();
                foreach (NetworkObject netObj in NetworkClientManager.Instance.netObjs)
                {
                    
                    if (netObj.objectID == myPlayerData.scissorsBuildingObjectID)
                    {
                        
                        button.interactable = (netObj.currentAction != NetworkObjectAction.UPGRADING && netObj.health > 0 && futureNumberOfUnit < GameManagement.Instance.maxUnitsPerPlayer);
                        break;
                    }
                   
                }
            }
        }
    }

    void onClick()
    {
        GameManagement.Instance.playerData.TryGetValue(NetworkClientManager.Instance.myClientID, out PlayerData myPlayerData);
        List<NetworkActionSnapshot> snaps = new List<NetworkActionSnapshot>();
        if (unitType == NetworkUnitType.PAPER)
        {
            snaps.Add(new NetworkActionSnapshot() { objectID = myPlayerData.paperBuildingObjectID, action = NetworkObjectAction.TRAINING });
        }
        else if (unitType == NetworkUnitType.ROCK)
        {
            snaps.Add(new NetworkActionSnapshot() { objectID = myPlayerData.rockBuildingObjectID, action = NetworkObjectAction.TRAINING });
        }
        else if (unitType == NetworkUnitType.SCISSORS)
        {
            snaps.Add(new NetworkActionSnapshot() { objectID = myPlayerData.scissorsBuildingObjectID, action = NetworkObjectAction.TRAINING });
        }

        NetworkClientManager.Instance.sendUnitsActions(snaps, unitType);
    }
}
