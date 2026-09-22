using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

public class LevelData
{
    public string Uuid;

    public int Num;

    public List<LevelBonusData> Bonuses = new List<LevelBonusData>();

    public LevelTerrainData TerrainDetails = new LevelTerrainData();

    public LevelType LevelType;

    public Vector3 StartPlayerPosition = new Vector3(7.4f, 0.15f, 8.25f);

    [Tooltip("Amount of time for LevelType.TimeBound")]
    [HideInInspector]
    public float LevelTime;

    [Tooltip("Grass percent to be mowed in LevelType.SpecificGrass")]
    [HideInInspector]
    public float GrassPercentToWin;

    [Tooltip("Grass color to be mowed in LevelType.SpecificGrass")]
    [HideInInspector]
    public string SpecificColorName;

    public FirebaseLevelData ToFirebaseLevel()
    {
        FirebaseLevelData firebaseLevel = new FirebaseLevelData();
        firebaseLevel.Uuid = Uuid;
        firebaseLevel.Num = Num;
        firebaseLevel.LevelType = LevelType;
        firebaseLevel.SpecificColorName = SpecificColorName;
        firebaseLevel.GrassPercentToWin = GrassPercentToWin;
        firebaseLevel.LevelTime = LevelTime;
        firebaseLevel.StartPlayerPosition = StartPlayerPosition;
        firebaseLevel.TerrainDetails = TerrainDetails.ToFirebaseLevel();
        List<FirebaseLevelBonusData> firebaseBonuses = new List<FirebaseLevelBonusData>();
        foreach(var bonus in Bonuses)
        {
            firebaseBonuses.Add(bonus.ToFirebaseLevelBonus());
        }
        firebaseLevel.Bonuses = firebaseBonuses;
        return firebaseLevel;
    }

    public LocalLevelData ToLocalLevel()
    {
        LocalLevelData localLevel = new LocalLevelData();
        localLevel.Uuid = Uuid;
        localLevel.Num = Num;
        localLevel.Json = JsonConvert.SerializeObject(ToFirebaseLevel(), Formatting.None,
                        new JsonSerializerSettings()
                        {
                            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                        });
        return localLevel;
    }
}
