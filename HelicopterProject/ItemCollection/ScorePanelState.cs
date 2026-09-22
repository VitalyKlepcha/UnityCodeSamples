using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ScorePanelState : MonoBehaviour
{
    #region Variables

    [SerializeField]
    private ScorePanelType _type;

    public GameObject Panel { get => gameObject; }
    public ScorePanelType Type { get => _type; }

    #endregion

    #region Methods

    void Start()
    {

    }


    #endregion
}
