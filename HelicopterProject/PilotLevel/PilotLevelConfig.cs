
public class PilotLevelConfig
{
    //Mutual score divided by mutual score divider determines amount of xp given
    public float MutualScoreDivider;

    public int TrickScoreDivider;

    public int InitialXpToLevelUp;

    //Each new level requires {_xpToLevelUpMultiplier} times more xp then previous
    public float XpToLevelUpMultiplier;

    public bool ShowLevelUpPopUp;

    public bool DecreaseLevel;
}
