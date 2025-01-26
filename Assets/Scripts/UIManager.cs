using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public GameObject[] menus;

    // Start is called before the first frame update
    void Start()
    {
        HideAllMenus();
        ShowMenu(menus[0]);
    }

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

    //Open level
    public void OpenLevel(string levelName)
    {
        EditorSceneManager.LoadScene(levelName);
    }

    //Closes the app
    public void Exit()
    {
        Application.Quit();
    }

    //Sets the music volume
    public void MusicSlider(Slider slider)
    {
        SoundManager.instance.MusicVolume(slider.value);
    }

    //Sets the music volume
    public void SFXSlider(Slider slider)
    {
        SoundManager.instance.SFXVolume(slider.value);
    }

    //Pause the game
    public void Pause(bool pause)
    {

    }

    //Retry current level
    public void Retry()
    {
        EditorSceneManager.LoadScene(EditorSceneManager.GetActiveScene().name);
    }
}
