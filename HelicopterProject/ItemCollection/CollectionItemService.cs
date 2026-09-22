using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

/// <summary>
/// Main service managing the collection item system.
/// Handles item generation, lifecycle, scoring, and UI integration.
/// Implements AutoSaveBehaviour for persistent data management.
/// </summary>
[RequireComponent(typeof(CollectionItemDirectionService), typeof(CollectionItemUIController), typeof(CollectionItemSpawner))]
public class CollectionItemService : AutoSaveBehaviour
{
    #region Configuration

    /// <summary>
    /// List of item prefabs available for generation.
    /// </summary>
    [SerializeField]
    private List<GameObject> _itemPrefabs;

    /// <summary>
    /// Animator for force generation visual feedback.
    /// </summary>
    [SerializeField]
    private Animator _forceGenerationAnimator;

    /// <summary>
    /// Currently active items in the scene.
    /// </summary>
    private List<GameObject> _items = new List<GameObject>();

    /// <summary>
    /// Service responsible for item generation logic.
    /// </summary>
    private CollectionItemGenerationService _generationService;

    /// <summary>
    /// Timestamp of the last generation call.
    /// </summary>
    private float _generationCalledTime;

    /// <summary>
    /// Number of items to generate during force generation.
    /// </summary>
    private int _forceGenerationItemCount = 3;

    /// <summary>
    /// Interval between automatic item generations in seconds.
    /// </summary>
    [SerializeField]
    private float _generationInterval = 10f;

    /// <summary>
    /// Current count of long-living items.
    /// </summary>
    private int _longLivingItemsCount = 0;

    /// <summary>
    /// Total mutual score across all item types.
    /// </summary>
    private int _mutualScore;

    /// <summary>
    /// Score breakdown by item type.
    /// </summary>
    private Dictionary<CollectionItemType, int> _scores = new Dictionary<CollectionItemType, int>();

    /// <summary>
    /// UI controller for score display.
    /// </summary>
    private CollectionItemUIController _uiController;

    /// <summary>
    /// Service for managing direction indicators.
    /// </summary>
    private CollectionItemDirectionService _directionService;

    /// <summary>
    /// Remote configuration service.
    /// </summary>
    private IRemoteConfig _remoteConfig;
    
    /// <summary>
    /// Pilot level service for progression tracking.
    /// </summary>
    private IPilotLevelService _pilotLevelService;
    #endregion

    #region Initialization

    /// <summary>
    /// Injects dependencies using Zenject.
    /// </summary>
    /// <param name="remoteConfig">Remote configuration service</param>
    /// <param name="pilotLevelService">Pilot level service</param>
    [Inject]
    public void Setup(IRemoteConfig remoteConfig, IPilotLevelService pilotLevelService)
    {
        _remoteConfig = remoteConfig;
        _pilotLevelService = pilotLevelService;
    }

    /// <summary>
    /// Initializes the collection item service.
    /// </summary>
    void Start()
    {
        _generationCalledTime = Time.time;
        
        // Fetch configuration from remote settings
        _generationInterval = _remoteConfig.GetFloat(Parameter.CollectionItemGenerationInterval);
        _forceGenerationItemCount = _remoteConfig.GetInt(Parameter.ForceGenerationItemCount);
        
        // Get required components
        CollectionItemSpawner spawner = GetComponent<CollectionItemSpawner>();
        _uiController = GetComponent<CollectionItemUIController>();
        _directionService = GetComponent<CollectionItemDirectionService>();
        
        // Subscribe to direction service events
        _directionService.OnCollectedAnimationEnded += UpdateScore;
        
        // Initialize UI and generation service
        _uiController.UpdateUIScore(_mutualScore);
        _generationService = new CollectionItemGenerationService(OnItemCollected, OnItemLifeTimePassed, spawner, _remoteConfig);
        
        InitializeScoreDictionary();
    }
    #endregion

    #region Main Loop

    /// <summary>
    /// Updates the collection system, handles automatic generation and direction updates.
    /// </summary>
    void Update()
    {
        // Automatic item generation at specified intervals
        if (Time.time > _generationCalledTime + _generationInterval && TrajectoryManager.isTrajectoryEnded)
        {
            try
            {
                _generationService.HandleGeneration(_items, _itemPrefabs, ref _longLivingItemsCount);
            }
            catch(System.Exception e)
            {
                Debug.LogError($"CollectionItemService: Generation failed - {e.Message}");
            }
            _generationCalledTime = Time.time;
        }
        
        // Update direction indicators for all active items
        _directionService.HandleDirectionBoxes(_items, _uiController.ItemTypeSprites);
    }
    #endregion

    #region Event Handlers

    /// <summary>
    /// Handles item collection events.
    /// </summary>
    /// <param name="item">The collected item</param>
    private void OnItemCollected(GameObject item)
    {
        Debug.Log("CollectionItemService: Item collected");
        var collectionItem = item.GetComponent<CollectionItem>();
        
        // Update score immediately if no separate score panel or no direction box for this item
        if (ScorePanel.CurrentState.Type != ScorePanelType.Separate || !_directionService.HasDirectionBox(collectionItem)
            || collectionItem.Type == CollectionItemType.Bad)
        {
            UpdateScore(collectionItem);
        }
        
        // Play collection sound if available
        if (collectionItem.Audio)
        {
            collectionItem.Audio.Play();
        }
        
        // Disable collider to prevent repeatable collisions
        item.GetComponent<Collider>().enabled = false;
        RemoveItem(item, true);
    }

    /// <summary>
    /// Handles item lifetime expiration events.
    /// </summary>
    /// <param name="item">The expired item</param>
    private void OnItemLifeTimePassed(GameObject item)
    {
        RemoveItem(item, false);
    }
    #endregion

    #region Item Management

    /// <summary>
    /// Removes an item from the active list and handles cleanup.
    /// </summary>
    /// <param name="item">Item to remove</param>
    /// <param name="isCollected">Whether the item was collected or expired</param>
    private void RemoveItem(GameObject item, bool isCollected)
    {
        var collectionItem = item.GetComponent<CollectionItem>();
        
        // Update long-living item count if necessary
        if (collectionItem.IsLongLiving)
            _longLivingItemsCount--;
            
        // Remove from active items list
        _items.Remove(item);
        
        // Remove direction indicator
        _directionService.RemoveDirectionBox(item, isCollected);
        
        // Play collection animation and destroy item
        var animator = item.GetComponent<Animator>();
        if (animator)
        {
            animator.SetTrigger("Collect");
            StartCoroutine(DestroyItemRoutine(item, animator));
        }
        else
        {
            Destroy(item);
        }
    }

    /// <summary>
    /// Coroutine for destroying items after animation completes.
    /// </summary>
    /// <param name="item">Item to destroy</param>
    /// <param name="animator">Item's animator component</param>
    /// <returns>IEnumerator for coroutine</returns>
    private IEnumerator DestroyItemRoutine(GameObject item, Animator animator)
    {
        // Wait for animation to start
        yield return new WaitForSeconds(0.1f);
        
        // Wait for animation to complete
        while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1 || animator.IsInTransition(0)) 
        {
            yield return null;
        }
        
        Destroy(item);
    }
    #endregion

    #region Score Management

    /// <summary>
    /// Updates the score when an item is collected.
    /// </summary>
    /// <param name="collectionItem">The collected item</param>
    public void UpdateScore(CollectionItem collectionItem)
    {
        // Update mutual score
        _mutualScore += collectionItem.ScorePoint;
        
        // Update type-specific score
        if(_scores.TryGetValue(collectionItem.Type, out int currentScore))
        {
            _scores[collectionItem.Type] = currentScore + 1;
        }
        
        // Update UI and recalculate pilot level
        _uiController.UpdateUIScore(_mutualScore, _scores, collectionItem.Type);
        _pilotLevelService.CalculateCurrentLevel();
    }

    /// <summary>
    /// Initializes the score dictionary with saved values.
    /// </summary>
    private void InitializeScoreDictionary()
    {
        foreach(var item in _itemPrefabs)
        {
            var collectionItem = item.GetComponent<CollectionItem>();
            if (!_scores.ContainsKey(collectionItem.Type))
            {
                _scores.Add(collectionItem.Type, PlayerPrefsController.GetCollectionItemScore(collectionItem.Type));
            }
        }
        _uiController.InitializeSeparateScorePanel(_scores, _directionService);
    }
    #endregion

    #region Force Generation

    /// <summary>
    /// Forces generation of multiple items with animated entrance.
    /// </summary>
    /// <param name="limit">Maximum number of items to generate</param>
    public void ForceGenerateItems(int limit)
    {
        if (enabled == false || _items.Count >= limit)
            return;
            
        // Play generation animation
        if (_forceGenerationAnimator)
            _forceGenerationAnimator.SetTrigger("Shake");
            
        List<GameObject> generatedItems = new List<GameObject>();
        
        // Generate multiple items
        for(int i = 0; i < _forceGenerationItemCount; i++)
        {
            try
            {
                var item = _generationService.HandleGeneration(_items, _itemPrefabs, ref _longLivingItemsCount);
                if(item != null)
                    generatedItems.Add(item);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"CollectionItemService: Force generation failed - {e.Message}");
            }
        }
        
        // Animate items moving to their positions
        if(generatedItems.Count > 0)
        {
            int i = 1;
            foreach (var item in generatedItems)
            {
                var collider = item.GetComponentInChildren<Collider>();
                var renderers = item.GetComponentsInChildren<Renderer>();
                var ps = item.GetComponentInChildren<ParticleSystem>();
                
                // Disable components during animation
                if (collider)
                    collider.enabled = false;
                foreach(var renderer in renderers)
                    renderer.enabled = false;
                if (ps)
                    ps.Stop();
                    
                // Calculate animation positions
                Vector3 startPosition = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height * 2 / 3, 5f));
                Vector3 targetPosition = item.GetComponent<Transform>().position;
                item.GetComponent<Transform>().position = startPosition;
                
                // Start movement animation
                StartCoroutine(MoveItemRoutine(item, 2.5f, 1, i, startPosition, targetPosition, renderers, ps, collider));
                i++;
            }
        }
        
        _generationCalledTime = Time.time;
    }

    /// <summary>
    /// Coroutine for animating item movement from screen to target position.
    /// </summary>
    private IEnumerator MoveItemRoutine(GameObject item, float moveTime, int delay, int itemCount, Vector3 startPosition, Vector3 targetPosition
        ,Renderer[] renderers, ParticleSystem ps, Collider collider = null)
    {
        // Wait for staggered animation start
        yield return new WaitForSeconds(delay * itemCount);
        
        // Enable renderers for visibility
        foreach (var renderer in renderers)
        {
            renderer.enabled = true;
        }
        
        Transform itemTransform = item.GetComponent<Transform>();
        float time = 0;
        
        // Animate movement to target position
        while (time < moveTime)
        {
            itemTransform.position = Vector3.Lerp(startPosition, targetPosition, time / moveTime);
            time += Time.deltaTime;
            yield return null;
        }
        
        itemTransform.position = targetPosition;
        
        // Re-enable components after animation
        if (collider)
            collider.enabled = true;
        if (ps)
            ps.Play();
    }
    #endregion

    #region Lifecycle

    /// <summary>
    /// Called when the service is enabled.
    /// Loads saved mutual score from player preferences.
    /// </summary>
    private void OnEnable()
    {
        _mutualScore = PlayerPrefsController.CollectionItem_MutualScore;
    }

    /// <summary>
    /// Saves collection item scores to player preferences.
    /// Called by AutoSaveBehaviour base class.
    /// </summary>
    protected override void SaveData()
    {
        // Save each collection item score by type
        foreach (var score in _scores)
        {
            PlayerPrefsController.SetCollectionItemScore(score.Key, score.Value);
        }
        
        // Save mutual score
        PlayerPrefsController.CollectionItem_MutualScore = _mutualScore;
    }
    #endregion

}
