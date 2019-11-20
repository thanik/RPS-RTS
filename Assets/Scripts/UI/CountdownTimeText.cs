using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class CountdownTimeText : MonoBehaviour
{
    TMP_Text text;
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
            }
            else
            {
                text.text = "Game will start in " + (gameStartTime - NetworkClientManager.Instance.networkGameTime).ToString("0.00") +"s.";
            }
        }
    }
}
