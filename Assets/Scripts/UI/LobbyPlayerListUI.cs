using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LobbyPlayerListUI : MonoBehaviour
{
    public GameObject playerPrefab;
    public List<GameObject> playerInList = new List<GameObject>();
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        List<NetworkClient> netClients = NetworkClientManager.Instance.netClients;
        if (netClients.Count > playerInList.Count)
        {
            playerInList.Add(Instantiate(playerPrefab, gameObject.transform));
        }
        else if(netClients.Count < playerInList.Count)
        {
            GameObject lastObject = playerInList[playerInList.Count - 1];
            playerInList.Remove(lastObject);
            Destroy(lastObject);
        }
        for(int i = 0; i < playerInList.Count; i++)
        {
            playerInList[i].GetComponent<PlayerInListUI>().updateUI(netClients[i]);
        }
    }
}
