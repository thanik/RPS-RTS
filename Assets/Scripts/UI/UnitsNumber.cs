using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UnitsNumber : MonoBehaviour
{
    TMP_Text text;
    // Start is called before the first frame update
    void Start()
    {
        text = GetComponent<TMP_Text>();
    }

    // Update is called once per frame
    void Update()
    {
        if(GameManagement.Instance.gameMode == GameMode.CLIENT && GameManagement.Instance.playerData.ContainsKey(NetworkClientManager.Instance.myClientID))
        {
            text.text = GameManagement.Instance.playerData[NetworkClientManager.Instance.myClientID].numberOfUnits + "/" + GameManagement.Instance.maxUnitsPerPlayer;
        }
        else
        {
            text.text = "";
        }
    }
}
