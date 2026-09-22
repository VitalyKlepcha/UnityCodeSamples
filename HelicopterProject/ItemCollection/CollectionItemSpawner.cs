using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// Handles spawning of collection items in AR environment.
/// Manages AR plane detection and positions items on detected surfaces.
/// </summary>
public class CollectionItemSpawner : MonoBehaviour 
{
    #region Configuration

    /// <summary>
    /// Camera transform for position calculations.
    /// </summary>
    [SerializeField]
    private Transform _cam;

    /// <summary>
    /// AR plane manager for surface detection.
    /// </summary>
    [SerializeField]
    private ARPlaneManager _planeManager;

    /// <summary>
    /// Dictionary of detected AR planes by their trackable IDs.
    /// </summary>
    private Dictionary<TrackableId, ARPlane> _planes = new Dictionary<TrackableId, ARPlane>();
    #endregion

    #region AR Plane Management

    /// <summary>
    /// Initializes the spawner and subscribes to AR plane events.
    /// </summary>
    void Start()
    {
        _planeManager.trackablesChanged.AddListener(OnTrackablesChange);
    }

    /// <summary>
    /// Handles AR plane trackable changes (added/removed planes).
    /// </summary>
    /// <param name="args">Arguments containing changed trackables</param>
    private void OnTrackablesChange(ARTrackablesChangedEventArgs<ARPlane> args)
    {
        // Add new planes to the dictionary
        foreach (var plane in args.added)
        {
            _planes.Add(plane.trackableId, plane);
        }
        
        // Remove planes that are no longer tracked
        foreach (var plane in args.removed)
        {
            _planes.Remove(plane.Value.trackableId);
        }
    }
    #endregion
    #region Item Spawning

    /// <summary>
    /// Spawns an item at a calculated position on an AR plane.
    /// </summary>
    /// <param name="itemPrefab">Prefab to spawn</param>
    /// <returns>Spawned item GameObject</returns>
    public GameObject SpawnItem(GameObject itemPrefab)
    {
        var worldPos = CalculatePosition();
        var item = Instantiate(itemPrefab, worldPos, Quaternion.identity);
        
        // Add AR anchor for stable tracking
        item.AddComponent<ARAnchor>();
        
        return item;
    }

    /// <summary>
    /// Calculates a valid spawn position on detected AR planes.
    /// </summary>
    /// <returns>World position for spawning</returns>
    /// <exception cref="PlaneNotFoundException">Thrown when no planes are available</exception>
    private Vector3 CalculatePosition()
    {
#if UNITY_EDITOR
        // Editor mode: spawn near camera for testing
        return _cam.position + new Vector3(Random.Range(0, 5), 0, Random.Range(0, 5));
#else
        // AR mode: spawn on detected planes
        if (_planes.Count == 0)
            throw new PlaneNotFoundException("No available planes");
            
        // Select a random plane from detected surfaces
        var plane = _planes.ElementAt(Random.Range(0, _planes.Count)).Value;

        // Calculate random position within plane boundaries
        float x = Random.Range(-plane.size.x/2, plane.size.x/2);
        float y = Random.Range(-plane.size.y / 2, plane.size.y / 2);

        // Adjust axes based on plane alignment (vertical vs horizontal)
        Vector3 pos = plane.alignment == PlaneAlignment.Vertical ? plane.center + new Vector3(x,y,0) : plane.center + new Vector3(x,0,y);
        
        return pos;
#endif
    }
    #endregion

}

/// <summary>
/// Custom exception thrown when no AR planes are available for spawning.
/// </summary>
public class PlaneNotFoundException : System.Exception
{
    /// <summary>
    /// Creates a new PlaneNotFoundException.
    /// </summary>
    public PlaneNotFoundException() { }

    /// <summary>
    /// Creates a new PlaneNotFoundException with a message.
    /// </summary>
    /// <param name="message">Error message</param>
    public PlaneNotFoundException(string message) : base(message) { }

    /// <summary>
    /// Creates a new PlaneNotFoundException with message and inner exception.
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="inner">Inner exception</param>
    public PlaneNotFoundException(string message, System.Exception inner) : base(message, inner) { }
}

