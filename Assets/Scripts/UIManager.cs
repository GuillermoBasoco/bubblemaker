using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public GameObject[] menus;

    //Hides every menu
    public void HideAllMenus()
    {
        foreach (GameObject menu in menus)
        {
            menu.SetActive(false);
        }
    }

    //Shows an specific menu
    public void ShowMenu(GameObject menu)
    {
        HideAllMenus();
        menu.SetActive(true);
    }
}
