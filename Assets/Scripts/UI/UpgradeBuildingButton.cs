using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeBuildingButton : MonoBehaviour
{
    public NetworkUnitType unitType;
    private Button button;
    private TMP_Text buttonText;
    void Start()
    {
        button = GetComponent<Button>();
        buttonText = GetComponentInChildren<TMP_Text>();
        button.onClick.AddListener(onClick);
    }

    void Update()
    {
        if (GameManagement.Instance.playerData != null && GameManagement.Instance.playerData.TryGetValue(NetworkClientManager.Instance.myClientID, out PlayerData myPlayerData))
        {

            if (unitType == NetworkUnitType.PAPER)
            {
                foreach (NetworkObject netObj in NetworkClientManager.Instance.netObjs)
                {
                    if (netObj.objectID == myPlayerData.paperBuildingObjectID)
                    {
                        if (netObj.health <= 0)
                        {
                            button.interactable = false;
                        }
                        else
                        {
                            if (netObj.currentAction == NetworkObjectAction.NOTHING)
                            {
                                if (netObj.objectLevel < 4)
                                {
                                    button.interactable = true;
                                    buttonText.text = "U\nP";
                                }
                                else
                                {
                                    button.interactable = false;
                                    buttonText.text = "M\nA\nX";
                                }
                            }
                            else if (netObj.currentAction == NetworkObjectAction.TRAINING)
                            {
                                button.interactable = false;
                                buttonText.text = "T..";
                            }
                            else if (netObj.currentAction == NetworkObjectAction.UPGRADING)
                            {
                                button.interactable = false;
                                buttonText.text = "U..";
                            }
                        }
                        break;
                    }
                    
                }
            }
            else if (unitType == NetworkUnitType.ROCK)
            {
                foreach (NetworkObject netObj in NetworkClientManager.Instance.netObjs)
                {
                    if (netObj.objectID == myPlayerData.rockBuildingObjectID)
                    {
                        if (netObj.health <= 0)
                        {
                            button.interactable = false;
                        }
                        else
                        {
                            if (netObj.currentAction == NetworkObjectAction.NOTHING)
                            {
                                if (netObj.objectLevel < 4)
                                {
                                    button.interactable = true;
                                    buttonText.text = "U\nP";
                                }
                                else
                                {
                                    button.interactable = false;
                                    buttonText.text = "M\nA\nX";
                                }
                            }
                            else if (netObj.currentAction == NetworkObjectAction.TRAINING)
                            {
                                button.interactable = false;
                                buttonText.text = "T..";
                            }
                            else if (netObj.currentAction == NetworkObjectAction.UPGRADING)
                            {
                                button.interactable = false;
                                buttonText.text = "U..";
                            }
                        }
                        break;
                    }
                    
                }
            }
            else if (unitType == NetworkUnitType.SCISSORS)
            {
                foreach (NetworkObject netObj in NetworkClientManager.Instance.netObjs)
                {
                    if (netObj.objectID == myPlayerData.scissorsBuildingObjectID)
                    {
                        if (netObj.health <= 0)
                        {
                            button.interactable = false;
                        }
                        else
                        {
                            if (netObj.currentAction == NetworkObjectAction.NOTHING)
                            {
                                if (netObj.objectLevel < 4)
                                {
                                    button.interactable = true;
                                    buttonText.text = "U\nP";
                                }
                                else
                                {
                                    button.interactable = false;
                                    buttonText.text = "M\nA\nX";
                                }
                            }
                            else if (netObj.currentAction == NetworkObjectAction.TRAINING)
                            {
                                button.interactable = false;
                                buttonText.text = "T..";
                            }
                            else if (netObj.currentAction == NetworkObjectAction.UPGRADING)
                            {
                                button.interactable = false;
                                buttonText.text = "U..";
                            }
                        }
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
            snaps.Add(new NetworkActionSnapshot() { objectID = myPlayerData.paperBuildingObjectID, action = NetworkObjectAction.UPGRADING });
        }
        else if (unitType == NetworkUnitType.ROCK)
        {
            snaps.Add(new NetworkActionSnapshot() { objectID = myPlayerData.rockBuildingObjectID, action = NetworkObjectAction.UPGRADING });
        }
        else if (unitType == NetworkUnitType.SCISSORS)
        {
            snaps.Add(new NetworkActionSnapshot() { objectID = myPlayerData.scissorsBuildingObjectID, action = NetworkObjectAction.UPGRADING });
        }
       
        NetworkClientManager.Instance.sendUnitsActions(snaps, NetworkUnitType.NONE);

    }
}
