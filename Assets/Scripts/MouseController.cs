using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MouseController : MonoBehaviour
{
    public float scrollSpeed = 25;
    public int scrollWidth = 15;
    public float maxBaseMinZoomPositionX = 12.5f;
    public float maxBaseMinZoomPositionY = 9f;

    public float maxBaseMaxZoomPositionX = 3.4f;
    public float maxBaseMaxZoomPositionY = 4.15f;

    public float minSizeZoom = 5f;
    public float maxSizeZoom = 10f;

    public List<GameObject> selectedObjects = new List<GameObject>();
    public List<GameObject> unitObjects = new List<GameObject>();

    public RectTransform selectSquareImage;
    private Vector3 startPos;
    private Vector3 endPos;
    private Vector3 startViewportPos;
    private Vector3 endViewportPos;
    private LayerMask unitMask;
    private LayerMask groundMask;
    private Ray ray;
    private GameObject tempGameObject;
    private float minPositionX = -12f;
    private float maxPositionX = 12f;
    private float minPositionY = -9f;
    private float maxPositionY = 9f;
    public Vector3 startCameraPosition;
    void Start()
    {
        selectSquareImage.gameObject.SetActive(false);
        unitMask = LayerMask.GetMask("Unit");
        groundMask = LayerMask.GetMask("Ground");

        // change camera position to player position
        if (GameManagement.Instance.gameMode == GameMode.CLIENT)
        {
            switch (NetworkClientManager.Instance.myClientID)
            {
                case 0:
                    startCameraPosition = new Vector3(minPositionX, maxPositionY, -10f);
                    break;
                case 1:
                    startCameraPosition = new Vector3(minPositionX, minPositionY, -10f);
                    break;
                case 2:
                    startCameraPosition = new Vector3(maxPositionX, maxPositionY, -10f);
                    break;
                case 3:
                    startCameraPosition = new Vector3(maxPositionX, minPositionY, -10f);
                    break;

            }
            Camera.main.transform.position = startCameraPosition;
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        // gather all my units
        unitObjects.Clear();
        foreach (NetworkObject netObj in NetworkClientManager.Instance.netObjs)
        {
            if (NetworkClientManager.Instance.myClientID == netObj.clientOwnerID && netObj.objectType == NetworkObjectType.UNIT)
            {
                unitObjects.Add(netObj.gameObject);
            }
        }

        ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit2D rayHit = Physics2D.Raycast(ray.origin, ray.direction, Mathf.Infinity, unitMask);
        hover(rayHit);
        select(rayHit);
        doAction(rayHit);
        selectionBox();
        if (!Input.GetMouseButton(0))
            MoveCamera();
    }

    private void hover(RaycastHit2D rayHit)
    {
        if (rayHit.collider)
        {
            if(tempGameObject && !tempGameObject.Equals(rayHit.collider.gameObject))
            {
                tempGameObject.GetComponent<StatusBarManager>().OnClick();
            }
            StatusBarManager statBarMan = rayHit.collider.GetComponent<StatusBarManager>();
            statBarMan.OnHover();
            tempGameObject = rayHit.collider.gameObject;
        }
        else
        {
            if (tempGameObject)
                tempGameObject.GetComponent<StatusBarManager>().OnClick();
        }
    }

    private void select(RaycastHit2D rayHit)
    {
        if (Input.GetMouseButtonDown(0))
        {
            startViewportPos = Camera.main.ScreenToViewportPoint(Input.mousePosition);
            if (rayHit.collider)
            {
                StatusBarManager statBarMan = rayHit.collider.GetComponent<StatusBarManager>();

                if (Input.GetKey(KeyCode.LeftControl))
                {
                    if (statBarMan.currentlySelected)
                    {
                        selectedObjects.Remove(rayHit.collider.gameObject);
                        statBarMan.currentlySelected = false;
                        statBarMan.OnClick();
                    }
                    else
                    {
                        selectedObjects.Add(rayHit.collider.gameObject);
                        statBarMan.currentlySelected = true;
                        statBarMan.OnClick();
                    }
                }
                else
                {
                    ClearSelection();
                    selectedObjects.Add(rayHit.collider.gameObject);
                    statBarMan.currentlySelected = true;
                    statBarMan.OnClick();
                }
            }
        }

        if(Input.GetMouseButtonUp(0))
        {
            endViewportPos = Camera.main.ScreenToViewportPoint(Input.mousePosition);

            if (startPos != endPos)
            {
                SelectObjects();
            }
            else if (!rayHit.collider)
            {
                ClearSelection();
            }
        }
    }

    private void doAction(RaycastHit2D rayHit)
    {
        if (Input.GetMouseButtonDown(1))
        {
            List<NetworkActionSnapshot> actions = new List<NetworkActionSnapshot>();
            Vector3 positionClicked = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            positionClicked.z = 0f;
            //Debug.Log("Position clicked: " + positionClicked);
            if (rayHit.collider)
            {
                //Debug.Log("Action do on unit");
                //Debug.Log(rayHit.collider.gameObject.GetComponent<NetworkObject>().objectID);
                foreach (GameObject selectedObj in selectedObjects)
                {
                    if (selectedObj)
                    {
                        NetworkObject selectedNetworkObj = selectedObj.GetComponent<NetworkObject>();
                        if (selectedNetworkObj.objectType == NetworkObjectType.UNIT)
                        {
                            //selectedObj.GetComponent<NavMeshAgent>().SetDestination(positionClicked);
                            //selectedNetworkObj.positionTarget = positionClicked;
                            //selectedNetworkObj.objectIDTarget = rayHit.collider.gameObject.GetComponent<NetworkObject>().objectID;


                            NetworkActionSnapshot newActionSnapshot = new NetworkActionSnapshot()
                            {
                                objectID = selectedNetworkObj.objectID,
                                objectIDTarget = rayHit.collider.gameObject.GetComponent<NetworkObject>().objectID,
                                positionTarget = positionClicked
                            };
                            if (rayHit.collider.gameObject.GetComponent<NetworkObject>().clientOwnerID != selectedNetworkObj.clientOwnerID)
                            {
                                //selectedNetworkObj.currentAction = NetworkObjectAction.WALKTHENATTACK;
                                newActionSnapshot.action = NetworkObjectAction.WALKTHENATTACK;
                            }
                            else
                            {
                                //selectedNetworkObj.currentAction = NetworkObjectAction.WALKING;
                                newActionSnapshot.action = NetworkObjectAction.WALKING;
                            }

                            actions.Add(newActionSnapshot);


                        }
                    }
                }

                
            }
            else
            {
                //Debug.Log("Action do on ground");
                foreach(GameObject selectedObj in selectedObjects)
                {
                    if (selectedObj)
                    {
                        NetworkObject selectedNetworkObj = selectedObj.GetComponent<NetworkObject>();
                        if (selectedNetworkObj.objectType == NetworkObjectType.UNIT)
                        {
                            //selectedNetworkObj.currentAction = NetworkObjectAction.WALKING;
                            //selectedNetworkObj.positionTarget = positionClicked;
                            //selectedObj.GetComponent<NavMeshAgent>().SetDestination(positionClicked);

                            NetworkActionSnapshot newActionSnapshot = new NetworkActionSnapshot()
                            {
                                objectID = selectedNetworkObj.objectID,
                                objectIDTarget = 0,
                                positionTarget = positionClicked,
                                action = NetworkObjectAction.WALKING
                            };

                            actions.Add(newActionSnapshot);
                        }
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

    private void SelectObjects()
    {
        List<GameObject> remObjects = new List<GameObject>();
        if (!Input.GetKey(KeyCode.LeftControl))
        {
            ClearSelection();
        }

        Rect selectRect = new Rect(startViewportPos.x, startViewportPos.y, endViewportPos.x - startViewportPos.x, endViewportPos.y - startViewportPos.y);
        foreach(GameObject selectObject in unitObjects)
        {
            if (selectObject)
            {
                
                if (selectRect.Contains(Camera.main.WorldToViewportPoint(selectObject.transform.position), true))
                {
                    if (!selectedObjects.Contains(selectObject))
                    {
                        selectedObjects.Add(selectObject);
                    }
                    selectObject.GetComponent<StatusBarManager>().currentlySelected = true;
                    selectObject.GetComponent<StatusBarManager>().OnClick();
                }
            }
            else
            {
                remObjects.Add(selectObject);
            }
        }

        if(remObjects.Count > 0)
        {
            foreach(GameObject rem in remObjects)
            {
                unitObjects.Remove(rem);
            }

            remObjects.Clear();
        }
    }

    public void ClearSelection()
    {
        if (selectedObjects.Count > 0)
        {
            foreach(GameObject obj in selectedObjects)
            {
                if (obj)
                {
                    obj.GetComponent<StatusBarManager>().currentlySelected = false;
                    obj.GetComponent<StatusBarManager>().OnClick();
                }
            }
            selectedObjects.Clear();
        }
    }

    private void selectionBox()
    {
        if (Input.GetMouseButtonDown(0))
        {
            //startPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            startPos = Input.mousePosition;
        }

        if(Input.GetMouseButtonUp(0))
        {
            selectSquareImage.gameObject.SetActive(false);
        }

        if(Input.GetMouseButton(0))
        {
            if (!selectSquareImage.gameObject.activeInHierarchy)
            {
                selectSquareImage.gameObject.SetActive(true);
            }

            endPos = Input.mousePosition;
            Vector3 center = (startPos + endPos) / 2f;
            selectSquareImage.position = center;

            float sizeX = Mathf.Abs(startPos.x - endPos.x);
            float sizeY = Mathf.Abs(startPos.y - endPos.y);

            selectSquareImage.sizeDelta = new Vector2(sizeX, sizeY);
            
        }
    }

    private void MoveCamera()
    {
        float xpos = Input.mousePosition.x;
        float ypos = Input.mousePosition.y;
        Vector3 movement = new Vector3(0, 0, 0);

        //horizontal camera movement
        if (xpos >= 0 && xpos < scrollWidth)
        {
            movement.x -= scrollSpeed;
        }
        else if (xpos <= Screen.width && xpos > Screen.width - scrollWidth)
        {
            movement.x += scrollSpeed;
        }

        //vertical camera movement
        if (ypos >= 0 && ypos < scrollWidth)
        {
            movement.y -= scrollSpeed;
        }
        else if (ypos <= Screen.height && ypos > Screen.height - scrollWidth)
        {
            movement.y += scrollSpeed;
        }

        //make sure movement is in the direction the camera is pointing
        //but ignore the vertical tilt of the camera to get sensible scrolling
        movement = Camera.main.transform.TransformDirection(movement);

        //calculate desired camera position based on received input
        Vector3 origin = Camera.main.transform.position;
        Vector3 destination = origin;
        destination.x += movement.x;
        destination.y += movement.y;

        // zoom
        if (Input.GetAxis("Mouse ScrollWheel") < 0)
        {
            Camera.main.orthographicSize += 1f;
            if (Camera.main.orthographicSize > maxSizeZoom)
            {
                Camera.main.orthographicSize = maxSizeZoom;
            }
            calculateMaxXY();
        }

        if (Input.GetAxis("Mouse ScrollWheel") > 0)
        {
            Camera.main.orthographicSize += -1f;
            if (Camera.main.orthographicSize < minSizeZoom)
            {
                Camera.main.orthographicSize = minSizeZoom;
            }
            calculateMaxXY();
        }

        if (destination.x < minPositionX)
        {
            destination.x = minPositionX;
        }
        if (destination.x > maxPositionX)
        {
            destination.x = maxPositionX;
        }

        if (destination.y < minPositionY)
        {
            destination.y = minPositionY;
        }
        if (destination.y > maxPositionY)
        {
            destination.y = maxPositionY;
        }

        //if a change in position is detected perform the necessary update
        if (destination != origin)
        {
            Camera.main.transform.position = Vector3.MoveTowards(origin, destination, Time.deltaTime * scrollSpeed);
        }
    }
    private void calculateMaxXY()
    {
        float zoomRatio = (Camera.main.orthographicSize - minSizeZoom) / (maxSizeZoom - minSizeZoom);
        minPositionX = -(maxBaseMinZoomPositionX - (maxBaseMinZoomPositionX - maxBaseMaxZoomPositionX) * zoomRatio);
        maxPositionX = (maxBaseMinZoomPositionX - (maxBaseMinZoomPositionX - maxBaseMaxZoomPositionX) * zoomRatio);
        minPositionY = -(maxBaseMinZoomPositionY - (maxBaseMinZoomPositionY - maxBaseMaxZoomPositionY) * zoomRatio);
        maxPositionY = (maxBaseMinZoomPositionY - (maxBaseMinZoomPositionY - maxBaseMaxZoomPositionY) * zoomRatio);
    }
}
