using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using System.Linq;

public class LevelManager : IlevelManager
{
    private ILocalDatabase _localDatabase;
    private IRemoteConfig _remoteConfig;
    private IRemoteDatabase _remoteDatabase;

    private int _currentLevel;

    LevelManager(ILocalDatabase localDatabase, IRemoteConfig remoteConfig, IRemoteDatabase remoteDatabase)
    {
        _localDatabase = localDatabase;
        _remoteConfig = remoteConfig;
        _remoteDatabase = remoteDatabase;
    }

    public Task<LevelData> GetLevel(int number) => Task.Run(async () => await _localDatabase.GetLevelData(await ClampLevel(number)));

    public Task<LevelData> GetNextLevel(int number) => Task.Run(async () => await _localDatabase.GetNextLevel(await ClampLevel(number)));

    public Task<int> GetFinishedLevel() => Task.Run(async () => await _localDatabase.GetFinishedLevel());

    public Task<int> GetLevelCount() => Task.Run(async () => await _localDatabase.GetLevelCount());

    public Task<int> GetRemoteLevelCount() => Task.Run(async () => await _remoteDatabase.GetRemoteLevelCount());

    public Task<int> GetLevelStars(int number) => Task.Run(async () => await IsLevelExist(number) ? await _localDatabase.GetLevelStars(await ClampLevel(number)) : 0);

    public Task<int> GetTotalStars() => Task.Run(async () => await _localDatabase.GetTotalStars());

    public Task<int> GetCurrentLevel() => Task.Run(async () =>
    {
        if (!await IsLevelExist(_currentLevel))
            _currentLevel = await ClampLevel(await _localDatabase.GetFinishedLevel());
        return _currentLevel;
    });

    public async Task SetCurrentLevel(int number)
    {
        if (!await IsLevelExist(number))
            _currentLevel = await ClampLevel(number);
        else
            _currentLevel = number;
    }

    private Task<int> ClampLevel(int number)
    {
        return Task.Run(async () => Mathf.Clamp(number, 1,await  _localDatabase.GetLevelCount()));
    }

    private async Task<bool> IsLevelExist(int number)
    {
        if (number < 1 || number > await _localDatabase.GetLevelCount())
            return false;
        else
            return true;
    }

    public void FinishLevel(int stars)
    {
        if (stars == 0) return;
        _localDatabase.SetFinishedLevel(_currentLevel);
        _localDatabase.UpdateLevelStars(_currentLevel, stars);
    }

    public Task<bool> IsNeedLoadLevels() => Task.Run(async () =>
    {
        var localLevelsNumber = await _localDatabase.GetLevelCount();

        if (localLevelsNumber == 0)
            return true;

        var leftBeforeLoadNumber = _remoteConfig.GetClass<LevelsLoadingConfig>(Parameter.LevelsLoadingConfig).leftBeforeLoadNumber;
        var nextLevelNumber = await _localDatabase.GetFinishedLevel();
        if (localLevelsNumber - nextLevelNumber <= leftBeforeLoadNumber)
            return true;

        return false;
    });

    public UniTask UpdateLevels(bool isRefresh = false, int? currentLevelNumber = null, CancellationToken cancellationToken = default) => UniTask.RunOnThreadPool(async () =>
    {
    var loadingConfig = _remoteConfig.GetClass<LevelsLoadingConfig>(Parameter.LevelsLoadingConfig);
    cancellationToken.ThrowIfCancellationRequested();
        var localLevelsNumber = await _localDatabase.GetLevelCount();
    cancellationToken.ThrowIfCancellationRequested();
    // If there's no saved levels, load first level from Firebase
    if (localLevelsNumber == 0)
    {
        await UpdateLevelsData(1, 1, loadingConfig);
        return;
    }

        // If all levels are completed or there's less than n levels left, load new levels
        var nextLevelNumber = (currentLevelNumber ?? (await _localDatabase.GetFinishedLevel()) + 1);
        var isUpdatePrevious = loadingConfig.isUpdatePrevious;
        cancellationToken.ThrowIfCancellationRequested();
        if (localLevelsNumber - nextLevelNumber <= loadingConfig.leftBeforeLoadNumber)
        {
            var fromLevelNumber = (isUpdatePrevious ? 0 : localLevelsNumber) + 1;
            var levelsNumber = (isUpdatePrevious ? localLevelsNumber : 0) + loadingConfig.loadNumber;

            await UpdateLevelsData(fromLevelNumber, levelsNumber, loadingConfig);
            return;
        }
    });

    private async Task UpdateLevelsData(int fromLevelNumber, int levelsNumber, LevelsLoadingConfig config, CancellationToken cancellationToken = default)
    {
        Log.Debug(string.Format("UpdateLevelsData from level {0}, levelNumber{1}",fromLevelNumber, levelsNumber));

        var levelsLoading = UpdateLevels(fromLevelNumber, levelsNumber, config, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();
        await Task.WhenAll(levelsLoading);
        cancellationToken.ThrowIfCancellationRequested();
    }

    private async Task UpdateLevels(int fromLevelNumber, int levelsNumber, LevelsLoadingConfig config, CancellationToken cancellationToken = default)
    {
        var remoteLevels = await LoadRemoteLevels(fromLevelNumber, levelsNumber, config);
        cancellationToken.ThrowIfCancellationRequested();
        Log.Debug("Remote levels count = " + remoteLevels.Count());
        var levelTasks = remoteLevels.Select(l => AddOrUpdateLoadedLevel(l, cancellationToken));
        await Task.WhenAll(levelTasks);
        Log.Debug("Ended updating levels from remote database");
    }

    private async Task<LevelData[]> LoadRemoteLevels(int fromLevelNumber, int levelsNumber, LevelsLoadingConfig config)
    {
        if (config.isLoadAllRemote)
        {
            var remoteLevels = (await _remoteDatabase.GetLevels()).AsEnumerable();

            if (!config.isUpdateNext)
                remoteLevels = remoteLevels.Where(l => l.Num <= fromLevelNumber + levelsNumber - 1);

            if (!config.isUpdatePrevious)
                remoteLevels = remoteLevels.Where(l => l.Num >= fromLevelNumber);

            return remoteLevels.ToArray();
        }
        else
        {
            return (await _remoteDatabase.GetNextLevels(fromLevelNumber, levelsNumber)).ToArray();
        }
    }

    private async Task AddOrUpdateLoadedLevel(LevelData level, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var sameNumberLevel = await _localDatabase.GetLevelData(level.Num);
        cancellationToken.ThrowIfCancellationRequested();
        if (sameNumberLevel != null)
            await _localDatabase.DeleteLevelData(sameNumberLevel);

        cancellationToken.ThrowIfCancellationRequested();
        await _localDatabase.UpdateLevelData(level);
        cancellationToken.ThrowIfCancellationRequested();
    }

    public async UniTask WaitForInitialization() => await _localDatabase.WaitForInitialization();
}
