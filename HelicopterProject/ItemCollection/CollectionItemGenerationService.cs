using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

/// <summary>
/// Service responsible for generating collection items with configurable properties.
/// Handles item selection, lifetime configuration, and spawning logic.
/// </summary>
public class CollectionItemGenerationService
{
    #region Configuration

    /// <summary>
    /// Maximum number of items that can exist simultaneously.
    /// </summary>
    private int _maxItemsNum = 5;

    /// <summary>
    /// Maximum number of long-living items allowed at once.
    /// </summary>
    private int _maxLongLivingItemsNum = 2;

    /// <summary>
    /// Reference to the spawner component for instantiating items.
    /// </summary>
    private CollectionItemSpawner _spawner;

    /// <summary>
    /// Default lifetime for regular items in seconds.
    /// </summary>
    private static float _itemLifeTime = 30f;

    /// <summary>
    /// Default lifetime for long-living items in seconds.
    /// </summary>
    private static float _longLivingItemLifeTime = 300f;

    /// <summary>
    /// Callback action triggered when an item is collected.
    /// </summary>
    private Action<GameObject> OnItemCollectedAction;

    /// <summary>
    /// Callback action triggered when an item's lifetime expires.
    /// </summary>
    private Action<GameObject> OnItemLifeTimePassedAction;

    /// <summary>
    /// Gets the default lifetime for regular items.
    /// </summary>
    public static float ItemLifeTime { get => _itemLifeTime; }

    /// <summary>
    /// Gets the default lifetime for long-living items.
    /// </summary>
    public static float LongLivingItemLifeTime { get => _longLivingItemLifeTime; }

    /// <summary>
    /// Remote config service for fetching configurable values.
    /// </summary>
    private IRemoteConfig _remoteConfig;
    #endregion

    #region Initialization

    /// <summary>
    /// Initializes the generation service with dependencies and configuration.
    /// </summary>
    /// <param name="OnItemCollected">Callback for item collection events</param>
    /// <param name="OnItemLifeTimePassed">Callback for item lifetime expiration events</param>
    /// <param name="spawner">Reference to the item spawner</param>
    /// <param name="remoteConfig">Remote configuration service</param>
    public CollectionItemGenerationService(Action<GameObject> OnItemCollected, Action<GameObject> OnItemLifeTimePassed, CollectionItemSpawner spawner, IRemoteConfig remoteConfig)
    {
        _remoteConfig = remoteConfig;
        
        // Fetch lifetime values from Firebase remote configuration
        _itemLifeTime = _remoteConfig.GetFloat(Parameter.CollectionItemNormalLifeTime);
        _longLivingItemLifeTime = _remoteConfig.GetFloat(Parameter.CollectionItemLongLifeTime);

        OnItemCollectedAction = OnItemCollected;
        OnItemLifeTimePassedAction = OnItemLifeTimePassed;
        _spawner = spawner;
    }
    #endregion
    #region Generation Logic

    /// <summary>
    /// Attempts to generate a new collection item if capacity allows.
    /// </summary>
    /// <param name="items">Current list of active items</param>
    /// <param name="itemPrefabs">Available item prefabs to choose from</param>
    /// <param name="longLivingItemsCount">Reference to current long-living items count</param>
    /// <returns>Generated item GameObject or null if capacity reached</returns>
    public GameObject HandleGeneration(List<GameObject> items, List<GameObject> itemPrefabs, ref int longLivingItemsCount)
    {
        if (items.Count >= _maxItemsNum)
            return null;
            
        var preparedItem = GenerateItem(itemPrefabs, ref longLivingItemsCount);
        items.Add(preparedItem);
        return preparedItem;
    }

    /// <summary>
    /// Generates a new item by selecting a prefab based on uniqueness and configuring its properties.
    /// </summary>
    /// <param name="itemPrefabs">Available item prefabs</param>
    /// <param name="longLivingItemsCount">Reference to current long-living items count</param>
    /// <returns>Generated and configured item GameObject</returns>
    private GameObject GenerateItem(List<GameObject> itemPrefabs, ref int longLivingItemsCount)
    {
        GameObject itemPrefab;
        
        // Select prefab based on uniqueness probability
        while (true) {
            var prefabNum = UnityEngine.Random.Range(0, itemPrefabs.Count);
            itemPrefab = itemPrefabs[prefabNum];
            
            // Consider item uniqueness - higher uniqueness means higher chance to spawn
            if (UnityEngine.Random.Range(1, 11) <= itemPrefab.GetComponent<CollectionItem>().Uniqueness)
                break;
        }
        
        var item = _spawner.SpawnItem(itemPrefab);
        var configuredItem = ConfigureItem(item, ref longLivingItemsCount);
        return configuredItem;
    }

    /// <summary>
    /// Configures the generated item with lifetime properties and event subscriptions.
    /// </summary>
    /// <param name="item">The item to configure</param>
    /// <param name="longLivingItemsCount">Reference to current long-living items count</param>
    /// <returns>Configured item GameObject</returns>
    private GameObject ConfigureItem(GameObject item, ref int longLivingItemsCount)
    {
        bool isLongLiving = false;
        
        // Randomly decide if new item will be long-living, respecting the maximum limit
        if (longLivingItemsCount < _maxLongLivingItemsNum)
        {
            isLongLiving = UnityEngine.Random.Range(0, 2) == 0 ? false : true;
        }
        
        var collectionItem = item.GetComponent<CollectionItem>();
        collectionItem.IsLongLiving = isLongLiving;
        
        if (isLongLiving)
        {
            longLivingItemsCount++;
            collectionItem.LifeTime = _longLivingItemLifeTime;
        }
        else
        {
            collectionItem.LifeTime = _itemLifeTime;
        }
        
        // Subscribe to item events
        collectionItem.OnItemCollected += OnItemCollectedAction;
        collectionItem.OnItemLifeTimePassed += OnItemLifeTimePassedAction;
        
        return item;
    }
    #endregion
}

