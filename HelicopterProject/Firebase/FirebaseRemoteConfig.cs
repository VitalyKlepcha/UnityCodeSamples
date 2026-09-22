using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using Firebase.Extensions;
using System;
using Config = Firebase.RemoteConfig.FirebaseRemoteConfig;

public class FirebaseRemoteConfig: IRemoteConfig
{

    Firebase.DependencyStatus dependencyStatus = Firebase.DependencyStatus.UnavailableOther;

    private bool _isFirebaseInitialized = false;

    private bool _isRemoteDataFetched = false;

    public bool IsFirebaseInitialized { get => _isFirebaseInitialized; }

    /// <summary>
    /// Indicates that remote data fetch is finished, successfully or not
    /// </summary>
    public bool IsRemoteDataFetched { get => _isRemoteDataFetched; set => _isRemoteDataFetched = value; }
    public Action OnRemoteDataFetched { get; set; }

    private Config _remoteConfig;

    FirebaseRemoteConfig()
    {
        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            dependencyStatus = task.Result;
            if (dependencyStatus == Firebase.DependencyStatus.Available)
            {
                _remoteConfig = Config.DefaultInstance;
                InitializeFirebase();
                FetchDataAsync();
            }
            else
            {
                Debug.LogError(
                  "Could not resolve all Firebase dependencies: " + dependencyStatus);
            }
        });
    }

    private void InitializeFirebase()
    {
        _remoteConfig.SetDefaultsAsync(Parameters.GetDefaults())
          .ContinueWithOnMainThread(task => {
              _isFirebaseInitialized = true;
          });
    }



    public Task FetchDataAsync()
    {
        //12 hours is good for production, use zero to get firebase changes constantly
        System.Threading.Tasks.Task fetchTask =
        Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.FetchAsync(
            TimeSpan.FromHours(12));
        return fetchTask.ContinueWithOnMainThread(FetchComplete);
    }

    void FetchComplete(Task fetchTask)
    {
        if (fetchTask.IsCanceled)
        {
        }
        else if (fetchTask.IsFaulted)
        {
        }
        else if (fetchTask.IsCompleted)
        {
        }
        OnRemoteDataFetched?.Invoke();
        _isRemoteDataFetched = true;

        var info = Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.Info;
        switch (info.LastFetchStatus)
        {
            case Firebase.RemoteConfig.LastFetchStatus.Success:
                Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.ActivateAsync()
                .ContinueWithOnMainThread(task => {
                });

                break;
            case Firebase.RemoteConfig.LastFetchStatus.Failure:
                switch (info.LastFetchFailureReason)
                {
                    case Firebase.RemoteConfig.FetchFailureReason.Error:
                        break;
                    case Firebase.RemoteConfig.FetchFailureReason.Throttled:
                        break;
                }
                break;
            case Firebase.RemoteConfig.LastFetchStatus.Pending:
                break;
        }
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

}
