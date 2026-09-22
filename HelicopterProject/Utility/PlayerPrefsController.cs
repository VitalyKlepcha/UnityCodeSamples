using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPrefsController
{
    private static int _splashHeliNumber = -1;

    public static int SplashHeliNumber{
        get {
            if (_splashHeliNumber < 0) {
                if (PlayerPrefs.HasKey("SplashHeli")) {
                    _splashHeliNumber = PlayerPrefs.GetInt("SplashHeli", 0);
                } else {
                    SplashHeliNumber = 0;
                }
            }
            return  _splashHeliNumber;
        }
        set {
            if(value > 5 ) { value = 0; } 
            _splashHeliNumber = value;
            PlayerPrefs.SetInt("SplashHeli", _splashHeliNumber);
        }
    }

    public static bool IsEducationPassed{
        get { return stringToBool(PlayerPrefs.GetString("EducationPassed", "false")); }
        set { PlayerPrefs.SetString("EducationPassed", boolToString(value)); }
    }

    public static bool IsFinalAudioListened_Cargo{
        get { return stringToBool(PlayerPrefs.GetString("IsFinalAudioListened_Cargo", "false")); }
        set { PlayerPrefs.SetString("IsFinalAudioListened_Cargo", boolToString(value)); }
    }

    public static bool IsFinalAudioListened_Police{
        get { return stringToBool(PlayerPrefs.GetString("IsFinalAudioListened_Police", "false")); }
        set { PlayerPrefs.SetString("IsFinalAudioListened_Police", boolToString(value)); }
    }

    public static bool IsFinalAudioListened_Medic{
        get { return stringToBool(PlayerPrefs.GetString("IsFinalAudioListened_Medic", "false")); }
        set { PlayerPrefs.SetString("IsFinalAudioListened_Medic", boolToString(value)); }
    }

    public static bool IsFinalAudioListened_Fire{
        get { return stringToBool(PlayerPrefs.GetString("IsFinalAudioListened_Fire", "false")); }
        set { PlayerPrefs.SetString("IsFinalAudioListened_Fire", boolToString(value)); }
    }

    public static bool IsFinalAudioListened_Military {
        get { return stringToBool(PlayerPrefs.GetString("IsFinalAudioListened_Military", "false")); }
        set { PlayerPrefs.SetString("IsFinalAudioListened_Military", boolToString(value)); }
    }

    public static bool IsFinalAudioListened_Safari {
        get { return stringToBool(PlayerPrefs.GetString("IsFinalAudioListened_Safari", "false")); }
        set { PlayerPrefs.SetString("IsFinalAudioListened_Safari", boolToString(value)); }
    }

    public static bool IsBought_Medic {
        get { return stringToBool(PlayerPrefs.GetString("IsBought_Medic", "false")); }
        set { PlayerPrefs.SetString("IsBought_Medic", boolToString(value)); }
    }
    public static bool IsBought_Cargo {
        get { return stringToBool(PlayerPrefs.GetString("IsBought_Cargo", "false")); }
        set { PlayerPrefs.SetString("IsBought_Cargo", boolToString(value)); }
    }
    public static bool IsBought_Military {
        get { return stringToBool(PlayerPrefs.GetString("IsBought_Military", "false")); }
        set { PlayerPrefs.SetString("IsBought_Military", boolToString(value)); }
    }
    public static bool IsBought_Safari {
        get { return stringToBool(PlayerPrefs.GetString("IsBought_Safari", "false")); }
        set { PlayerPrefs.SetString("IsBought_Safari", boolToString(value)); }
    }
    public static bool IsBought_Fire {
        get { return stringToBool(PlayerPrefs.GetString("IsBought_Fire", "false")); }
        set { PlayerPrefs.SetString("IsBought_Fire", boolToString(value)); }
    }
    public static bool IsBought_Police {
        get { return stringToBool(PlayerPrefs.GetString("IsBought_Police", "false")); }
        set { PlayerPrefs.SetString("IsBought_Police", boolToString(value)); }
    }
    public static bool IsBought_PackHeli {
        get { return stringToBool(PlayerPrefs.GetString("IsBought_PackHeli", "false")); }
        set { PlayerPrefs.SetString("IsBought_PackHeli", boolToString(value)); }
    }

    public static bool IsQrReaded_Medic {
        get { return stringToBool(PlayerPrefs.GetString("IsQrReaded_Medic", "false")); }
        set { PlayerPrefs.SetString("IsQrReaded_Medic", boolToString(value)); }
    }
    public static bool IsQrReaded_Military {
        get { return stringToBool(PlayerPrefs.GetString("IsQrReaded_Military", "false")); }
        set { PlayerPrefs.SetString("IsQrReaded_Military", boolToString(value)); }
    }
    public static bool IsQrReaded_Safari {
        get { return stringToBool(PlayerPrefs.GetString("IsQrReaded_Safari", "false")); }
        set { PlayerPrefs.SetString("IsQrReaded_Safari", boolToString(value)); }
    }
    public static bool IsQrReaded_Cargo {
        get { return stringToBool(PlayerPrefs.GetString("IsQrReaded_Cargo", "false")); }
        set { PlayerPrefs.SetString("IsQrReaded_Cargo", boolToString(value)); }
    }
    public static bool IsQrReaded_Police {
        get { return stringToBool(PlayerPrefs.GetString("IsQrReaded_Police", "false")); }
        set { PlayerPrefs.SetString("IsQrReaded_Police", boolToString(value)); }
    }
    public static bool IsQrReaded_Fire {
        get { return stringToBool(PlayerPrefs.GetString("IsQrReaded_Fire", "false")); }
        set { PlayerPrefs.SetString("IsQrReaded_Fire", boolToString(value)); }
    }

    public static bool IsAssemblySeen_Medic {
        get { return stringToBool(PlayerPrefs.GetString("IsAssemblySeen_Medic", "false")); }
        set { PlayerPrefs.SetString("IsAssemblySeen_Medic", boolToString(value)); }
    }
    public static bool IsAssemblySeen_Fire {
        get { return stringToBool(PlayerPrefs.GetString("IsAssemblySeen_Fire", "false")); }
        set { PlayerPrefs.SetString("IsAssemblySeen_Fire", boolToString(value)); }
    }
    public static bool IsAssemblySeen_Cargo {
        get { return stringToBool(PlayerPrefs.GetString("IsAssemblySeen_Cargo", "false")); }
        set { PlayerPrefs.SetString("IsAssemblySeen_Cargo", boolToString(value)); }
    }
    public static bool IsAssemblySeen_Police {
        get { return stringToBool(PlayerPrefs.GetString("IsAssemblySeen_Police", "false")); }
        set { PlayerPrefs.SetString("IsAssemblySeen_Police", boolToString(value)); }
    }
    public static bool IsAssemblySeen_Safari {
        get { return stringToBool(PlayerPrefs.GetString("IsAssemblySeen_Safari", "false")); }
        set { PlayerPrefs.SetString("IsAssemblySeen_Safari", boolToString(value)); }
    }
    public static bool IsAssemblySeen_Military {
        get { return stringToBool(PlayerPrefs.GetString("IsAssemblySeen_Military", "false")); }
        set { PlayerPrefs.SetString("IsAssemblySeen_Military", boolToString(value)); }
    }
    public static bool IsAssemblySeen_Shmel {
        get { return stringToBool(PlayerPrefs.GetString("IsAssemblySeen_Shmel", "false")); }
        set { PlayerPrefs.SetString("IsAssemblySeen_Shmel", boolToString(value)); }
    }
    public static bool IsAssemblySeen_Grom {
        get { return stringToBool(PlayerPrefs.GetString("IsAssemblySeen_Grom", "false")); }
        set { PlayerPrefs.SetString("IsAssemblySeen_Grom", boolToString(value)); }
    }

    public static bool ScanSeen
    {
        get { return Convert.ToBoolean(PlayerPrefs.GetInt("HelicopterScanned", 0)); }
        set { PlayerPrefs.SetInt("HelicopterScanned", Convert.ToInt32(value)); }
    }

    public static float FlightTime {
        get { return PlayerPrefs.GetFloat("FlightTime", 0); }
        set { PlayerPrefs.SetFloat("FlightTime", value); }
    }

    public static int CollectionItem_MutualScore
    {
        get { return PlayerPrefs.GetInt("CollectionItem_MutualScore", 0); }
        set { PlayerPrefs.SetInt("CollectionItem_MutualScore", value); }
    }

    public static int PilotLevel
    {
        get { return PlayerPrefs.GetInt("PilotLevel", 1); }
        set { PlayerPrefs.SetInt("PilotLevel", value); }
    }

    public static void SetCollectionItemScore(CollectionItemType type, int score)
    {
        PlayerPrefs.SetInt(string.Format("CollectionItem_{0}", type.ToString()), score);
    }

    public static int GetCollectionItemScore(CollectionItemType type)
    {
        return PlayerPrefs.GetInt(string.Format("CollectionItem_{0}", type.ToString()), 0);
    }

    public static void SetTrickStar(string trickName, int starNum)
    {
        PlayerPrefs.SetInt(string.Format("{0}_stars", trickName), starNum);
    }

    public static int GetTrickStar(string trickName)
    {
        return PlayerPrefs.GetInt(string.Format("{0}_stars", trickName), 0);
    }

    public static void SetTrickScore(string trickName, int score)
    {
        PlayerPrefs.SetInt(string.Format("{0}_score", trickName), score);
    }

    public static int GetTrickScore(string trickName)
    {
        return PlayerPrefs.GetInt(string.Format("{0}_score", trickName), 0);
    }

    private static int boolToInt(bool val) {
        if (val) return 1;
        else return 0;
    }

    private static bool intToBool(int val) {
        return (val != 0);
    }

    private static string boolToString(bool val){
        if (val) return "true";
        else return "false";
    }

    private static bool stringToBool(string val) {
       return val == "true";
    }
}