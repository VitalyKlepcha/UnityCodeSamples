using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public interface ILocalDatabase
{
    public UniTask<LevelData> GetNextLevel(int number);

    public UniTask<int> GetFinishedLevel();

    public UniTask SetFinishedLevel(int number);

    public UniTask<int> GetLevelCount();

    public UniTask UpdateLevelStars(int levelNum, int starNum);

    public UniTask<int> GetLevelStars(int number);

    public UniTask<int> GetTotalStars();

    public UniTask<LevelData> GetLevelData(string uuid);

    public UniTask<LevelData> GetLevelData(int num);

    public UniTask UpdateLevelData(LevelData ld);

    public UniTask DeleteLevelData(LevelData ld);

    public UniTask WaitForInitialization();
}