using UnityEngine;
using Zenject;

public class PopUpManager : MonoBehaviour, IPopUpManager
{
    private PopUpCanvas _popUpCanvas;

    private int _popUpCount = 0;

    [SerializeField]
    private GameObject _levelPopUpPrefab;

    [Inject]
    void Setup(PopUpCanvas popUpCanvas)
    {
        _popUpCanvas = popUpCanvas;
    }

    public void ShowPopUp(PopUp popUpType)
    {
        var popUp = Instantiate(GetPrefab(popUpType), _popUpCanvas.transform).GetComponent<BasePopUp>();
        _popUpCount++;
        Pause();
        popUp.OnPopUpClosedEvent += () => 
        { 
            _popUpCount--;
            if (_popUpCount == 0)
                Resume();
        };
    }

    private GameObject GetPrefab(PopUp popUpType)
    {
        switch (popUpType)
        {
            case PopUp.LevelUpPopUp: return _levelPopUpPrefab;
            default: return null;
        }
    }

    private void Pause()
    {
        Debug.Log("Pause");
        AudioManager.PauseSpeaker();
        AudioManager.Pause("HeliFly");
        Time.timeScale = 0f;
    }

    private void Resume()
    {
        Debug.Log("Resume");
        AudioManager.UnpauseSpeaker();
        AudioManager.Unpause("HeliFly");
        Time.timeScale = 1f;
    }
}

public enum PopUp
{
    LevelUpPopUp
}