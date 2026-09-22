using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]

public class Base_RBController : MonoBehaviour
{
    #region Variables
    protected Rigidbody rb;
    
    protected Transform cog;

    
    #endregion
   

    // Update is called once per frame
    void FixedUpdate()
    {
        HandleHelicopterComponents();
       
    }

    protected virtual void HandleHelicopterComponents()
    {

    }
}
