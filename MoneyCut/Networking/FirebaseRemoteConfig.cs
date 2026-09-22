using Cysharp.Threading.Tasks;
using Firebase;
using Firebase.Extensions;
using Firebase.RemoteConfig;
using System;
using System.Threading.Tasks;
using Config = Firebase.RemoteConfig.FirebaseRemoteConfig;
public class FirebaseRemoteConfig : IRemoteConfig
{
    private readonly Config _remoteConfig;

    public Action OnRemoteDataFetched { get;  set; }

    public bool IsRemoteDataFetched { get => _isRemoteDataFetched; }

    private bool _isRemoteDataFetched;

    FirebaseRemoteConfig()
    {
        _remoteConfig = Config.DefaultInstance;
        _remoteConfig.SetDefaultsAsync(Parameters.GetDefaults());
    }

    public bool GetBool(Parameter parameter) => _remoteConfig.GetValue(parameter.GetKey()).BooleanValue;

    public T GetClass<T>(Parameter parameter)
    {
        var json = _remoteConfig.GetValue(parameter.GetKey()).StringValue;
        return UnityEngine.JsonUtility.FromJson<T>(json);
    }

    public float GetFloat(Parameter parameter)
    {
        var stringValue = _remoteConfig.GetValue(parameter.GetKey()).StringValue;

        if (float.TryParse(stringValue, out var floatValue))
            return floatValue;

        var defaultValues = Parameters.GetDefaults();
        if (defaultValues.ContainsKey(parameter.GetKey()))
        {
            var defaultValue = defaultValues[parameter.GetKey()].ToString();

            if (float.TryParse(defaultValue, out var defaultFloatValue))
                return defaultFloatValue;
        }

        return default;
    }

    public int GetInt(Parameter parameter)
    {
        var stringValue = _remoteConfig.GetValue(parameter.GetKey()).StringValue;

        if (int.TryParse(stringValue, out var intValue))
            return intValue;

        var defaultValues = Parameters.GetDefaults();
        if (defaultValues.ContainsKey(parameter.GetKey()))
        {
            var defaultValue = defaultValues[parameter.GetKey()].ToString();

            if (int.TryParse(defaultValue, out var defaultIntValue))
                return defaultIntValue;
        }

        return default;
    }

    public string GetString(Parameter parameter) => _remoteConfig.GetValue(parameter.GetKey()).StringValue;

    public UniTask FetchDataAsync()
    {
        Log.Debug("Fetching data...");
        Task fetchTask = _remoteConfig.FetchAsync(TimeSpan.Zero);
        return fetchTask.ContinueWithOnMainThread(FetchComplete).AsUniTask();
    }

    async void FetchComplete(Task fetchTask)
    {
        if (fetchTask.IsCanceled)
        {
            Log.Debug("Fetch canceled.");
        }
        else if (fetchTask.IsFaulted)
        {
            Log.Debug("Fetch encountered an error.");
        }
        else if (fetchTask.IsCompleted)
        {
            Log.Debug("Fetch completed successfully!");
            OnRemoteDataFetched?.Invoke();
            _isRemoteDataFetched = true;
        }

        var info = _remoteConfig.Info;

        switch (info.LastFetchStatus)
        {
            case LastFetchStatus.Success:
                if (await _remoteConfig.ActivateAsync())
                {
                    Log.Debug($"Remote data loaded and ready (last fetch time {info.FetchTime}).");
                }
                else
                {
                    Log.Debug("Activate fetched failed");
                }
                break;
            case LastFetchStatus.Failure:
                switch (info.LastFetchFailureReason)
                {
                    case FetchFailureReason.Error:
                        Log.Debug("Fetch failed for unknown reason");
                        break;
                    case FetchFailureReason.Throttled:
                        Log.Debug($"Fetch throttled until {info.ThrottledEndTime}");
                        break;
                }
                break;
            case LastFetchStatus.Pending:
                Log.Debug("Latest Fetch call still pending.");
                break;
        }
    }
}