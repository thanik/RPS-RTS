using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;

public class StatusBarManager : MonoBehaviour
{
    public GameObject statusBarPrefab;
    public bool currentlySelected = false;
    private Canvas canvas;
    private RectTransform canvasRect;
    private NetworkObject netObj;
    public GameObject statusBar;
    private int maxHealth;
    private float maxCooldown;
    private bool hasLineRenderer = false;
    private LineRenderer lineRenderer;
    void Start()
    {
        canvas = FindObjectOfType<Canvas>();
        canvasRect = canvas.GetComponent<RectTransform>();
        statusBar = Instantiate(statusBarPrefab, canvas.transform);
        netObj = GetComponent<NetworkObject>();
        hasLineRenderer = TryGetComponent<LineRenderer>(out lineRenderer);
        GameManagement.Instance.maxHealth.TryGetValue(netObj.objectType, out maxHealth);
        //if (netObj.objectType == NetworkObjectType.BUILDING)
        //{
        //    maxCooldown = GameManagement.Instance.buildingActionCooldown[netObj.objectLevel];
        //}
        //else
        //{
        //    maxCooldown = GameManagement.Instance.unitActionCooldown[netObj.objectLevel];
        //}
        OnClick();
    }

    // Update is called once per frame
    void Update()
    {
        if (statusBar)
        {
            float offsetPosY;
            if (netObj.currentAction == NetworkObjectAction.TRAINING)
            {
                maxCooldown = GameManagement.Instance.buildingActionCooldown[netObj.objectLevel];
            }
            else if(netObj.currentAction == NetworkObjectAction.UPGRADING)
            {
                maxCooldown = GameManagement.Instance.buildingUpgradingCooldown;
            }
            else if (netObj.currentAction == NetworkObjectAction.WALKTHENATTACK || netObj.currentAction == NetworkObjectAction.ATTACK || netObj.currentAction == NetworkObjectAction.GUARD)
            {
                maxCooldown = GameManagement.Instance.unitActionCooldown[netObj.objectLevel];
            }
            float cooldownTime = (netObj.cooldownTime == 0f) ? 0f : maxCooldown - (netObj.cooldownTime - NetworkClientManager.Instance.networkGameTime);
            statusBar.GetComponent<StatusBar>().updateBar(netObj.health, maxHealth, cooldownTime, maxCooldown);
            // Offset position above object bbox (in world space)
            if (netObj.objectType == NetworkObjectType.BUILDING)
            {
                offsetPosY = transform.position.y + 0.6f;
            }
            else
            {
                offsetPosY = transform.position.y + 0.45f;
            }

            // Final position of marker above GO in world space
            Vector3 offsetPos = new Vector3(transform.position.x, offsetPosY, transform.position.z);

            // Calculate *screen* position (note, not a canvas/recttransform position)
            Vector2 canvasPos;
            Vector2 screenPoint = Camera.main.WorldToScreenPoint(offsetPos);

            // Convert screen position to Canvas / RectTransform space <- leave camera null if Screen Space Overlay
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out canvasPos);

            // Set
            statusBar.transform.localPosition = canvasPos;
        }

        if (hasLineRenderer)
        {
            if (currentlySelected)
            {

                Vector3[] corners = new Vector3[2] { transform.position, GetComponent<NetworkObject>().positionTarget };
                lineRenderer.SetPositions(corners);
                lineRenderer.positionCount = 2;

            }
            else
            {
                lineRenderer.positionCount = 0;
            }
        }
    }

    public void OnClick()
    {
        if (currentlySelected)
        {
            statusBar.GetComponent<StatusBar>().setStatusBarMode(StatusBarMode.WITHBG);
        }
        else
        {
            statusBar.GetComponent<StatusBar>().setStatusBarMode(StatusBarMode.HIDDEN);
        }
    }

    public void OnHover()
    {
        if (!currentlySelected)
        {
            statusBar.GetComponent<StatusBar>().setStatusBarMode(StatusBarMode.NOBG);
        }
    }
}
