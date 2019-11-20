using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerInListUI : MonoBehaviour
{
    public TMP_Text clientNoText;
    public Image colorImg;
    public TMP_Text playerNameText;
    public TMP_Text pingText;
    public TMP_Text readyText;
    
    public void updateUI(NetworkClient client)
    {
        clientNoText.text = client.clientID.ToString();
        playerNameText.text = client.nickname;
        if (client.clientID == NetworkClientManager.Instance.myClientID)
        {
            pingText.text = (NetworkClientManager.Instance.latency * 1000).ToString("0") + "ms";
        }
        else
        {
            pingText.text = "";
        }

        if(client.isReady)
        {
            readyText.text = "READY";
            readyText.color = new Color32(180, 255, 180, 255);
        }
        else
        {
            readyText.text = "NOT READY";
            readyText.color = new Color32(255, 180, 180, 255);
        }
        colorImg.color = GameManagement.Instance.playerColorCode[client.clientID];
    }
}
