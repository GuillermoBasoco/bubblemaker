using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;

    public string BGMusic;
    public Sound[] music, SFXs;
    public AudioSource musicSource, SFXSource;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            instance.BGMusic = BGMusic;
            Destroy(gameObject);
            return;
        }        
    }

    // Start is called before the first frame update
    void Start()
    {
        PlayMusic(BGMusic); 
    }

    //Play music by music name
    public void PlayMusic(string name)
    {
        Sound s = System.Array.Find(music, sound => sound.name == name);
        if (s == null)
        {
            Debug.LogWarning("Music: " + name + " not found!");
            return;
        }
        else
        {
            musicSource.clip = s.clip;
            musicSource.Play();
        }
    }

    //Play sound by sound name
    public void PlaySFX(string name)
    {
        Sound s = System.Array.Find(SFXs, sound => sound.name == name);
        if (s == null)
        {
            Debug.LogWarning("Sound: " + name + " not found!");
            return;
        }
        else
        {
            SFXSource.clip = s.clip;
            SFXSource.Play();
        }
    }

    //Mutes and unmutes the music source
    public void ToggleMusic()
    {
        musicSource.mute = !musicSource.mute;
    }

    //Mutes and unmutes the sfx source
    public void ToggleSFX()
    {
        SFXSource.mute = !SFXSource.mute;
        instance.PlaySFX("UI");
    }

    //Changes the volume of the music
    public void MusicVolume(float volume)
    {
        musicSource.volume = volume * volume;
    }

    //Changes the volume of the SFX
    public void SFXVolume(float volume)
    {
        SFXSource.volume = volume * volume;
    }
}
