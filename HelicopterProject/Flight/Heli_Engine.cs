using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Heli_Engine : MonoBehaviour
{
    #region Variables
    public float maxHP = 100f;
    [SerializeField]
    private float maxRPM = 700;

    private float startMaxRPM = 0f;

    private float standartRPM = 700f;

    public float engineDelay = 1f;

    private float minRPM = 100f;

    public event Action OnStartMaxRPMAchieved;

    public const float MAX_RPM = 1400f; 

    private bool startMaxRPMAchieved = false;
    #endregion

    #region Properties
    private float curHP;
    private float curRPM;
    private float curTailRPM;
    public AnimationCurve powerCurve = AnimationCurve.EaseInOut(0f,0f,1f,1f);

    public float CurHP { get => curHP;  }
    public float CurRPM { get => curRPM; }
    public float SeekRPM { get => maxRPM; set => maxRPM = value; }
    public float CurTailRPM { get => curTailRPM; }
    public float StandartRPM { get => standartRPM;  }
    public float MinRPM { get => minRPM; }
    public float StartMaxRPM { get => startMaxRPM; set => startMaxRPM = value; }
    public bool StartMaxRPMAchieved { get => startMaxRPMAchieved; set => startMaxRPMAchieved = value; }
    #endregion

    public void EngineUpdate(float throttleInput,float tailInput)
    {
        float maxRPMbased;
        maxRPMbased = startMaxRPMAchieved ? maxRPM : startMaxRPM;
        if(startMaxRPM >= 400 && !startMaxRPMAchieved)
        {
            OnStartMaxRPMAchieved?.Invoke();
            startMaxRPMAchieved = true;
        }
        float wantedHP = powerCurve.Evaluate(throttleInput) * maxHP;
        float wantedRPM = throttleInput * maxRPMbased;
        curHP = Mathf.Lerp(curHP, wantedHP, Time.deltaTime*engineDelay);
        curRPM = Mathf.Lerp(curRPM, wantedRPM, Time.deltaTime*engineDelay);
        float wantedTailRPM = tailInput * maxRPMbased;
        curTailRPM = Mathf.Lerp(curTailRPM, wantedTailRPM, Time.deltaTime * engineDelay);
    }

    public void InvokeOnStartMaxRPMAchievedEvent()
    {
        OnStartMaxRPMAchieved?.Invoke();
    }

    
}
