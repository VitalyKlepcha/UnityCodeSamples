using Firebase.Database;
using Firebase.Extensions;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;
using System;
using System.Threading;
using System.Linq;
using Cysharp.Threading.Tasks;
using System.Runtime.InteropServices;

public class FirebaseRemoteDatabase : IRemoteDatabase
{

    private const int DEFAULT_LEVEL_COUNT = 20;

    int _levelsCount = DEFAULT_LEVEL_COUNT;
    bool _isLevelCountUpdated;
    bool _isLevelCountUpdateFaulted;


    public FirebaseRemoteDatabase()
    {

    }

    #region Level count

    public async UniTask UpdateRemoteLevelCount(int value)
    {
        _levelsCount += value;
        await FirebaseDatabase.DefaultInstance.GetReference("levelCount").SetValueAsync(_levelsCount);
    }

    public async UniTask UpdateLevelCountFromRemote()
    {
        DataSnapshot snapshot = null;
        Log.Debug("Start get level count remote");
        await FirebaseDatabase.DefaultInstance.GetReference("levelCount").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                _isLevelCountUpdateFaulted = true;
                Debug.LogError("Level count wasn't loaded");
            }
            else if (task.IsCompleted)
            {
                snapshot = task.Result;
                _isLevelCountUpdated = true;
            }
        });
        if (snapshot != null)
        {
            if (int.TryParse(snapshot.GetRawJsonValue(), out int result))
            {
                _levelsCount = result;
            }
        }
    }

    public async Task<int> GetRemoteLevelCount()
    {
        if(_isLevelCountUpdated)
            return _levelsCount;
        else
        {
            if (_isLevelCountUpdateFaulted)
            {
                UpdateLevelCountFromRemote().Forget();
                _isLevelCountUpdateFaulted = false;
            }
            try
            {
                await UniTask.WaitUntil(() => _isLevelCountUpdated == true).Timeout(TimeSpan.FromSeconds(2));
            }
            catch(TimeoutException)
            {
                Log.Debug("Timeout when getting remote level count");
                return DEFAULT_LEVEL_COUNT;
            }
            return _levelsCount;
        }
    }
    #endregion
    public async Task<bool> HasLevel(int levelNum)
    {
        DataSnapshot snapshot = null;
        bool levelExist = false;
        await FirebaseDatabase.DefaultInstance.GetReference("levels").Child("level " + levelNum).GetValueAsync().ContinueWithOnMainThread(task =>
        {

            if (task.IsCompleted)
            {
                snapshot = task.Result;
                levelExist = snapshot.Exists;
            }
        });
        return levelExist;
    }

    public async Task<LevelData> LoadLevel(int levelNum)
    {
        DataSnapshot snapshot = null;
        await FirebaseDatabase.DefaultInstance.GetReference("levels").Child("level " + levelNum).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Level wasn't loaded");
            }
            else if (task.IsCompleted)
            {
                Debug.Log("Level loaded successfully");
                snapshot = task.Result;
            }
        });
        if (snapshot == null)
            return null;
        var firebaseLevel = JsonConvert.DeserializeObject<FirebaseLevelData>(snapshot.GetRawJsonValue());
        var ld = await firebaseLevel.ToCoreLevel();
        return ld;
        }

    public async Task SaveLevel(LevelData ld)
    {
        var firebaseLevel = ld.ToFirebaseLevel();
        string jsonLevel = JsonConvert.SerializeObject(firebaseLevel, Formatting.None,
                        new JsonSerializerSettings()
                        {
                            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                        });
        await FirebaseDatabase.DefaultInstance.GetReference("levels").Child("level " + firebaseLevel.Num.ToString()).SetRawJsonValueAsync(jsonLevel);
    }

    public async Task<LevelData[]> GetLevels() => await GetObject(
        () => FirebaseDatabase.DefaultInstance.GetReference("levels").OrderByChild("Num").GetValueAsync(),

         async(result) =>
        {

            var levels = await result.Children.Select(async data =>
            {
                var firebaseLevel = JsonConvert.DeserializeObject<FirebaseLevelData>(data.GetRawJsonValue());
                return await firebaseLevel.ToCoreLevel();
            }).ToArray();

            return levels;
        },

        Array.Empty<LevelData>());

    public async Task<LevelData[]> GetNextLevels(int nextLevelNumber, int levelsCount) => await GetObject(
        async () => {
            Log.Debug("Start getting levels from firebase");
            await FirebaseDatabase.DefaultInstance.GetReference("levels").OrderByChild("Num").StartAt(nextLevelNumber).LimitToFirst(levelsCount).GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Log.Error("Level wasn't loaded");
                }
                else if (task.IsCompleted)
                {
                    snapshot = task.Result;
                }
            });
            Log.Debug("Done getting levels from firebase");
            return snapshot;
        },

        async(result) =>
        {
            var levels = await result.Children.Select(async data =>
            {
                var firebaseLevel = JsonConvert.DeserializeObject<FirebaseLevelData>(data.GetRawJsonValue());
                return await firebaseLevel.ToCoreLevel();
            }).ToArray();

            return levels;
        },

        Array.Empty<LevelData>());

    private async Task<T> GetObject<T>(Func<Task<DataSnapshot>> firebaseAction, Func<DataSnapshot, Task<T>> parsingAction,
    T defaultValue = default)
    {
        var isOnBackground = Thread.CurrentThread.IsBackground;
        if (isOnBackground)
            await UniTask.SwitchToMainThread();

        var result = await firebaseAction.Invoke();
        if (result == null || !result.Exists)
            return defaultValue;
        isOnBackground = Thread.CurrentThread.IsBackground;
        if (!isOnBackground)
            await UniTask.SwitchToThreadPool();
        Log.Debug("Start level parsing");
        return await parsingAction.Invoke(result);
    }

}

