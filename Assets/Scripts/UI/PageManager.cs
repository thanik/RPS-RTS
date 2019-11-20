using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PageManager : MonoBehaviour
{
    public List<GameObject> pages = new List<GameObject>();
    public GameObject errorDialog;
    public TMP_Text errorText;
    // Start is called before the first frame update

    void Start()
    {
        foreach (GameObject page in pages)
        {
            page.SetActive(false);
        }
        pages[0].SetActive(true);
        errorDialog.SetActive(false);
    }

    public void changePage(int pageIndex)
    {
        foreach(GameObject page in pages)
        {
            page.SetActive(false);
        }
        pages[pageIndex].SetActive(true);
    }

    public void showError(string errorString)
    {
        errorDialog.SetActive(true);
        errorText.text = errorString;
    }

    public void closeErrorDialog()
    {
        errorDialog.SetActive(false);
    }
}
