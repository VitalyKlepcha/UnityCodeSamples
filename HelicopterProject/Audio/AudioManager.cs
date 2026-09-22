using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using System.Linq;

public class AudioManager : MonoBehaviour
{
    public Sound[] inspSounds;
    public static Sound[] sounds;
    public static AudioManager instance;
    [SerializeField]
    private AudioMixer audioMixer;

    private static Sound pausedSpeaker;

    private static Locale _currentLocale;


    void Awake()
    {
        
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoad;
        foreach (var s in inspSounds)
        {
            s.source = gameObject.AddComponent<AudioSource>();
            s.source.volume = s.volume;
            s.source.pitch = s.pitch;
            s.source.clip = s.clip;
            s.source.loop = s.loop;
            if (s.mixerGroup == "Music")
                s.source.outputAudioMixerGroup = audioMixer.FindMatchingGroups("Music")[0];
            else if (s.mixerGroup == "Sound")
                s.source.outputAudioMixerGroup = audioMixer.FindMatchingGroups("Sound")[0];
            else if (s.mixerGroup == "Voice")
                s.source.outputAudioMixerGroup = audioMixer.FindMatchingGroups("Voice")[0];
            else
                s.source.outputAudioMixerGroup = audioMixer.FindMatchingGroups("Master")[0];
            
        }
        sounds = inspSounds;
        LoadVolume();
        StopAllMusicAndPlay("Summer_Smile");
        _currentLocale = LocalizationSettings.SelectedLocale;
        LocalizationSettings.SelectedLocaleChanged += SelectedLocaleChangedHandler;
    }

    private static void SelectedLocaleChangedHandler(Locale newLocale) => _currentLocale = newLocale; 
    private void LoadVolume()
    {
        float musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        float soundVolume = PlayerPrefs.GetFloat("SoundVolume", 1f);
        audioMixer.SetFloat("Music", Mathf.Log10(musicVolume) * 20f);
        audioMixer.SetFloat("Voice", Mathf.Log10(soundVolume) * 20f);
        audioMixer.SetFloat("Sound", Mathf.Log10(soundVolume) * 20f);
    }

    public static void Play(string name)
    {
        if (sounds == null) { return; }

        Sound[] foundSounds = sounds.Where(s => s.name == name).ToArray();
        Sound chosenSound = null;
        if(foundSounds.Length == 1)
            chosenSound = foundSounds[0];
        else if(foundSounds.Length >1)
            //If multiple sounds goes with same name, select by locale or any if not found
            chosenSound = foundSounds.Where(s => s.locale == _currentLocale).DefaultIfEmpty(foundSounds[0]).First();
        if (chosenSound == null)
            return;
        if (chosenSound.source)
        {
            if ((chosenSound.oneTime && !chosenSound.WasPlayed) || !chosenSound.oneTime)
            {
                if (chosenSound.mixerGroup == "Voice")
                {
                    if (!PlayerPrefsController.IsEducationPassed || HeliAudio_Controller.PlayDespiteConstraints || EducationMode.IsEducationMode)
                    {
                        chosenSound.source.Play();
                        chosenSound.WasPlayed = true;
                        Audio_Skip_Controller.step++;
                    }
                    else
                    {
                        return;
                    }
                }
                chosenSound.source.Play();
                chosenSound.WasPlayed = true;
                //Remember speaker played times
            }
        }
    }

    public static void StopAllAndPlay(string name)
    {
        if (sounds == null) { return; }
        foreach (var s in sounds)
        {
            if (s.name != name)
            {
                s.source.Stop();
            }
                
        }
        Play(name);
    }

    public static void StopAllMusicAndPlay(string name)
    {
        if (sounds == null) { return; }
        foreach (var s in sounds)
        {
            if (s.name != name && s.mixerGroup == "Music")
            {
                s.source.Stop();
            }

        }
        Play(name);
    }

    public static void StopAllVoiceAndPlay(string name)
    {
        if (sounds == null) { return; }
        foreach (var s in sounds)
        {
            if (s.name != name && s.mixerGroup == "Voice")
            {
                s.source.Stop();
            }

        }
        Play(name);
    }

    public static void Stop(string name)
    {
        if (sounds == null) { return; }
        foreach (var s in sounds)
        {
            if (s.name == name)
            {
                s.source.Stop();
            }

        }
    }

    public static void StopAllVoice()
    {
        if (sounds == null) { return; }
        foreach (var s in sounds)
        {
            if (s.mixerGroup == "Voice")
            {
                s.source.Stop();
            }

        }
    }

    public static AudioSource GetSource(string name)
    {
        if (sounds == null) { return null; }
        foreach (var s in sounds)
        {
            if (s.name == name)
            {
                return s.source;
            }

        }
        return null;
    }

    public static bool IsPlaying(string name) {
        AudioSource audioSrc = GetSource(name);
        return (audioSrc != null && audioSrc.isPlaying);
    }

    public static bool IsAnySpeakerPlaying()
    {
        if (sounds == null) { return false; }
        foreach (var s in sounds)
        {
            if (s.mixerGroup == "Voice" && s.source.isPlaying)
            {
                return true;
            }

        }
        return false;
    }

    private  void OnSceneLoad(Scene scene, LoadSceneMode mode)
    {
        ResetWasPlayedAudio();
    }

    public static void ResetWasPlayedAudio()
    {
        if (sounds == null) { return; }
        foreach (var s in sounds)
        {
            if (s.WasPlayed)
            {
                s.WasPlayed = false;
            }

        }
    }
    
    public static void PauseSpeaker()
    {
        if (sounds == null) { return; }
        foreach (var s in sounds)
        {
            if (s.mixerGroup == "Voice" && s.source.isPlaying)
            {
                pausedSpeaker = s;
                s.source.Pause();
            }

        }
    }

    public static void Pause(string name)
    {
        if (sounds == null) { return; }
        foreach(var s in sounds)
        {
            if(s.name == name)
            {
                s.source.Pause();
                break;
            }
        }
    }

    public static void Unpause(string name)
    {
        if (sounds == null) { return; }
        foreach (var s in sounds)
        {
            if (s.name == name)
            {
                s.source.UnPause();
                break;
            }
        }
    }

    public static void UnpauseSpeaker()
    {
        if (pausedSpeaker != null)
        {
            pausedSpeaker.source.UnPause();
        }
        pausedSpeaker = null;
    }

    private void OnDestroy()
    {
        LocalizationSettings.SelectedLocaleChanged -= SelectedLocaleChangedHandler;
    }

}
