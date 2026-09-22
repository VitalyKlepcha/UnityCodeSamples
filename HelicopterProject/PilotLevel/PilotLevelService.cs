using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.SmartFormat.Extensions;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
using Zenject;


public class PilotLevelService : IPilotLevelService
{
    private int _level = 1;

    private PilotLevelConfig _pilotLevelConfig;

    public event Action OnLevelChanged;
    public event ExpChangeHandler OnExpChanged;

    public int CurrentLevel { get => _level; }

    public int CurrentExp { get; private set; }

    public int ExpToLevelUp { get; private set; }

    IntVariable _levelVariable;

    IRemoteConfig _remoteConfig;
    IPopUpManager _popUpManager;

    [Inject]
    PilotLevelService(IRemoteConfig remoteConfig, IPopUpManager popUpManager)
    {
        _remoteConfig = remoteConfig;
        _popUpManager = popUpManager;
        var source = LocalizationSettings.StringDatabase.SmartFormatter.GetSourceExtension<PersistentVariablesSource>();
        _levelVariable = source["global"]["pilotLevel"] as IntVariable;
        _level = PlayerPrefsController.PilotLevel;
        _levelVariable.Value = _level;
        if (_remoteConfig.IsRemoteDataFetched)
        {
            _pilotLevelConfig = _remoteConfig.GetClass<PilotLevelConfig>(Parameter.PilotLevelConfig);
            CalculateCurrentLevel(true);
        }
        else
        {
            _remoteConfig.OnRemoteDataFetched += () =>
            {
                _pilotLevelConfig = _remoteConfig.GetClass<PilotLevelConfig>(Parameter.PilotLevelConfig);
                CalculateCurrentLevel(true);
            };
        }
    }

    public void CalculateCurrentLevel(bool blockPopUp = false)
    {
        //Cache level and recalculate
        int prevCalculatedLevel = _level;
        _level = 1;

        var mutualItemScore = PlayerPrefsController.CollectionItem_MutualScore;
        var mutualTrickScore = PlayerPrefsController.GetTrickScore("Trajectory8") + PlayerPrefsController.GetTrickScore("TrajectoryTriangle")
            + PlayerPrefsController.GetTrickScore("TrajectoryCircle");
        int mutualXp = Mathf.FloorToInt(mutualItemScore / _pilotLevelConfig.MutualScoreDivider) + Mathf.FloorToInt(mutualTrickScore/ _pilotLevelConfig.TrickScoreDivider);

        //Level up cycle
        int xpToLevelUp = _pilotLevelConfig.InitialXpToLevelUp;
        while(mutualXp > xpToLevelUp)
        {
            mutualXp -= xpToLevelUp;
            xpToLevelUp = Mathf.RoundToInt(xpToLevelUp * _pilotLevelConfig.XpToLevelUpMultiplier);
            _level++;
        }

        CurrentExp = mutualXp;
        ExpToLevelUp = xpToLevelUp;
        OnExpChanged?.Invoke(CurrentExp, ExpToLevelUp);
        //If level updated
        if(_level != prevCalculatedLevel)
        {
            if (_level < prevCalculatedLevel && !_pilotLevelConfig.DecreaseLevel)
            {
                _level = prevCalculatedLevel;
                return;
            }
            OnLevelChanged?.Invoke();
            PlayerPrefsController.PilotLevel = _level;
            _levelVariable.Value = _level;

            //Do not show when level is decreased
            if (!blockPopUp && _pilotLevelConfig.ShowLevelUpPopUp && _level > prevCalculatedLevel)
                _popUpManager.ShowPopUp(PopUp.LevelUpPopUp);
        }
    }
}
