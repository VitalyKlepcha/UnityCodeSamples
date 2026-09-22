using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class Heli_Input : BaseHeli_Input 
{

    #region Variables
    private float throttleInput = 0f;
    private Vector2 cyclicInput = Vector2.zero;
    private float collectiveInput = 0f;
    private float pedalsInput = 0f;
    private float stickyThrottleInput = 0f;
    private float stickyCollectiveInput = 0f;
    private float tailRotorInput = 0f;
    //Change gyro init angles variables
    private Transform helicopter;
    private Vector3 prevHeliPos;
    public event Action OnMainRotorEnabled;
    public event Action OnTailRotorEnabled;
    public event Action OnTailRotorAvailable;
    private bool tailRotorAvailableCalled = false;
    private bool tailRotorEnabled = false;

    #region Properties
    public float PedalsInput { get => pedalsInput;  }
    public float ThrottleInput { get => throttleInput;  }
    public Vector2 CyclicInput { get => cyclicInput; set => cyclicInput = value; }
    
    public float StickyThrottleInput { get => stickyThrottleInput;  }
    public float StickyCollectiveInput { get => stickyCollectiveInput; }
    public float CollectiveInput { get => collectiveInput; set => collectiveInput = value; }
    public float TailRotorInput { get => tailRotorInput; }

    #endregion

    [HideInInspector]
    public bool riseAndRotateFlag = false;
    [HideInInspector]
    public float riseAndRotateTime;
    #endregion


    #region BuiltInMethods
    private void Start()
    {
        helicopter = GetComponent<Transform>();
        prevHeliPos = helicopter.transform.position;

    }

    #endregion
    protected override void HandleInput()
    {
        base.HandleInput();
        CheckTailRotorAvailability();
        HandleStickyCollective();
        
    }

    private void CheckTailRotorAvailability()
    {
        if(riseAndRotateFlag == true && Time.time - riseAndRotateTime > 3f && !tailRotorAvailableCalled)
        {
            tailRotorAvailableCalled = true;
            OnTailRotorAvailable?.Invoke();
        }
    }

    #region Mobile_Input
#if UNITY_ANDROID || UNITY_IOS

    private void HandleStickyCollective()
    {
        stickyCollectiveInput += collectiveInput * Time.deltaTime;
        stickyCollectiveInput = Mathf.Clamp01(stickyCollectiveInput);
        
    }

    public void HandleStickyThrottle()
    {
        if (StickyThrottleInput == 0)
        {
            OnMainRotorEnabled?.Invoke();
            stickyThrottleInput = stickyThrottleInput == 0 ? 1 : 0;
        }
        else if (TailRotorInput == 0 && StickyThrottleInput == 1 && riseAndRotateFlag == true && Time.time - riseAndRotateTime > 3f && !AudioManager.GetSource("Speaker2").isPlaying)
        {
            HandleTailRotor();
        }
    }

    public void HandleTailRotor()
    {
        if (!tailRotorEnabled)
        {
            tailRotorEnabled = true;
            OnTailRotorEnabled?.Invoke();
            tailRotorInput = tailRotorInput == 0 ? 1 : 0;
        }
    }
    
    public void EnableTailRotor()
    {
        tailRotorInput = 1;
    }

    
#endif
    #endregion
}

public enum GyroDir
{
    Right,
    Forward,
    Up,
    All
}