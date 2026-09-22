using System;
using UnityEngine;

public abstract class BasePopUp : MonoBehaviour
{
    public event Action OnPopUpClosedEvent;
    public virtual void ClosePopUP()
    {
        Destroy(gameObject);
        OnPopUpClosedEvent?.Invoke();
    }
}
