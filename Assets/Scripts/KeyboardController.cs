using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class KeyboardController : MonoBehaviour
{
    MouseController mouseController;
    // Start is called before the first frame update
    void Start()
    {
        mouseController = GetComponent<MouseController>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 currentPosition = Camera.main.transform.position;
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            currentPosition.x -= 0.2f;
        }
        else if(Input.GetKey(KeyCode.RightArrow))
        {
            currentPosition.x += 0.2f;
        }

        if (Input.GetKey(KeyCode.UpArrow))
        {
            currentPosition.y += 0.2f;
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            currentPosition.y -= 0.2f;
        }
        Camera.main.transform.position = currentPosition;

        if(Input.GetKeyDown(KeyCode.Q))
        {
            mouseController.ClearSelection();
            foreach (GameObject gameObject in mouseController.unitObjects)
            {
                NetworkObject netObj = gameObject.GetComponent<NetworkObject>();
                StatusBarManager statsBar = gameObject.GetComponent<StatusBarManager>();
                if (netObj.unitType == NetworkUnitType.ROCK)
                {
                    if (!mouseController.selectedObjects.Contains(gameObject))
                    {
                        mouseController.selectedObjects.Add(gameObject);
                    }
                    statsBar.currentlySelected = true;
                    statsBar.OnClick();
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            mouseController.ClearSelection();
            foreach (GameObject gameObject in mouseController.unitObjects)
            {
                NetworkObject netObj = gameObject.GetComponent<NetworkObject>();
                StatusBarManager statsBar = gameObject.GetComponent<StatusBarManager>();
                if (netObj.unitType == NetworkUnitType.PAPER)
                {
                    if (!mouseController.selectedObjects.Contains(gameObject))
                    {
                        mouseController.selectedObjects.Add(gameObject);
                    }
                    statsBar.currentlySelected = true;
                    statsBar.OnClick();
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            mouseController.ClearSelection();
            foreach (GameObject gameObject in mouseController.unitObjects)
            {
                NetworkObject netObj = gameObject.GetComponent<NetworkObject>();
                StatusBarManager statsBar = gameObject.GetComponent<StatusBarManager>();
                if (netObj.unitType == NetworkUnitType.SCISSORS)
                {
                    if (!mouseController.selectedObjects.Contains(gameObject))
                    {
                        mouseController.selectedObjects.Add(gameObject);
                    }
                    statsBar.currentlySelected = true;
                    statsBar.OnClick();
                }
            }
        }

        if(Input.GetKeyDown(KeyCode.H))
        {
            Camera.main.transform.position = mouseController.startCameraPosition;
        }

        if(Input.GetKeyDown(KeyCode.G))
        {
            List<NetworkActionSnapshot> actions = new List<NetworkActionSnapshot>();
            foreach (GameObject selectedObj in mouseController.selectedObjects)
            {
                if (selectedObj)
                {
                    NetworkObject selectedNetworkObj = selectedObj.GetComponent<NetworkObject>();
                    if (selectedNetworkObj.objectType == NetworkObjectType.UNIT)
                    {
                        NetworkActionSnapshot newActionSnapshot = new NetworkActionSnapshot()
                        {
                            objectID = selectedNetworkObj.objectID,
                            action = NetworkObjectAction.GUARD,
                            objectIDTarget = 0,
                            positionTarget = selectedObj.transform.position
                        };
                        actions.Add(newActionSnapshot);
                    }
                }
            }

            if (actions.Count > 0)
            {
                NetworkClientManager.Instance.sendUnitsActions(actions, NetworkUnitType.NONE);
                actions.Clear();
            }
        }
    }
}
