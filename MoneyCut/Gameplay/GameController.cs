using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;
using Cysharp.Threading.Tasks;

public class GameController : MonoBehaviour
{
    IlevelManager _levelManager;
    ILevelLoader _levelLoader;
    MainMenu _mainMenu;

    [SerializeField]
    private GameObject _finishLevelPopUpPrefab;
    [SerializeField] private EnvironmentGenerator _environmentGenerator;

    [SerializeField]
    private GameObject _gui;

    [SerializeField]
    private InGameInfo _gameInfo;

    [SerializeField]
    private GameObject _player;

    [Inject(Id = "PopUpCanvas")]
    private Canvas _popUpCanvas;

    private bool _levelFinished = false;

    private LevelData _currentLevel;
    private LevelType _currentLevelType;

    //Time used when LevelType is TimeBound
    private float _timeLeft = 60f;
    private float _grassPercentToWin = 50f;

    [SerializeField] private StarProgressUI _starProgressUI;

    private int _currentStarCount = 0;
    private float _starCheckInterval = 1.5f; // How often to check for star progress
    private float _nextStarCheckTime = 0f;

    [Inject]
    private DiContainer _diContainer;

    [Inject]
    public void Setup(IlevelManager levelManager, ILevelLoader levelLoader, MainMenu mainMenu)
    {
        _levelManager = levelManager;
        _levelLoader = levelLoader;
        _mainMenu = mainMenu;
    }

    private async void Start()
    {
        _levelManager.UpdateLevels().Forget();
        var level = await _levelManager.GetFinishedLevel() + 1;
        _levelLoader.LoadLevel(level);
        _environmentGenerator.SaveOriginalState();
        _environmentGenerator.GenerateEnvironment(level);
        _currentLevel = await _levelManager.GetLevel(await _levelManager.GetCurrentLevel());
        _currentLevelType = _currentLevel.LevelType;
        _starProgressUI?.Initialize();
        _starProgressUI?.ResetStars();
    }

    private void OnEnable()
    {
        LevelLoader.AfterLoad += UpdateLevelData;
        LevelLoader.AfterLoad += _gameInfo.UpdateInGameInfoView;
        _mainMenu.OnViewSwitch += _gameInfo.MenuViewSwitchAction;
    }

    private void OnDisable()
    {
        LevelLoader.AfterLoad -= UpdateLevelData;
        LevelLoader.AfterLoad -= _gameInfo.UpdateInGameInfoView;
        _mainMenu.OnViewSwitch -= _gameInfo.MenuViewSwitchAction;
    }

    private void UpdateLevelData(LevelData levelData)
    {
        _currentLevel = levelData;
        _currentLevelType = levelData.LevelType;
        if (_currentLevelType == LevelType.TimeBound)
            _timeLeft = levelData.LevelTime;
        else if(_currentLevelType == LevelType.SpecificGrass)
        {
            _grassPercentToWin = levelData.GrassPercentToWin;
        }
        _currentStarCount = 0;
        _starProgressUI?.ResetStars();
    }

    void Update()
    {
        if (_levelFinished || !_mainMenu.IsGameStarted)
            return;
        // Check star progress at intervals (for performance)
        if (Time.time >= _nextStarCheckTime && _currentLevelType != LevelType.TimeBound)
        {
            UpdateStarProgress();
            _nextStarCheckTime = Time.time + _starCheckInterval;
        }
        switch (_currentLevelType) {
            case LevelType.Classic:  
                CheckClassicLevelProgress();
                break;
            case LevelType.TimeBound:
                CheckTimeBoundLevelProgress();
                break;
            case LevelType.Message:
                CheckMessageLevelProgress();
                break;
            case LevelType.SpecificGrass:
                CheckSpecificGrassLevelProgress();
                break;
            default:break;
         }

    }
    
    private void CheckClassicLevelProgress()
    {
        if (LevelLogic.GrassPercent <= 0.1f)
        {
            FinishLevel(3);
        }
        else if(LevelLogic.FuelPercent <= 0)
        {
            FinishLevel(GetStarProgress());
        }
    }

    private void CheckTimeBoundLevelProgress()
    {
        _timeLeft -= Time.deltaTime;
        _gameInfo.UpdateTimeIndicator(100.0f * Mathf.Clamp01(_timeLeft / _currentLevel.LevelTime));
        if (LevelLogic.GrassPercent <= 0.1f)
        {
            FinishLevel(3);
        }
        else if (_timeLeft <= 0)
        {
            FinishLevel(GetStarProgress());
        }
    }

    private void CheckMessageLevelProgress()
    {
        if (LevelLogic.ZeroPrototypePercent <= 0.1f)
        {
            FinishLevel(3);
        }
        else if (LevelLogic.FuelPercent <= 0)
        {
            FinishLevel(GetStarProgress());
        }
    }

    private void CheckSpecificGrassLevelProgress()
    {
        _gameInfo.UpdateGrassIndicator(100f - 100.0f * Mathf.Clamp01((100f - LevelLogic.ZeroPrototypePercent) / _grassPercentToWin));
        if (LevelLogic.ZeroPrototypePercent <= 100 - _grassPercentToWin)
        {
            FinishLevel(3);
        }
        else if (LevelLogic.FuelPercent <= 0)
        {
            FinishLevel(GetStarProgress());
        }
    }

    private void FinishLevel(int stars)
    {
        _levelFinished = true;
        _levelManager.FinishLevel(stars);
        var popUp = _diContainer.InstantiatePrefab(_finishLevelPopUpPrefab, _popUpCanvas.transform).GetComponent<FinishLevelPopUp>();
        popUp.Initialize(stars, GetFinishTitle(stars));
        _mainMenu.ShowFinishCamera();
        if(_player)
            _player.SetActive(false);
        if(_gui)
            _gui.SetActive(false);

    }

    public void OnEarlyFinishLevelButtonClicked()
    {
        FinishLevel(GetStarProgress());
    }

    private int GetStarProgress()
    {
        int stars = 0;
        switch (_currentLevelType)
        {
            case LevelType.Classic:
            case LevelType.TimeBound:
                if (LevelLogic.GrassPercent <= 5)
                    stars = 3;
                else if (LevelLogic.GrassPercent <= 30)
                    stars = 2;
                else if (LevelLogic.GrassPercent <= 60)
                    stars = 1;
                break;
            case LevelType.Message:
                if (LevelLogic.ZeroPrototypePercent <= 5)
                    stars = 3;
                else if (LevelLogic.ZeroPrototypePercent <= 30)
                    stars = 2;
                else if (LevelLogic.ZeroPrototypePercent <= 60)
                    stars = 1;
                break;
            case LevelType.SpecificGrass:
                if (LevelLogic.ZeroPrototypePercent <= _grassPercentToWin)
                    stars = 3;
                else if (LevelLogic.ZeroPrototypePercent <= _grassPercentToWin * 1.3)
                    stars = 2;
                //80 - min threshold to get one star
                else if (LevelLogic.ZeroPrototypePercent <= _grassPercentToWin * 1.6 && LevelLogic.ZeroPrototypePercent <= 80)
                    stars = 1;
                break;
            default: break;
        }
        return stars;
    }

    public static string GetFinishTitle(int stars)
    {
        switch (stars) {
            case 0: return "Nice try";
            case 1: return "Not bad";
            case 2: return "Good!";
            case 3: return "Perfect!";
            default: return "Good";
        }
    }

    private void UpdateStarProgress()
    {
        int newStarCount = GetStarProgress();

        if (newStarCount > _currentStarCount)
        {
            _starProgressUI?.UpdateStars(newStarCount, true);
            _currentStarCount = newStarCount;
        }
    }

    private void OnDestroy()
    {
        _environmentGenerator.RestoreOriginalState();
    }
}
