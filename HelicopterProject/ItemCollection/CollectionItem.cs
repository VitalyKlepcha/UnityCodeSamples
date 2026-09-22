using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Audio;

[RequireComponent(typeof(Collider))]
public class CollectionItem : MonoBehaviour
{
    #region Variables

    [SerializeField]
    private int _scorePoint;

    private bool _isLongLiving;

    [SerializeField]
    private CollectionItemType _itemType;

    //Chance of spawning this object, 1 - very low / 10 - very high
    [Range(1,10)]
    [SerializeField]
    private int uniqueness = 10;

    //Controlled by generation service
    private float _lifeTime = 30f;

    [SerializeField]
    private AudioSource _audioSource;

    public event Action<GameObject> OnItemCollected;

    public event Action<GameObject> OnItemLifeTimePassed;

    private bool _lifeTimePassed;
    #endregion

    #region Properties

    public bool IsLongLiving { get => _isLongLiving; set => _isLongLiving = value; }
    public int ScorePoint { get => _scorePoint; }
    public CollectionItemType Type { get => _itemType; }
    public float LifeTime { get => _lifeTime; set => _lifeTime = value; }
    public int Uniqueness { get => uniqueness; }
    public AudioSource Audio { get => _audioSource; set => _audioSource = value; }

    #endregion

    #region Methods
    private void Update()
    {
        _lifeTime -= Time.deltaTime;
        if(!_lifeTimePassed && _lifeTime <= 0)
        {
            OnItemLifeTimePassed?.Invoke(gameObject);
            _lifeTimePassed = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("CollectionItem: OnTriggerEnter with " + other.gameObject.name);
        if(other.tag == "Helicopter" && IsVisibleOnScreen())
        {
            OnItemCollected?.Invoke(gameObject);
        }
    }

    private bool IsVisibleOnScreen()
    {
        // 1. Get the item's position on screen
        Vector3 screenPos = Camera.main.WorldToViewportPoint(transform.position);

        // 2. Check if it's within camera view (0-1 range means visible)
        return (screenPos.x > 0 && screenPos.x < 1 &&
                screenPos.y > 0 && screenPos.y < 1 &&
                screenPos.z > 0); // Z > 0 means in front of camera
    }
    #endregion
}

public enum CollectionItemType
{
    Star,
    Bad,
    Gem
}