using PaintIn3D;
using System;
using System.Collections;
using System.Collections.Generic;
using UltimateSlider;
using UnityEngine;
using UnityEngine.UI;
public class TrajectoryManager : MonoBehaviour
{
    public static TrajectoryManager instance;
    #region Variables

    public Trajectory[] trajectories;

    public static Transform lastTrajectoryCheckpoints { get => currentTr.trajectoryFlight.transform;}

    public static bool fingerSwipePassed = false;

    public static bool isTrickPanelOpened = false;

    public GameObject tricksPanel;


    public static bool isTrajectoryEnded = true;

    public static GameObject trajectoryFinger;

    public Canvas canvas;

    public  GameObject paintPlane;

    private Animator planeAnimator;

    public static Trajectory currentTr;

    public static string TrajectoryName { get => currentTr.name; }

    [SerializeField]
    private Camera cam;

    public static bool trajectoryImageStarted = false;

    //Education mode variables 

    public static bool trickButtonClicked = false;

    private static bool educationMode = false;

    public static int tricksPassed = 0;

    public static bool TrajectoryEducationMode { get => educationMode; set => educationMode = value; }


    public SliderManager slider;

    [SerializeField]
    private GameObject trophy;

    [SerializeField]
    private GameObject certificate;

    private bool isTrophyOpened = false;

    [SerializeField]
    private GameObject uiDisabler;

    private float lastInteractionTime;

    [SerializeField]
    private GameObject tip;

    #endregion

    #region Built-in methods
    private void Start()
    {
        HideTricksPanel();
        LoadTrickItemStars();
        LoadTrickItemScore();
    }

    private void Update()
    {
        if (educationMode && tricksPassed == 3 && isTrajectoryEnded)
        {
            HideTricksPanel();
            educationMode = false;
            trophy.SetActive(true);
            var animator = trophy.GetComponent<Animator>();
            animator.ResetTrigger("Empty");
            animator.SetTrigger("Scale");
            if (PlayerPrefs.GetInt("SpeakerBecamePilotListened", 0) == 0)
            {
                AudioManager.Play("SpeakerBecamePilot");
                //When speaker ended + 2 seconds
                Invoke("DisableTrophyAndPlayFinalAudio", 21f);
            }
        }
        HandleUIDisabler();
        HandleTip();
    }

    #endregion

    #region Methods
    private void HandleTip()
    {
        if(isTrajectoryEnded && Time.time - lastInteractionTime > 2f)
        {
            tip.SetActive(true);
        }
        else
        {
            tip.SetActive(false);
        }
    }

    public void UpdateInteractionTime()
    {
        lastInteractionTime = Time.time;
    }

    public void OnTrickMenuBtnClick()
    {
        trickButtonClicked = true;
        if (!isTrickPanelOpened)
        {
            UpdateInteractionTime();
            ShowTricksPanel();
            //if wasn't played
            if (!AudioManager.IsAnySpeakerPlaying() && HeliAudio_Controller.SpeakerTricks2Played == false && EducationMode.IsEducation())
            {
                HeliAudio_Controller.SpeakerTricks2Played = true;
                AudioManager.Play("SpeakerTricks2");
                educationMode = true;
            }
        }
        else if (!educationMode)
        {
            HideTricksPanel();
        }
    }

    public void OnTrajectoryButtonClick(string name)
    {
        Debug.Log("OnTrajectoryButtonClick: name: " + name);
        Debug.Log("OnTrajectoryButtonClick: isTrajectoryEnded: " + isTrajectoryEnded);
        Debug.Log("OnTrajectoryButtonClick: IsHeliLanded: " + Heli_Characteristics.IsHeliLanded);
        if (!isTrajectoryEnded || Heli_Characteristics.IsHeliLanded) {
            return;
        }
        trajectoryImageStarted = true;
        foreach (var tr in trajectories)
        {
            if (tr.name == name)
            {
                paintPlane = Instantiate(tr.trajectoryFinger, cam.transform, false);
                paintPlane.transform.localPosition = tr.offset;
                paintPlane.SetActive(true);
                paintPlane.GetComponentInChildren<P3dPaintable>().enabled = false;
                planeAnimator = paintPlane.GetComponentInChildren<Animator>();
                planeAnimator.ResetTrigger("Disappear");
                planeAnimator.SetTrigger("Appear");
                currentTr = tr;
                isTrajectoryEnded = false;
                if (educationMode)
                {
                    HandleSelectedTrickUI();
                    StartCoroutine(UpdateEducationTricks(currentTr));
                }
                else
                {
                    HandleSelectedTrickUI();
                }
            }
        }
    }

    private IEnumerator UpdateEducationTricks(Trajectory madeTrajectory)
    {
        yield return new WaitUntil(() => isTrajectoryEnded);
        if (tricksPassed >= 3)
        {
            foreach (var trajectory in trajectories)
            {
                trajectory.trickItem.Icon.GetComponent<Button>().interactable = true;
                foreach (var star in trajectory.trickItem.Stars)
                {
                    ImageUtility.SetAlpha(star, 1f);
                }
            }
        }
        else
        {
            foreach (var trajectory in trajectories)
            {
                if (trajectory == madeTrajectory)
                {
                    trajectory.trickItem.Icon.GetComponent<Button>().interactable = false;
                    foreach (var star in trajectory.trickItem.Stars)
                    {
                        ImageUtility.SetAlpha(star, 0.4f);
                    }
                }
            }
        }
    }

    private void InstantiateImage()
    {
        isTrajectoryEnded = false;
        Instantiate(currentTr.trajectoryFinger);
    }

    #region ButtonMethods
    public void OnTrophyClicked()
    {
        certificate.SetActive(true);
        isTrophyOpened = true;
        trophy.SetActive(false);
    }

    public void OnCertificateCancelClicked()
    {
        TrophyManager.isTrophyActive = true;
        certificate.SetActive(false);
        if (isTrophyOpened)
        {
            HeliAudio_Controller.PlayFinalAudio();
        }
    }
    #endregion

    private void DisableTrophyAndPlayFinalAudio()
    {
        if (!isTrophyOpened)
        {
            trophy.SetActive(false);
            HeliAudio_Controller.PlayFinalAudio();
            TrophyManager.isTrophyActive = true;
        }
    }

    private void HandleUIDisabler()
    {
        if (!isTrajectoryEnded)
        {
            uiDisabler.SetActive(true);
        }
        else
        {
            uiDisabler.SetActive(false);
        }
    }
    
    private void HandleSelectedTrickUI()
    {
        foreach(var trajectory in trajectories)
        {
            if(trajectory != currentTr)
            {
                ImageUtility.SetAlpha(trajectory.trickItem.Icon, 0.4f);
                foreach(var star in trajectory.trickItem.Stars)
                {
                    ImageUtility.SetAlpha(star, 0.4f);
                }
            }
            else
            {
                trajectory.trickItem.Frame.SetActive(true);
            }
        }
        currentTr.trickItem.Animator.SetTrigger("Enable");
        StartCoroutine(EnableTrickItemAfterTrajectoryEndRoutine(currentTr));
    }

    private IEnumerator EnableTrickItemAfterTrajectoryEndRoutine(Trajectory currentTrajectory)
    {
         yield return new WaitWhile(() => isTrajectoryEnded == false);
        currentTrajectory.trickItem.Animator.SetTrigger("Disable");
        foreach (var trajectory in trajectories)
        {
            ImageUtility.SetAlpha(trajectory.trickItem.Icon, 1f);
            foreach (var star in trajectory.trickItem.Stars)
            {
                ImageUtility.SetAlpha(star, 1f);
            }
            trajectory.trickItem.Frame.SetActive(false);
        }
    }

    public void HideTricksPanel() {
        if (tricksPanel != null) {
            tricksPanel.gameObject.SetActive(false);
        }
        isTrickPanelOpened = false;
    }

    public void ShowTricksPanel() {
        if (tricksPanel != null) {
            tricksPanel.gameObject.SetActive(true);
        }
        isTrickPanelOpened = true;
    }

    private void LoadTrickItemStars()
    {
        foreach(var tr in trajectories)
        {
            tr.trickItem.SetAchievedStars(PlayerPrefsController.GetTrickStar(tr.name));
        }
    }

    private void LoadTrickItemScore()
    {
        foreach (var tr in trajectories)
        {
            tr.trickItem.SetScore(PlayerPrefsController.GetTrickScore(tr.name));
        }
    }
    #endregion
}
