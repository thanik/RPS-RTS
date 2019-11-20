using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class JoinGameButton : MonoBehaviour
{
    public TMP_InputField nicknameText;
    public TMP_InputField ipAddressText;
    public TMP_InputField portNumberText;
    void Start()
    {
        GetComponent<Button>().onClick.AddListener(onClick);
    }

    void onClick()
    {
        string nickname = nicknameText.text;
        if (nickname == "")
        {
            nickname = "Player";
        }
        NetworkClientManager.Instance.connectToServer(nickname, ipAddressText.text, int.Parse(portNumberText.text));

    }

}
