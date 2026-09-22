using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseHeli_Input : MonoBehaviour
{
    #region Variables
    protected float horizontal = 0f;
    protected float vertical = 0f;
    #endregion

    void Update()
    {
        
        HandleInput();
    }

    protected virtual void HandleInput()
    {
       
        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");
        
    }
}
