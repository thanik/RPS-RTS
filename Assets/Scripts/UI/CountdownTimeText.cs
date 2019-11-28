using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class CountdownTimeText : MonoBehaviour
{
    TMP_Text text;
    public Button disconnectButton;
    public Button readyButton;
    void Start()
    {
        text = GetComponent<TMP_Text>();
    }

    // Update is called once per frame
    void Update()
    {
        if (text)
        {
            float gameStartTime = GameManagement.Instance.gameStartTime;
            if(gameStartTime == 0)
            {
                if (NetworkClientManager.Instance.netClients.Count == 1)
                {
                    text.text = "Need more players.";
                }
                else
                {
                    text.text = "";
                }
                disconnectButton.interactable = true;
                readyButton.interactable = true;
            }
            else if((gameStartTime - NetworkClientManager.Instance.networkGameTime) < 0)
            {
                text.text = "Game is starting...";
                disconnectButton.interactable = false;
                readyButton.interactable = false;
            }
            else
            {
                text.text = "Game will start in " + (gameStartTime - NetworkClientManager.Instance.networkGameTime).ToString("0.00") +"s.";
            }
        }
    }
}
