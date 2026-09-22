using UnityEngine;
using System;
using System.Collections;

public class HeliAudio_Controller : MonoBehaviour
{
    #region Variables
    static private  HeliAudio_Controller instance;

    private bool initInputEvenets = false;

    private AudioSource flySound;

    private Heli_Input input;

    private static bool speakerTricks1Played = false;

    public static bool SpeakerTricks1Played { get => speakerTricks1Played; set => speakerTricks1Played = value; }

    private static bool speakerTricks2Played = false;

    public static bool SpeakerTricks2Played { get => speakerTricks2Played; set => speakerTricks2Played = value; }

    private static bool finalAudioPlayed = false;

    public static bool FinalAudioPlayed { get => finalAudioPlayed; set => finalAudioPlayed = value; }
    public static bool PlayDespiteConstraints { get => playDespiteConstraints; set => playDespiteConstraints = value; }

    private static bool playDespiteConstraints = false;

    public static event Action OnEducationEnded;

    public static event Action OnFinalAudioEnded;

    #endregion

    #region BuiltInMethods
    private void Start()
    {
        instance = this;
        input = GetComponent<Heli_Input>();
        //if wasn't played
        input.OnTailRotorEnabled += PlaySpeaker3Audio;
        //if wasn't played
        input.OnTailRotorEnabled += PlaySpeakersAudioWithDelay;
    }
    #endregion

    #region Methods
    public void HandleAudio(Heli_Input input,Heli_Engine engine)
    {
        if (initInputEvenets == false)
        {
            input.OnMainRotorEnabled += PlayFlyAudio;
            initInputEvenets = true;
        }
        if (flySound)
        {
            var volume = Mathf.InverseLerp(0, engine.StandartRPM, engine.CurRPM);
            flySound.volume = volume/2.5f;
        }
    }

    public void PlayEngineAudio()
    {
        AudioManager.Play("HeliEngine");
    }

    public void PlayFlyAudioWithDelay(float time)
    {
        Invoke("PlayFlyAudio", 1f);
    }

    public void PlayFlyAudio()
    {
        AudioManager.Stop("HeliEngine");
        AudioManager.Play("HeliFly");
        flySound = AudioManager.GetSource("HeliFly");
    }

    public void PlaySpeaker3Audio()
    {
        AudioManager.Play("Speaker3");
    }

    public void PlaySpeakersAudioWithDelay()
    {
        Invoke("PlaySpeaker4Audio", 24f);
        Invoke("PlaySpeakerTricks1Audio", 89f);
    }

    public void PlaySpeaker4Audio()
    {
        if (!AudioManager.IsAnySpeakerPlaying())
        {
            AudioManager.Play("Speaker4");
        }
    }

    public void PlaySpeakerTricks1Audio()
    {
        if (!AudioManager.IsAnySpeakerPlaying())
        {
            if (PlayerPrefs.GetInt("SpeakerTricks1Listened", 0) == 0)
            {
                AudioManager.Play("SpeakerTricks1");
                speakerTricks1Played = true;
            }
        }
    }

    public static void PlayFinalAudio()
    {
        var helicopterType = Helicopter_Manager.instance.selectedHelicopter.type;
        switch (helicopterType) {
            case HeliType.Medic:
                AudioManager.StopAllVoiceAndPlay("SpeakerFinalMedic");
                PlayerPrefsController.IsFinalAudioListened_Medic = true;
            break;

            case HeliType.Military:
                AudioManager.StopAllVoiceAndPlay("SpeakerFinalMilitary");
                PlayerPrefsController.IsFinalAudioListened_Military = true;   
            break;

            case HeliType.Safari:
                AudioManager.StopAllVoiceAndPlay("SpeakerFinalMilitary");
                PlayerPrefsController.IsFinalAudioListened_Safari = true;   
            break;

            case HeliType.Fire:
                AudioManager.StopAllVoiceAndPlay("SpeakerFinalFire");
                PlayerPrefsController.IsFinalAudioListened_Fire = true;
            break;

            case HeliType.Police:
                AudioManager.StopAllVoiceAndPlay("SpeakerFinalPolice");
                PlayerPrefsController.IsFinalAudioListened_Police = true;
            break;

            case HeliType.Cargo:
                AudioManager.StopAllVoiceAndPlay("SpeakerFinalCargo");
                PlayerPrefsController.IsFinalAudioListened_Cargo = true;
            break;
            default: 
                Debug.Log("ZP: HeliAudio_Controller.PlayFinalAudio: helicopterType = '" + helicopterType + "'  Not Found!"); 
            break;
        }
        if (EducationMode.IsEducation())
            OnEducationEnded?.Invoke();
        instance.StartCoroutine(AfterFinalAudioProcessing());
        finalAudioPlayed = true;
        PlayerPrefsController.IsEducationPassed = true;
    }

    private static IEnumerator AfterFinalAudioProcessing()
    {
        yield return new WaitWhile(() => AudioManager.IsAnySpeakerPlaying());
        Debug.Log("Call OnFinalAudioEnded");
        OnFinalAudioEnded?.Invoke();
    }
    #endregion
}
