using UnityEngine;
using TMPro;

/// <summary>
/// Manages helicopter physics, movement, and flight characteristics.
/// Handles realistic helicopter simulation including cyclic control, auto-leveling,
/// landing mechanics, and advanced flight physics.
/// </summary>
public class Heli_Characteristics : MonoBehaviour
{
    #region Physics Configuration

    /// <summary>
    /// Force applied to tail rotor for anti-torque control.
    /// </summary>
    public float tailForce = 2f;
    
    [Header("Cyclic Properties")]
    /// <summary>
    /// Force applied by cyclic control for pitch/roll.
    /// </summary>
    public float cyclicForce = 30f;
    
    /// <summary>
    /// Force for cyclic-based movement.
    /// </summary>
    public float cyclicMovementForce = 2000f;
    
    [Space]
    
    /// <summary>
    /// Force applied for automatic leveling.
    /// </summary>
    public float correctiveForce = 2f;

    /// <summary>
    /// Forward direction vector flattened to horizontal plane.
    /// </summary>
    private Vector3 flatFwd;
    
    /// <summary>
    /// Right direction vector flattened to horizontal plane.
    /// </summary>
    private Vector3 flatRight;
    
    /// <summary>
    /// Dot product with forward direction for angle calculations.
    /// </summary>
    private float forwardDot;
    
    /// <summary>
    /// Dot product with right direction for angle calculations.
    /// </summary>
    private float rightDot;

    /// <summary>
    /// Visual indicator prefab for target position.
    /// </summary>
    public GameObject dotPrefab;
    
    /// <summary>
    /// Instantiated dot indicator instance.
    /// </summary>
    private GameObject dot;

    [Header("Cyclic Control System")]
    /// <summary>
    /// Previous camera rotation for cyclic calculations.
    /// </summary>
    private float prevCamRotationY;
    
    /// <summary>
    /// Temporary camera rotation for calculations.
    /// </summary>
    private float tmpCamRotation;
    
    /// <summary>
    /// Main camera transform reference.
    /// </summary>
    [SerializeField]
    private Transform cam;
    
    /// <summary>
    /// Distance threshold for cyclic activation.
    /// </summary>
    private float cyclicDis = 0.3f;
    
    /// <summary>
    /// Flag indicating cyclic control is active.
    /// </summary>
    private bool cyclicFlag = false;
    
    /// <summary>
    /// Camera-based cyclic control state.
    /// </summary>
    private int cyclicCamFlag = 0;
    
    /// <summary>
    /// Previous dot position for movement calculations.
    /// </summary>
    private Vector3 prevDotPos;
    
    /// <summary>
    /// Direction of cyclic input.
    /// </summary>
    private float dir = 0;
    
    /// <summary>
    /// Flag for handling 360-degree angle crossover.
    /// </summary>
    private bool isZeroAngleCrossed;
    
    /// <summary>
    /// Previous camera position for movement detection.
    /// </summary>
    private Vector3 prevCamPos;
    
    [Space]

    /// <summary>
    /// Initial helicopter Y position for takeoff calculations.
    /// </summary>
    private float initHeliY = -500;
    
    /// <summary>
    /// Initial helicopter Z position for takeoff calculations.
    /// </summary>
    private float initHeliZ = -500;
    
    /// <summary>
    /// Flag indicating initial height has been set.
    /// </summary>
    private bool startHeightInitialized = false;

    /// <summary>
    /// Prefab for landing plane visualization.
    /// </summary>
    [SerializeField]
    private GameObject planePrefab;
    
    /// <summary>
    /// Instantiated landing plane instance.
    /// </summary>
    private GameObject plane;

    /// <summary>
    /// Time when main rotor started.
    /// </summary>
    private float startTime;
    
    /// <summary>
    /// Flag indicating start time has been initialized.
    /// </summary>
    private bool isStartTimeInitialized = false;

    /// <summary>
    /// Flag indicating rise and rotate maneuver has been completed.
    /// </summary>
    private bool riseAndRotatePassed = false;

    /// <summary>
    /// Flag indicating rise and rotate initialization.
    /// </summary>
    private bool riseAndRotateInitialized = false;
    
    /// <summary>
    /// Flag indicating speaker audio has started.
    /// </summary>
    private bool isSpeaker2PlayStarted = false;

    /// <summary>
    /// Static flag indicating helicopter is landed.
    /// </summary>
    private static bool isHeliLanded = true;

    /// <summary>
    /// Gets whether helicopter is currently landed.
    /// </summary>
    public static bool IsHeliLanded { get => isHeliLanded; set => isHeliLanded = value; }
    
    /// <summary>
    /// Gets or sets rise and rotate completion status.
    /// </summary>
    public bool RiseAndRotatePassed { get => riseAndRotatePassed; set => riseAndRotatePassed = value; }
    
    /// <summary>
    /// Gets or sets trick disabler game object.
    /// </summary>
    public GameObject TrickDisabler { get => trickDisabler; set => trickDisabler = value; }
    
    /// <summary>
    /// Gets or sets non-education heliport destruction status.
    /// </summary>
    public bool NotEducationHeliportDestroyed { get => notEducationHeliportDestroyed; set => notEducationHeliportDestroyed = value; }
    
    /// <summary>
    /// Modifier for rapid RPM changes to smooth movement.
    /// </summary>
    private float RapidRpmChangeModifier { 
        get { return _rapidRpmChangeModifier; }
        set {
            _rapidRpmChangeModifier = Mathf.Clamp(value, 1, 5);
        } 
    }

    /// <summary>
    /// Game object that disables trick functionality.
    /// </summary>
    private GameObject trickDisabler;

    /// <summary>
    /// Flag for non-education heliport destruction.
    /// </summary>
    private bool notEducationHeliportDestroyed;

    /// <summary>
    /// Velocity vector for smooth movement calculations.
    /// </summary>
    private Vector3 movementVelocity = Vector3.zero;

    /// <summary>
    /// Internal rapid RPM change modifier.
    /// </summary>
    private float _rapidRpmChangeModifier = 1f;

    /// <summary>
    /// Previous maximum RPM for change detection.
    /// </summary>
    private float _prevMaxRpm = 700f;

    /// <summary>
    /// Flag indicating cyclic control is enabled.
    /// </summary>
    private bool _cyclicEnabled;

    /// <summary>
    /// Free flight timer component.
    /// </summary>
    private HeliFreeFlyTimer _flightTimer;

    #endregion

    #region Initialization

    /// <summary>
    /// Initializes helicopter characteristics system.
    /// Sets up camera, visual indicators, and event subscriptions.
    /// </summary>
    private void Start()
    {
        cam = Camera.main.transform;
        dot = Instantiate(dotPrefab, cam.transform);
        dot.transform.localPosition = new Vector3(0, 0, 3);
        prevDotPos = dot.transform.position;
        prevCamRotationY = cam.rotation.eulerAngles.y;
        
        // Subscribe to engine events
        transform.GetComponentInChildren<Heli_Engine>().OnStartMaxRPMAchieved += OnStartMaxRPMAchieved;
    }
    #endregion

    #region Main Update Logic

    /// <summary>
    /// Initializes the main rotor start time.
    /// Called when main rotor is enabled.
    /// </summary>
    public void InitStartMainRotorTime()
    {
        startTime = Time.time;
    }

    /// <summary>
    /// Main update method for helicopter physics and characteristics.
    /// Handles all flight mechanics based on current state.
    /// </summary>
    /// <param name="rb">Helicopter rigidbody</param>
    /// <param name="input">Input controller</param>
    /// <param name="engine">Engine component</param>
    /// <param name="autoLevelOnly">Whether to only apply auto-leveling</param>
    public void UpdateCharacteristics(Rigidbody rb, Heli_Input input, Heli_Engine engine, bool autoLevelOnly)
    {
        // Initialize start time tracking if needed
        if (!isStartTimeInitialized)
        {
            input.OnMainRotorEnabled += InitStartMainRotorTime;
            isStartTimeInitialized = true;
        }
        
        // Calculate angles for auto-leveling
        CalculateAngles();
        
        // Apply auto-leveling forces
        AutoLevel(rb);
        
        // Update visual indicator position
        HandleDotPosition(engine);

        // Handle landing/takeoff mechanics
        if (engine.SeekRPM > 200f && engine.CurRPM > 350f && !autoLevelOnly)
        {
            DestroyPlane();
            isHeliLanded = false;
        }
        
        if(engine.SeekRPM < 200f && !autoLevelOnly && riseAndRotatePassed)
        {
            HandlePlane(rb);
            isHeliLanded = true;
        }
        else if (!autoLevelOnly)
        {
            // Handle movement and controls
            HandleMovement(rb, input, engine);
            
            if (notEducationHeliportDestroyed)
            {
                // Apply pedal and cyclic controls
                HandlePedals(rb, input, engine, 0);
                HandleCyclic(rb, input, engine);
            }
        }
        
        // Update trick disabler state
        ControllTrickDisabler();
        
        // Update flight timer if in flight
        if(isHeliLanded == false && _flightTimer) {
            _flightTimer.UpdateFlightTime();
        }
    }
    #endregion
    #region Auto-Leveling System

    /// <summary>
    /// Applies automatic leveling forces to keep helicopter stable.
    /// Uses corrective forces based on current tilt angles.
    /// </summary>
    /// <param name="rb">Helicopter rigidbody</param>
    private void AutoLevel(Rigidbody rb)
    {
        // Calculate corrective forces based on angle deviations
        float forwardForce = correctiveForce * rightDot;
        float rightForce = -correctiveForce * forwardDot;
        
        // Apply torque to level the helicopter
        rb.AddRelativeTorque(Vector3.forward * forwardForce, ForceMode.Acceleration);
        rb.AddRelativeTorque(Vector3.right * rightForce, ForceMode.Acceleration);
    }

    /// <summary>
    /// Calculates angle vectors for auto-leveling and cyclic control.
    /// Projects forward and right vectors onto horizontal plane.
    /// </summary>
    private void CalculateAngles()
    {
        // Flatten forward vector to horizontal plane
        flatFwd = transform.forward;
        flatFwd.y = 0;
        flatFwd = flatFwd.normalized;

        // Flatten right vector to horizontal plane
        flatRight = transform.right;
        flatRight.y = 0;
        flatRight = flatRight.normalized;

        // Calculate dot products for angle measurements
        forwardDot = Vector3.Dot(transform.up, flatFwd);
        rightDot = Vector3.Dot(transform.up, flatRight);
    }
    #endregion

    #region Flight Control Systems

    /// <summary>
    /// Handles landing plane creation and positioning.
    /// Creates visual landing aid and guides helicopter to landing position.
    /// </summary>
    /// <param name="rb">Helicopter rigidbody</param>
    private void HandlePlane(Rigidbody rb)
    {
        if (plane == null)
        {
            // Create landing plane at appropriate position
            plane = Instantiate(planePrefab, new Vector3(dot.transform.position.x,
                dot.transform.position.y - 0.4f, dot.transform.position.z), Quaternion.Euler(0,-rb.rotation.y,0));
        }
        
        // Guide helicopter toward landing position
        rb.position = Vector3.MoveTowards(rb.position, plane.transform.position + new Vector3(0, 0.36f, 0), 0.01f);
    }

    /// <summary>
    /// Handles cyclic control based on camera rotation and movement.
    /// Implements realistic helicopter cyclic control mechanics.
    /// </summary>
    /// <param name="rb">Helicopter rigidbody</param>
    /// <param name="input">Input controller</param>
    /// <param name="engine">Engine component</param>
    protected virtual void HandleCyclic(Rigidbody rb, Heli_Input input, Heli_Engine engine)
    {
        // Require minimum RPM for cyclic control
        if (engine.CurRPM < 200f || engine.CurTailRPM < 200f)
            return;
        if (!_cyclicEnabled)
            return;
            
        // Calculate camera rotation rate
        float camRotationRate = cam.rotation.eulerAngles.y - prevCamRotationY;

        // Determine rotation direction
        dir = camRotationRate > 0 ? 1 : -1;
        
        // Handle 360-degree angle crossover
        if (Mathf.Abs(cam.rotation.eulerAngles.y - tmpCamRotation) > 200)
        {
            isZeroAngleCrossed = !isZeroAngleCrossed;
        }
        
        // Adjust direction if angle was crossed
        if (isZeroAngleCrossed)
            dir *= -1;

        // Check if cyclic should be activated based on distance
        if (Mathf.Abs((dot.transform.position - prevDotPos).magnitude) > cyclicDis && cyclicFlag == false)
        {
            cyclicFlag = true;
            prevDotPos = dot.transform.position;
        }
        else if (Mathf.Abs((dot.transform.position - rb.position).magnitude) < cyclicDis)
        {
            // Reset cyclic state when helicopter reaches target
            prevCamRotationY = cam.rotation.eulerAngles.y;
            cyclicFlag = false;
            isZeroAngleCrossed = false;
            cyclicCamFlag = 0;
            prevCamPos = cam.position;
        }

        // Apply cyclic torque based on camera rotation
        if (cyclicFlag == true && Mathf.Abs(camRotationRate) > 5f)
        {
            rb.AddRelativeTorque(Vector3.forward * cyclicForce * dir, ForceMode.Acceleration);
        }
        
        // Store previous camera rotation for next frame
        tmpCamRotation = cam.rotation.eulerAngles.y;

        // Apply cyclic based on camera movement direction
        if (cyclicFlag == true)
        {
            // Forward movement detection
            if (Mathf.Abs((dot.transform.position - prevDotPos).normalized.x - cam.transform.forward.x) < 0.25
                && Mathf.Abs((dot.transform.position - prevDotPos).normalized.z - cam.transform.forward.z) < 0.15)
            {
                cyclicCamFlag = 1; // Forward torque
            }
            // Backward movement detection
            else if (Mathf.Abs(((dot.transform.position - prevDotPos).normalized * -1).x - cam.transform.forward.x) < 0.4
                && Mathf.Abs(((dot.transform.position - prevDotPos).normalized * -1).z - cam.transform.forward.z) < 0.15)
            {
                cyclicCamFlag = 2; // Back torque
            }
            // Right movement detection
            else if (Mathf.Abs((dot.transform.position - prevDotPos).normalized.x - cam.transform.right.x) < 0.15
                && Mathf.Abs((dot.transform.position - prevDotPos).normalized.z - cam.transform.right.z) < 0.25)
            {
                cyclicCamFlag = 3; // Right torque
            }
            // Left movement detection
            else if (Mathf.Abs(((dot.transform.position - prevDotPos).normalized * -1).x - cam.transform.right.x) < 0.15
                && Mathf.Abs(((dot.transform.position - prevDotPos).normalized * -1).z - cam.transform.right.z) < 0.25)
            {
                cyclicCamFlag = 4; // Left torque
            }
            // Special case: backward movement with additional torque
            else if(((dot.transform.position - prevDotPos).normalized * -1).z > 0.05)
            {
                rb.AddRelativeTorque(Vector3.right * cyclicForce/1.5f, ForceMode.Acceleration);
                
                // Apply additional torque for lateral movement
                if((dot.transform.position - prevDotPos).normalized.x > 0.4)
                {
                    rb.AddRelativeTorque(Vector3.forward * cyclicForce/2f, ForceMode.Acceleration);
                }
                else if ((dot.transform.position - prevDotPos).normalized.x < -0.4)
                {
                    rb.AddRelativeTorque(Vector3.forward * -cyclicForce/2f, ForceMode.Acceleration);
                }
            }
            // Camera movement detected
            else if ((cam.position - prevCamPos).magnitude > 0.2)
            {
                cyclicCamFlag = 5;
            }
            
            // Apply torque based on detected movement direction
            switch (cyclicCamFlag) {
                case 1:
                    rb.AddRelativeTorque(Vector3.right * -cyclicForce * 1.5f, ForceMode.Acceleration);
                    break;
                case 2: 
                    rb.AddRelativeTorque(Vector3.right * cyclicForce * 1.5f, ForceMode.Acceleration);
                    break;
                case 3: 
                    rb.AddRelativeTorque(Vector3.forward * cyclicForce, ForceMode.Acceleration);
                    break;
                case 4: 
                    rb.AddRelativeTorque(Vector3.forward * -cyclicForce, ForceMode.Acceleration);
                    break;
                default: 
                    break;
            }
        }
    }

    /// <summary>
    /// Handles pedal control for helicopter yaw rotation.
    /// Aligns helicopter with camera direction based on tail rotor input.
    /// </summary>
    /// <param name="rb">Helicopter rigidbody</param>
    /// <param name="input">Input controller</param>
    /// <param name="engine">Engine component</param>
    /// <param name="offset">Rotation offset</param>
    protected virtual void HandlePedals(Rigidbody rb, Heli_Input input, Heli_Engine engine, float offset)
    {
        // Require minimum RPM for pedal control
        if (engine.CurRPM < 200f || engine.CurTailRPM < 200f)
            return;
            
        // Special rotation mode for direct look control
        if(cyclicCamFlag == 5)
        {
            rb.rotation = Quaternion.Lerp(rb.rotation, Quaternion.LookRotation(-(dot.transform.position - rb.position)), Time.deltaTime);
        }
        else
        {
            // Align helicopter with camera yaw
            rb.rotation = Quaternion.Lerp(rb.rotation, Quaternion.Euler(rb.rotation.eulerAngles.x, cam.rotation.eulerAngles.y + 180 + offset,
                rb.rotation.eulerAngles.z), Time.deltaTime);
        }
    }

    /// <summary>
    /// Handles helicopter movement and positioning.
    /// Controls vertical movement, takeoff, and forward flight.
    /// </summary>
    /// <param name="rb">Helicopter rigidbody</param>
    /// <param name="input">Input controller</param>
    /// <param name="engine">Engine component</param>
    private void HandleMovement(Rigidbody rb, Heli_Input input, Heli_Engine engine)
    {
        // Require full throttle for movement
        if (input.StickyThrottleInput != 1)
            return;
            
        // Calculate normalized RPM for movement speed
        float normalizedRPM = Mathf.InverseLerp(0f, Heli_Engine.MAX_RPM, engine.CurRPM);
        normalizedRPM *= 2;
        normalizedRPM = normalizedRPM < 0.5f ? 0f : normalizedRPM;
        normalizedRPM = normalizedRPM > 1.8f ? 2f : normalizedRPM;

        // Handle initial takeoff sequence
        if ((input.StickyThrottleInput == 1 && engine.CurTailRPM < 200f && !riseAndRotatePassed) || (!EducationMode.IsEducation() && !notEducationHeliportDestroyed))
        {
            RiseAndRotate(rb, input, engine, normalizedRPM);
        }
        // Handle normal flight movement
        else if(input.StickyThrottleInput == 1 && engine.CurTailRPM > 200f)
        {
            riseAndRotatePassed = true;
            float normalizedTailRPM = Mathf.InverseLerp(0f, engine.SeekRPM, engine.CurTailRPM);
            
            // Handle rapid RPM changes for smooth movement
            HandleRapidRPMChange(engine, rb);
            
            // Move helicopter toward target position
            rb.position = Vector3.Lerp(rb.position, dot.transform.position, Time.deltaTime * normalizedRPM / 2 * normalizedTailRPM / RapidRpmChangeModifier);
        }
    }

    /// <summary>
    /// Modifies movement speed to handle rapid RPM changes smoothly.
    /// Prevents jerky movement when RPM values change quickly.
    /// </summary>
    /// <param name="engine">Engine component</param>
    /// <param name="rb">Helicopter rigidbody</param>
    private void HandleRapidRPMChange(Heli_Engine engine, Rigidbody rb)
    {
        RapidRpmChangeModifier -= Time.deltaTime;
        float diff = Mathf.Abs(engine.SeekRPM - _prevMaxRpm);
        
        // Increase modifier for large RPM changes
        if (diff > 100f)
        {
            RapidRpmChangeModifier = diff / 100f;
        }
        
        // Reset modifier when helicopter reaches destination
        if(Mathf.Approximately(rb.position.x, dot.transform.position.x) && Mathf.Approximately(rb.position.y, dot.transform.position.y)
            && Mathf.Approximately(rb.position.z, dot.transform.position.z))
        {
            RapidRpmChangeModifier = 1f;
        }
        
        _prevMaxRpm = engine.SeekRPM;
    }

    /// <summary>
    /// Handles the initial takeoff sequence with rise and rotation.
    /// Guides helicopter through proper takeoff procedure.
    /// </summary>
    /// <param name="rb">Helicopter rigidbody</param>
    /// <param name="input">Input controller</param>
    /// <param name="engine">Engine component</param>
    /// <param name="normalizedRPM">Normalized engine RPM</param>
    private void RiseAndRotate(Rigidbody rb, Heli_Input input, Heli_Engine engine, float normalizedRPM)
    {
        // Store initial position for takeoff calculations
        if (!startHeightInitialized)
        {
            initHeliY = rb.position.y;
            initHeliZ = rb.position.z;
            startHeightInitialized = true;
        }
 
        // Move forward and rise during takeoff
        rb.position = Vector3.Lerp(rb.position, new Vector3(rb.position.x, initHeliY + 2.5f, initHeliZ + 1f),
            Time.deltaTime * normalizedRPM / 8);
            
        // Start rotation when sufficient RPM is reached
        if (engine.CurRPM > 550f)
        {
            if (!isSpeaker2PlayStarted)
            {
                isSpeaker2PlayStarted = true;
                AudioManager.Play("Speaker2");
            }
            if (!riseAndRotateInitialized)
            {
                input.riseAndRotateFlag = true;
                input.riseAndRotateTime = Time.time;
                riseAndRotateInitialized = true;
            }
            
            // Apply rotation during education mode
            if(EducationMode.IsEducation())
                rb.AddTorque(0, -1.5f, 0, ForceMode.Acceleration);
        }
    }

    /// <summary>
    /// Updates the visual indicator position based on engine RPM.
    /// Moves dot up/down to represent target altitude.
    /// </summary>
    /// <param name="engine">Engine component</param>
    private void HandleDotPosition(Heli_Engine engine)
    {
        float y = Mathf.InverseLerp(0, engine.StandartRPM*2, engine.SeekRPM);
        y = y * 3 - 1.5f;
        dot.transform.localPosition = new Vector3(0, y, 3);
    }

    /// <summary>
    /// Event handler for when start maximum RPM is achieved.
    /// Marks helicopter as airborne.
    /// </summary>
    public void OnStartMaxRPMAchieved()
    {
        isHeliLanded = false;
    }

    /// <summary>
    /// Destroys the landing plane visual aid.
    /// </summary>
    public void DestroyPlane()
    {
        if (plane != null)
        {
            Destroy(plane);
        }
    }

    /// <summary>
    /// Controls the trick disabler based on landing state.
    /// Disables tricks when helicopter is landed.
    /// </summary>
    private void ControllTrickDisabler()
    {
        trickDisabler.SetActive(IsHeliLanded);
    }

    /// <summary>
    /// Enables cyclic control system.
    /// Called when helicopter is ready for advanced control.
    /// </summary>
    public void EnableCyclic()
    {
        _cyclicEnabled = true;
        prevDotPos = dot.transform.position;
    }

    /// <summary>
    /// Initializes the flight timer for tracking free flight time.
    /// </summary>
    /// <param name="score">UI text component for displaying time</param>
    public void InitFlightTimer(TextMeshProUGUI score)
    {
        _flightTimer = gameObject.AddComponent<HeliFreeFlyTimer>();
        _flightTimer.Score = score;
    }
    #endregion
}
