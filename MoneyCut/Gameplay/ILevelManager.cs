using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public interface IlevelManager
{
    public Task<LevelData> GetLevel(int number);

    public Task<LevelData> GetNextLevel(int number);

    public Task<int> GetLevelCount();

    public Task<int> GetFinishedLevel();

    public void FinishLevel(int stars);

    public Task<int> GetCurrentLevel();

    public Task SetCurrentLevel(int number);

    public Task<int> GetLevelStars(int number);

    public Task<int> GetTotalStars();

    public Task<int> GetRemoteLevelCount();

    /// <summary>
    /// Load levels from remote database based on loading config
    /// </summary>
    public UniTask UpdateLevels(bool isRefresh = false, int? currentLevelNumber = null, CancellationToken cancellationToken = default);

    public UniTask WaitForInitialization();
}
