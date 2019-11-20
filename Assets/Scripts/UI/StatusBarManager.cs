using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StatusBarManager : MonoBehaviour
{
    public GameObject statusBarPrefab;
    public bool currentlySelected = false;
    private Canvas canvas;
    private RectTransform canvasRect;
    private NetworkObject netObj;
    private GameObject statusBar;
    private int maxHealth;
    private float maxCooldown;
    void Start()
    {
        canvas = FindObjectOfType<Canvas>();
        canvasRect = canvas.GetComponent<RectTransform>();
        statusBar = Instantiate(statusBarPrefab, canvas.transform);
        netObj = GetComponent<NetworkObject>();
        GameManagement.Instance.maxHealth.TryGetValue(netObj.objectType, out maxHealth);
        if (netObj.objectType == NetworkObjectType.BUILDING)
        {
            GameManagement.Instance.buildingActionCooldown.TryGetValue(netObj.objectLevel, out maxCooldown);
        }
        else
        {
            GameManagement.Instance.unitActionCooldown.TryGetValue(netObj.objectLevel, out maxCooldown);
        }
        OnClick();
    }

    // Update is called once per frame
    void Update()
    {
        if (statusBar)
        {

            statusBar.GetComponent<StatusBar>().updateBar(netObj.health, maxHealth, netObj.cooldownTime, maxCooldown);
            // Offset position above object bbox (in world space)
            float offsetPosY = transform.position.y + 0.5f;

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
