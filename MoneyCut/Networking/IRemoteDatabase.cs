using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public interface IRemoteDatabase 
{
    public Task SaveLevel(LevelData ld);

    public Task<bool> HasLevel(int levelNum);

    public Task<LevelData> LoadLevel(int levelNum);

    public Task<LevelData[]> GetLevels();

    public Task<LevelData[]> GetNextLevels(int nextLevelNumber, int levelsCount);

    public Task<int> GetRemoteLevelCount();

    public UniTask UpdateLevelCountFromRemote();

    public UniTask UpdateRemoteLevelCount(int value);
}
