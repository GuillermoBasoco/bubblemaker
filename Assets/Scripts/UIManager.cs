using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public GameObject[] menus;
    public Slider musicSlider, sfxSlider;
    public Toggle musicToggle, sfxToggle;

    // Start is called before the first frame update
    void Start()
    {
        HideAllMenus();
        ShowMenu(menus[0]);
        //musicSlider.value = SoundManager.instance.musicV;
        //sfxSlider.value = SoundManager.instance.sfxV;
        musicToggle.isOn = !SoundManager.instance.musicSource.mute;
        sfxToggle.isOn = !SoundManager.instance.SFXSource.mute;
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
        SceneManager.LoadScene(levelName);
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

    public void toggleMusic()
    {
        SoundManager.instance.ToggleMusic();
    }

    public void toggleSFX()
    {
        SoundManager.instance.ToggleSFX();
    }

    //Pause the game
    public void Pause(bool pause)
    {

    }

    //Retry current level
    public void Retry()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void PlaySFX(string name)
    {
        SoundManager.instance.PlaySFX(name);
    }

    public void PlayMusic(string name)
    {
        SoundManager.instance.PlayMusic(name);
    }
}
