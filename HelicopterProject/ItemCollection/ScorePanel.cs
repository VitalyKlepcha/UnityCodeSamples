using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ScorePanel : MonoBehaviour
{
    #region Variables

    [SerializeField]
    private List<ScorePanelState> _states;

    private static ScorePanelState _currentState;

    public static ScorePanelState CurrentState { get => _currentState; }

    #endregion

    #region Methods

    void Start()
    {
        SetState(ScorePanelType.Separate);
    }

    public void SetState(ScorePanelType type)
    {
        if (_currentState != null && _currentState.Type == type)
            return;
        foreach (var state in _states)
        {
            if( state.Type == type)
            {
                ChangeState(state);
            }
        }
    }

    public void SetNextState()
    {
        var newState = _states.SkipWhile(x => x != _currentState).Skip(1).DefaultIfEmpty(_states[0]).FirstOrDefault();
        ChangeState(newState);
    }

    private void ChangeState(ScorePanelState newState)
    {
        if(_currentState)
            _currentState.Panel.SetActive(false);

        newState.Panel.SetActive(true);
        _currentState = newState;
    }

    #endregion
}

public enum ScorePanelType
{
    Mutual,
    Separate,
    FreeFly
}