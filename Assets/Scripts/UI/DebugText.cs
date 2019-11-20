using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DebugText : MonoBehaviour
{
    // Start is called before the first frame update
    TMP_Text text;
    void Start()
    {
        text = GetComponent<TMP_Text>();
    }

    // Update is called once per frame
    void Update()
    {
        string content;
        content = "Debug\n";
        content += "Mode: " + GameManagement.Instance.gameMode.ToString() + "\n";
        content += "State: " + GameManagement.Instance.gameState.ToString() + "\n";
        if (GameManagement.Instance.gameMode == GameMode.CLIENT)
        {
            content += "My Client ID: " + NetworkClientManager.Instance.myClientID + "\n";
            content += "Game Time: " + NetworkClientManager.Instance.networkGameTime.ToString("0.####") + " s\n";
            //content += "Latest packet game time: " + latestGameTime.ToString() + "\n";
            content += "Latency: " + (NetworkClientManager.Instance.latency * 1000).ToString("0.##") + " ms\n";
            content += "Time Delta: " + NetworkClientManager.Instance.timeDelta.ToString("0.####") + " s\n";
            content += "Bytes in: " + NetworkClientManager.Instance.bytesIn.ToString() + "\n";
            content += "Bytes out: " + NetworkClientManager.Instance.bytesOut.ToString() + "\n";
        }
        else if (GameManagement.Instance.gameMode == GameMode.SERVER || (GameManagement.Instance.gameMode == GameMode.LISTEN))
        {
            content += "Game Time: " + NetworkServerManager.Instance.networkGameTime + "\n";
            content += "Clients: " + NetworkServerManager.Instance.netClients.Count + "\n";
        }

        text.text = content;
    }
}
