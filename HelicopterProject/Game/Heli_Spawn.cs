using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.UI;
using System;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.Audio;
using Zenject;

public class Heli_Spawn : MonoBehaviour
{
    #region Variables
    private ARRaycastManager raycastManager;

    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    [Header("Buttons")]
    [SerializeField]
    private Button upButton;

    [SerializeField]
    private Button downButton;
    [SerializeField]
    private Button upButtonLandscape;

    [SerializeField]
    private Button downButtonLandscape;

    [SerializeField]
    private Button educationButton;

    [SerializeField]
    private Button freezeButton;

    [SerializeField]
    private Button trickButton;

    [SerializeField]
    private GameObject qrButton;

    [Space]

    [SerializeField]
    private GameObject leftArrow;

    [SerializeField]
    private GameObject rightArrow;

    private GameObject helicopterPrefab;

    private GameObject helicopter;

    [SerializeField]
    private Transform cam;

    private Heli_Input input;

    private GameObject startHeliportPrefab;

    private GameObject startHeliport;

    private Animator heliportAnimator;

    private bool heliportShowed = false;
    private bool helicopterShowed = false;

    public TextMeshProUGUI score;

    public TextMeshProUGUI audioScore;

    private Animator trickButtonAnimator;

    public TrajectoryManager trajectoryManager;

    public AudioMixer audioMixer;

    public FillArea_Controller fillAreaController;
    public FillArea_Controller fillAreaControllerLanscape;

    private Vector3 heliSpawnPos;

    [SerializeField]
    private TextMeshProUGUI _flightTimerTextMesh;

    [Header("Disablers")]
    [SerializeField]
    private GameObject notTutorialDisabler;

    [SerializeField]
    private GameObject trickDisabler;

    [SerializeField]
    private GameObject heliNotAvailableDisabler;

    [Space]


    [SerializeField]
    private GameObject scanButton;

    [SerializeField]
    private HeightControllerBehaviour _heightControllerPortrait;

    [SerializeField]
    private HeightControllerBehaviour _heightControllerLandscape;

    [SerializeField]
    private GameObject[] _collectionItemElements;

    [SerializeField]
    private GameObject _freeFlyTimer;

    //This helicopter will be choosen if Helicopter_Manager is not initialized or not set correctly
    [SerializeField]
    private HelicopterIcon _defaulHeli;

    [Inject]
    DiContainer _diContainer;
    #endregion

    #region Events
    private event Action OnInitialPlaneDestroyed;
    public event Action OnAssemblyEnded;
    public event Action OnAssemblyStarted;
    public event Action OnRespawn;
    #endregion

    #region BuiltInMethods
    void Start()
    {
        audioMixer.SetFloat("Voice", 0);
        helicopter = null;
        raycastManager = GetComponent<ARRaycastManager>();

        HelicopterIcon selHeli = Helicopter_Manager.instance != null ? Helicopter_Manager.instance.selectedHelicopter : _defaulHeli;
        if (selHeli != null)
        {
            helicopterPrefab = selHeli.helicopter;
            startHeliportPrefab = selHeli.startHeliport;
        }


        trickButtonAnimator = trickButton.GetComponent<Animator>();
        if (Helicopter_Manager.CheckIfHeliIsAvailiable())
        {
            AudioManager.Play("Speaker1");
        }
        else
        {
            heliNotAvailableDisabler.SetActive(true);
        }
        if(Helicopter_Manager.instance)
            PlayerPrefsController.SplashHeliNumber = Helicopter_Manager.instance.heliNum;
    }

    
    void Update()
    {
        if(startHeliport == null && !heliportShowed)
        {
            ShowHeliport();
        }
        if (heliportShowed && !helicopterShowed)
        {
            //If assembly seen, spawn helicopter without waiting assembly animation
            if (!heliportAnimator)
            {
                helicopterShowed = true;
                ShowHelicopter();
                OnAssemblyEnded?.Invoke();
            }
            //If assembly animation, including cases when helicopter not available
            else if (heliportAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime > 1.05 && !heliportAnimator.IsInTransition(0))
            {

                ReplaceAssembliedHelicopter();
            }
        }
        ControllTrickButton();
    }

    #endregion

    #region Methods
    public void ReplaceAssembliedHelicopter(bool endRotation = false)
    {
        helicopterShowed = true;
        ShowHelicopter();
        OnAssemblyEnded?.Invoke();
        heliportAnimator.SetBool("Assembly", false);
        if(endRotation)
            heliportAnimator.SetTrigger("Empty");
        Destroy(startHeliport.transform.GetChild(1).gameObject);
    }

    private void ControllTrickButton()
    {
        if (!helicopter)
        {
            return;
        }
        if (HeliAudio_Controller.SpeakerTricks1Played && !AudioManager.GetSource("SpeakerTricks1").isPlaying
            || (!EducationMode.IsEducation() && helicopter.GetComponent<Heli_Characteristics>().NotEducationHeliportDestroyed))
        {
            trickButton.gameObject.SetActive(true);
            if (TrajectoryManager.trickButtonClicked)
            {
                trickButtonAnimator.ResetTrigger("Scale");
                trickButtonAnimator.SetTrigger("Empty");
            }
            else if (HeliAudio_Controller.SpeakerTricks1Played && EducationMode.IsEducation())
            {
                var trickButtonAnimator = trickButton.GetComponent<Animator>();
                trickButtonAnimator.ResetTrigger("Empty");
                trickButtonAnimator.SetTrigger("Scale");
            }
            
        }
        else 
        {
            trickButton.gameObject.SetActive(false);
        }
    }

    private void ShowHeliport()
    {
        if(startHeliportPrefab != null) {
            startHeliport = Instantiate(startHeliportPrefab,
                new Vector3(cam.position.x  + cam.forward.x * 7, cam.position.y - 2f, cam.position.z + (cam.forward *7f).z),
                Quaternion.Euler(0,cam.rotation.eulerAngles.y,0));
        }
        //if assembly seen
        if (Helicopter_Manager.instance != null
            && Helicopter_Manager.instance.IsAssemblySeen() 
            && !EducationMode.IsEducation()
            && Helicopter_Manager.CheckIfHeliIsAvailiable())
        {
            Destroy(startHeliport.transform.GetChild(1).gameObject);
            heliportShowed = true;
        }
        else
        {
            OnAssemblyStarted?.Invoke();
            heliportAnimator = startHeliport.transform.GetChild(0).GetComponent<Animator>();
            heliportAnimator.SetBool("Assembly", true);
            SavePlayerPrefsAssembly();
            heliportShowed = true;
        }
    }

    private void ShowHelicopter()
    {
        var heliPos = startHeliport.transform.position;
        helicopter = _diContainer.InstantiatePrefab(helicopterPrefab,
            new Vector3(heliPos.x - 0.095f, heliPos.y + 0.5f, heliPos.z + -1.46f),
            Quaternion.Euler(0, cam.rotation.eulerAngles.y + 180, 0),
            null);

        input = helicopter.GetComponent<Heli_Input>();
        input.OnTailRotorEnabled += DestroyHeliPort;

        upButtonLandscape.GetComponent<Heli_Rise>().initHelicopter(helicopter);
        downButtonLandscape.GetComponent<Heli_Rise>().initHelicopter(helicopter);
        upButton.GetComponent<Heli_Rise>().initHelicopter(helicopter);
        downButton.GetComponent<Heli_Rise>().initHelicopter(helicopter);
        freezeButton.onClick.AddListener(helicopter.GetComponent<Heli_Controller>().FreezeHelicopter);
        helicopter.GetComponentInChildren<Heli_Animation_Controller>().OnGreetEnded += EnableFreezeButton;
        helicopter.GetComponentInChildren<Heli_Animation_Controller>().OnGreetEnded += EnableTrickButton;
        helicopter.GetComponent<Heli_Crosshair_Controller>().ScoreText = score;
        helicopter.GetComponent<HeliAudioScore_Controller>().ScoreText = audioScore;
        helicopter.GetComponent<HeliDirection_Controller>().LeftArrow = leftArrow;
        helicopter.GetComponent<HeliDirection_Controller>().RightArrow = rightArrow;
        fillAreaController.Engine = helicopter.GetComponentInChildren<Heli_Engine>();
        fillAreaControllerLanscape.Engine = helicopter.GetComponentInChildren<Heli_Engine>();
        _heightControllerLandscape.Engine = helicopter.GetComponentInChildren<Heli_Engine>();
        _heightControllerPortrait.Engine = helicopter.GetComponentInChildren<Heli_Engine>();
        helicopter.GetComponent<Heli_Characteristics>().TrickDisabler = trickDisabler;
        helicopter.GetComponentInChildren<Heli_Animation_Controller>().HeliportAnimator = startHeliport.transform.GetChild(0).GetComponent<Animator>();
        helicopter.GetComponent<Heli_Characteristics>().InitFlightTimer(_flightTimerTextMesh);
        SubscribeEvents();
        //if not education
        if(!EducationMode.IsEducation())
        {
            input.EnableTailRotor();
            helicopter.GetComponent<Heli_Characteristics>().RiseAndRotatePassed = true;
            helicopter.GetComponentInChildren<Heli_Engine>().OnStartMaxRPMAchieved += DelayedDestroyHeliPort;
            notTutorialDisabler.SetActive(true);
            EnableFreezeButton();
            
            downButton.interactable = true;
        }
        
    }

    private void SubscribeEvents()
    {
        OnInitialPlaneDestroyed += _heightControllerLandscape.OnInitialPlaneDestroyed;
        OnInitialPlaneDestroyed += _heightControllerPortrait.OnInitialPlaneDestroyed;
        HeliAudio_Controller.OnEducationEnded += OnEducationEnded;
        HeliAudio_Controller.OnFinalAudioEnded += OnFinalAudioEnded;
    }

    public void DestroyHelicopter()
    {
        if (helicopter)
        {
            helicopter.GetComponent<Heli_Characteristics>().DestroyPlane();
            Destroy(helicopter);
        }
    }

    public void DestroyHeliPort()
    {
        startHeliport.GetComponentInChildren<Rigidbody>().AddForce(Vector3.down * 3, ForceMode.VelocityChange);
        Destroy(startHeliport,3f);
        startHeliport = null;
        helicopter.GetComponent<Heli_Characteristics>().NotEducationHeliportDestroyed = true;
        helicopter.GetComponent<Heli_Characteristics>().EnableCyclic();
        EnableTrickButton();
        if (PlayerPrefsController.IsEducationPassed && !Helicopter_Manager.instance.FinalAudioListened() && !EducationMode.IsEducation())
        {
            Debug.Log("Start final audio");
            _freeFlyTimer.SetActive(false);
            HeliAudio_Controller.PlayDespiteConstraints = true;
            HeliAudio_Controller.PlayFinalAudio();
            HeliAudio_Controller.PlayDespiteConstraints = false;
            Debug.Log(_freeFlyTimer);
            HeliAudio_Controller.OnFinalAudioEnded += EnableFreeFlyTimer;
        }
        else if (!EducationMode.IsEducation())
        {
            EnableFreeFlyTimer();
        }
        if (!EducationMode.IsEducation())
        {
            EnableCollectionItem();
        }
        OnInitialPlaneDestroyed?.Invoke();
    }

    private void DelayedDestroyHeliPort()
    {
        Invoke("DestroyHeliPort", 7f);
    }

    public void Respawn()
    {
        if (TrajectoryManager.isTrajectoryEnded == true && TrajectoryManager.trajectoryImageStarted == false)
        {
            
            AudioManager.Stop("HeliFly");
            OnRespawn?.Invoke();
            DestroyHelicopter();
            if (startHeliport)
            {
                Destroy(startHeliport);
            }
            heliportShowed = false;
            helicopterShowed = false;
            ResetStaticValues();
            if (TrajectoryManager.trajectoryFinger)
            {
                Destroy(TrajectoryManager.trajectoryFinger);
            }
            DisableFreezeButton();
            DisableTrickButton();
            TrajectoryManager.instance.HideTricksPanel();
            AudioManager.StopAllVoice();
            AudioManager.Play("Speaker1");
        }
        
    }

    public static void ResetStaticValues()
    {
        TrajectoryManager.trajectoryImageStarted = false;
        TrajectoryManager.isTrajectoryEnded = true;
        TrajectoryManager.fingerSwipePassed = false;
        TrajectoryManager.tricksPassed = 0;
        HeliAudio_Controller.SpeakerTricks1Played = false;
        HeliAudio_Controller.SpeakerTricks2Played = false;
        HeliAudio_Controller.FinalAudioPlayed = false;
        TrophyManager.isTrophyActive = false;
        Heli_Characteristics.IsHeliLanded = true;
        Audio_Skip_Controller.step = 0;
        AudioManager.ResetWasPlayedAudio();
    }

    public void OnEducationEnded()
    {
        EnableCollectionItem();
    }

    public void OnFinalAudioEnded()
    {
        EnableFreeFlyTimer();
    }

    private void EnableFreeFlyTimer()
    {
        _freeFlyTimer.SetActive(true);
    }

    public void EnableTrickButton()
    {
        if(PlayerPrefs.GetInt("SpeakerTricks1Listened", 0) == 1) {
            trickButton.gameObject.SetActive(true);
        }
    }

    private void EnableCollectionItem()
    {
        foreach(var e in _collectionItemElements)
        {
            e.SetActive(true);
        }
    }

    public void DisableTrickButton()
    {
        trickButton.gameObject.SetActive(false);
    }

    public void EnableFreezeButton()
    {
        freezeButton.gameObject.SetActive(true);
    }

    public void DisableFreezeButton()
    {
        freezeButton.gameObject.SetActive(false);
    }

    public void SavePlayerPrefsAssembly()
    {
        switch (Helicopter_Manager.instance.currentHeliType) {
            case HeliType.Medic     : PlayerPrefsController.IsAssemblySeen_Medic = true; break;
            case HeliType.Fire      : PlayerPrefsController.IsAssemblySeen_Fire = true; break;
            case HeliType.Cargo     : PlayerPrefsController.IsAssemblySeen_Cargo = true; break;
            case HeliType.Police    : PlayerPrefsController.IsAssemblySeen_Police = true; break;
            case HeliType.Safari    : PlayerPrefsController.IsAssemblySeen_Safari = true; break;
            case HeliType.Military  : PlayerPrefsController.IsAssemblySeen_Military = true; break;
            case HeliType.Shmel     : PlayerPrefsController.IsAssemblySeen_Shmel = true; break;
            case HeliType.Grom      : PlayerPrefsController.IsAssemblySeen_Grom = true; break;
            default: 
                Debug.Log("ZP: Heli_Spawn.SavePlayerPrefsAssembly: currentHeliType = '" + Helicopter_Manager.instance.currentHeliType + "'  Not Found!!!!!!"); 
            break;
        }
    }

    private void OnDestroy()
    {
        HeliAudio_Controller.OnEducationEnded -= OnEducationEnded;
        HeliAudio_Controller.OnFinalAudioEnded -= OnFinalAudioEnded;
    }

    #endregion
}
