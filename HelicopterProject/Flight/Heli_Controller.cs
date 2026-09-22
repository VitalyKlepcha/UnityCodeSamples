using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BaseHeli_Input))]

public class Heli_Controller : Base_RBController
{
    #region Variables
    protected Heli_Input input;

    public Heli_Engine engine;

    public HeliRotor_Controller rotorController;

    private Heli_Characteristics characteristics;

    private Heli_Trajectory_Controller trajectoryController;

    private Heli_Animation_Controller animationController;

    private HeliAudio_Controller audioController;

    private Heli_Blinking_Contoller blinkingController;

    private HeliAudioScore_Controller scoreController;

    protected float finalRPM;

    protected float finalPower;

    protected float finalTailRPM;

    private float fingerSwipeTime = -1;

    public float rpmChangeValue = 1f;

    private bool heliFreezed = false;
    #endregion

    #region BuiltInMethods
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        input = GetComponent<Heli_Input>();
        trajectoryController = GetComponent<Heli_Trajectory_Controller>();
        characteristics = GetComponent<Heli_Characteristics>();
        animationController = transform.GetChild(1).GetComponent <Heli_Animation_Controller>();
        audioController = GetComponent <HeliAudio_Controller>();
        blinkingController = GetComponent<Heli_Blinking_Contoller>();
        scoreController = GetComponent<HeliAudioScore_Controller>();
        cog = gameObject.transform.GetChild(0);
    }
    #endregion

    #region Methods
    protected override void HandleHelicopterComponents()
    {
        HandleEngine();
        HandleRotors();
        HandleAnimation();
        HandleAudio();
        HandleBlinking();
        HandleScore();
        //turn off common fly if trajectory fly expected
        CalculateFingerSwipeTime();
         if (TrajectoryManager.fingerSwipePassed == true && (TrajectoryManager.lastTrajectoryCheckpoints.name == "CheckpointsTriangle"))
        {
            HandleTrajectory();
            HandleCharacteristics(true);
        }
        else if (TrajectoryManager.fingerSwipePassed == true)
        {
            HandleTrajectory();
        }
        else if (heliFreezed == true)
        {
            HandleCharacteristics(true);
        }
        else
        {
            HandleCharacteristics(false);
        }
    }

    private void HandleScore()
    {
        if (scoreController)
        {
            scoreController.HandleScore();
        }
    }

    private void HandleBlinking()
    {
        if (blinkingController)
        {
            blinkingController.HandleBlinkEffect();
        }
    }

    private void HandleAudio()
    {
        if (audioController)
        {
            audioController.HandleAudio(input,engine);
        }
    }

    private void HandleAnimation()
    {
        if (animationController)
        {
            animationController.HandleAnimation(input,rb,engine);
            animationController.HandleRotation(input,engine);
        }
    }

    private void CalculateFingerSwipeTime()
    {
        if (TrajectoryManager.fingerSwipePassed == true && fingerSwipeTime == -1)
        {
            fingerSwipeTime = Time.time;
        }
        else if (TrajectoryManager.fingerSwipePassed == false)
        {
            fingerSwipeTime = -1;
        }
    }

    private void HandleTrajectory()
    {
        trajectoryController.TrajectoryFly(rb);
    }

    private void HandleRotors()
    {
        if (rotorController)
        {
            
            rotorController.UpdateRotors(input,finalRPM, finalTailRPM);
        }
    }

    private void HandleCharacteristics(bool autoLevelOnly)
    {

        if (characteristics)
        {
            characteristics.UpdateCharacteristics(rb, input,engine, autoLevelOnly);
        }
    }

    private void HandleEngine()
    {
        engine.EngineUpdate(input.StickyThrottleInput,input.TailRotorInput);
        finalPower = engine.CurHP;
        finalRPM = engine.CurRPM;
        finalTailRPM = engine.CurTailRPM;
    }
    #endregion

    #region RPM Methods
    public void DecreaseMaxRPM(float value)
    {
        engine.SeekRPM -= rpmChangeValue + value;
        engine.SeekRPM = engine.SeekRPM < 0 ? 0 : engine.SeekRPM;
    }

    public void IncreaseMaxRPM(float value)
    {
        engine.SeekRPM += rpmChangeValue + value;
        engine.SeekRPM = engine.SeekRPM > 1400 ? 1400 : engine.SeekRPM;
    }

    public void DecreaseStartMaxRPM(float value)
    {
        engine.StartMaxRPM -= rpmChangeValue + value;
        engine.StartMaxRPM = engine.StartMaxRPM < 0 ? 0 : engine.StartMaxRPM;
    }

    public void IncreaseStartMaxRPM(float value)
    {
        engine.StartMaxRPM += rpmChangeValue + value;
        engine.StartMaxRPM = engine.StartMaxRPM > 1400 ? 1400 : engine.StartMaxRPM;
    }

    public void FreezeHelicopter()
    {
        heliFreezed = !heliFreezed;
    }
    #endregion
}
