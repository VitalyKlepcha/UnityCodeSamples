using System;

public delegate void ExpChangeHandler(int currentExp, int expToLevelUp);
public interface IPilotLevelService
{
    void CalculateCurrentLevel(bool blockPopUp = false);

    int CurrentLevel { get; }
    int CurrentExp { get; }
    int ExpToLevelUp { get; }

    event Action OnLevelChanged;
    event ExpChangeHandler OnExpChanged;
}
