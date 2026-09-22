using UnityEngine;

/// <summary>
/// Base class for MonoBehaviour scripts that need to save data when the app pauses, loses focus, or is disabled.
/// </summary>
public abstract class AutoSaveBehaviour : MonoBehaviour
{
    [SerializeField, Tooltip("Auto-save interval in seconds. Set to 0 to disable.")]
    private float _autoSaveInterval = 180f;

    private float _lastSaveTime;

    protected void Update()
    {
        if (_autoSaveInterval > 0 && Time.time - _lastSaveTime >= _autoSaveInterval)
        {
            SaveData();
            _lastSaveTime = Time.time;
        }
    }

    protected abstract void SaveData();

    private void OnDisable()
    {
        SaveData();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        SaveData();
    }

    private void OnApplicationPause(bool pause)
    {
        SaveData();
    }

    private void OnApplicationQuit()
    {
        SaveData();
    }
}