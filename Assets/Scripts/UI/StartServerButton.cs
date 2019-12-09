using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class StartServerButton : MonoBehaviour
{
    private Button button;
    public Button stopServerButton;
    public TMP_InputField portNumberField;
    void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(onClick);
    }

    // Update is called once per frame
    void onClick()
    {
        int portNumber = int.Parse(portNumberField.text);
        if (portNumber > 0 && portNumber < 65535)
        {
            NetworkServerManager.Instance.setupServer(portNumber);
            stopServerButton.interactable = true;
            portNumberField.interactable = false;
            button.interactable = false;
        }
    }
}
