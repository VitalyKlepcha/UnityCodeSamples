using Cysharp.Threading.Tasks;
using SQLite;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using static UniTaskExt;

public class SQLiteDatabase : ILocalDatabase
{
    private SQLiteAsyncConnection _connection;

    private static SemaphoreSlim _dbLock = new SemaphoreSlim(1, 1);
    private readonly UniTaskCompletionSource<bool> _initCompletionSource = new UniTaskCompletionSource<bool>();

    private bool _isInitialized = false;

#if UNITY_EDITOR
    private const string DATABASE_NAME = "moneycutDebug.db";
#else
    private const string DATABASE_NAME = "moneycutRelease.db";
#endif


    public SQLiteDatabase()
    {


        InitializeDatabaseAsync().Forget();


    }

    private async UniTaskVoid InitializeDatabaseAsync()
    {
        await _dbLock.WaitAsync();
        try
        {
            string dbPath;

#if UNITY_EDITOR
            dbPath = Path.Combine(Application.streamingAssetsPath, DATABASE_NAME);
#else
        dbPath = Path.Combine(Application.persistentDataPath, DATABASE_NAME);
        
        if (!File.Exists(dbPath))
        {
#if UNITY_ANDROID
            await CopyDatabaseAndroid(dbPath);
#elif UNITY_IOS
            var loadDb = Application.dataPath + "/Raw/" + databaseName;  // this is the path to your StreamingAssets in iOS
            // then save to Application.persistentDataPath
            
            File.Copy(loadDb, filepath);
#endif
        }
#endif

            _connection = new SQLiteAsyncConnection(dbPath, SQLiteOpenFlags.ReadWrite |
                SQLiteOpenFlags.Create |
                SQLiteOpenFlags.FullMutex);

            Debug.Log("Database initialized at: " + dbPath);

            // Initialize tables sequentially
            await InitializeGameStatistics();
            await InitializeLevelStatistics();

            _isInitialized = true;
            _initCompletionSource.TrySetResult(true);
        }
        catch (Exception ex)
        {
            _initCompletionSource.TrySetException(ex);
            Debug.LogError(ex);
            throw;
        }
        finally
        {
            _dbLock.Release();
        }
    }

    private async UniTask CopyDatabaseAndroid(string targetPath)
    {
        string sourcePath = Path.Combine(Application.streamingAssetsPath, DATABASE_NAME);

        using (UnityWebRequest loadDb = UnityWebRequest.Get(sourcePath))
        {
            await loadDb.SendWebRequest().ToUniTask(
                progress: null,
                cancellationToken: CancellationToken.None,
                timing: PlayerLoopTiming.Update);

            if (loadDb.result != UnityWebRequest.Result.Success)
            {
                throw new Exception("Database load failed: " + loadDb.error);
            }

            File.WriteAllBytes(targetPath, loadDb.downloadHandler.data);
        }
    }

    private async UniTask InitializeGameStatistics()
    {
        await _connection.CreateTableAsync<GameStatistics>();
        if (await _connection.Table<GameStatistics>().CountAsync() > 0)
            return;
        var gs = new GameStatistics();
        gs.Id = 0;
        gs.TotalStars = 0;
        gs.FinishedLevel = 0;
        await _connection.InsertAsync(gs);
    }

    private async UniTask InitializeLevelStatistics()
    {
        await _connection.CreateTableAsync<LevelStatistics>();
        await _connection.CreateTableAsync<LocalLevelData>();
    }

    public UniTask WaitForInitialization()
    {
        return _initCompletionSource.Task;
    }

    public async UniTask<int> GetFinishedLevel()
    {
        await WaitForInitialization();
        var gs = await _connection.Table<GameStatistics>().FirstOrDefaultAsync();
        return gs.FinishedLevel;
    }

    public async UniTask<int> GetLevelCount()
    {
        await WaitForInitialization();
        return await _connection.Table<LocalLevelData>().CountAsync();
    }

    public async UniTask<int> GetLevelStars(int number)
    {
        await WaitForInitialization();
        var ls =  await _connection.Table<LevelStatistics>().Where(ls => ls.Num == number).FirstOrDefaultAsync();
        if (ls == null)
            return 0;
        return ls.Stars;
    }

    public async UniTask<LevelData> GetNextLevel(int number)
    {
        await WaitForInitialization();
        return await GetLevelData(number + 1);
    }

    public async UniTask<int> GetTotalStars()
    {
        await WaitForInitialization();
        var gs = await _connection.Table<GameStatistics>().FirstOrDefaultAsync();
        return gs.TotalStars;
    }

    public async UniTask SetFinishedLevel(int number)
    {
        await WaitForInitialization();
        var gs = await _connection.Table<GameStatistics>().FirstOrDefaultAsync();
        if(gs.FinishedLevel < number)
            gs.FinishedLevel = number;
        await _connection.InsertOrReplaceAsync(gs);
    }

    public async UniTask UpdateLevelStars(int levelNum, int starNum)
    {
        await WaitForInitialization();
        var ls = await _connection.Table<LevelStatistics>().Where(ls => ls.Num == levelNum).FirstOrDefaultAsync();
        if (ls == null)
        {
            ls = new LevelStatistics();
            ls.Num = levelNum;
        }
        if(ls.Stars < starNum)
            ls.Stars = starNum;
        await _connection.InsertOrReplaceAsync(ls);
    }

    public async UniTask<LevelData> GetLevelData(string uuid)
    {
        await WaitForInitialization();
        var ld = await GetAsync<LocalLevelData>(uuid);
        return await ld.ToCoreLevel();
    }

    public async UniTask UpdateLevelData(LevelData ld)
    {
        await WaitForInitialization();
        await InsertOrReplaceAsync(ld.ToLocalLevel());
    }

    #region Generic methods
    private async UniTask CreateTableAndThen<T>(ActionAsync action) where T : new()
    {
        try
        {
            if (action != null)
                await action.Invoke();
        }
        catch (SQLiteException e)
        {
            if (e.Result != SQLite3.Result.Error)
                return;

            await _connection.CreateTableAsync<T>();
            if (action != null)
                await action.Invoke();
        }
    }

    public UniTask InsertAsync<T>(T value) where T : new() => CreateTableAndThen<T>(async () => await _connection.InsertAsync(value));

    public UniTask InsertOrReplaceAsync<T>(T value) where T : new() => CreateTableAndThen<T>(async () => await _connection.InsertOrReplaceAsync(value));

    public UniTask InsertOrReplaceAllAsync<T>(T[] value) where T : new() => CreateTableAndThen<T>(async () => await _connection.InsertAllAsync(value, "OR REPLACE"));

    public UniTask InsertAllAsync<T>(T[] value) where T : new() => CreateTableAndThen<T>(async () => await _connection.InsertAllAsync(value));

    public UniTask DeleteAsync<T>(object primaryKey) where T : new() => CreateTableAndThen<T>(async () => await _connection.DeleteAsync<T>(primaryKey));
    public UniTask DeleteAllAsync<T>() where T : new() => CreateTableAndThen<T>(async () => await _connection.DeleteAllAsync<T>());

    public async UniTask<T> GetAsync<T>(object primaryKey) where T : new()
    {
        await WaitForInitialization();
        try
        {
            return await _connection.GetAsync<T>(primaryKey);
        }
        catch (Exception e)
        {
            Log.Debug("Exception raised by SqlLite GetAsync method: " + e.Message);
            return default;
        }
    }

    public async UniTask<T[]> GetAllAsync<T>() where T : new()
    {
        await WaitForInitialization();
        try
        {
            return await _connection.Table<T>().ToArrayAsync();
        }
        catch (Exception e)
        {
            Log.Debug("Exception raised by SqlLite GetAllAsync method: " + e.Message);
            return default;
        }
    }

    public async UniTask<LevelData> GetLevelData(int num)
    {
        await WaitForInitialization();
        var ld = await _connection.Table<LocalLevelData>().Where(ld => ld.Num == num).FirstOrDefaultAsync();
        return ld == null ? null : await ld.ToCoreLevel();
    }

    public async UniTask DeleteLevelData(LevelData ld) 
    {
        await WaitForInitialization();
        await _connection.DeleteAsync<LocalLevelData>(ld.Uuid);
    }

    #endregion
}