using KartGame.KartSystems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BonusLogic : MonoBehaviour
{
    /// <summary>
    /// Start position of player on level
    /// </summary>
    private Vector3 _startPosition;
    private Quaternion _startRotation;

    [SerializeField]
    BonusContainer _bonusContainer;
    [SerializeField] BonusNotificationManager _bonusNotificationManager;
    [SerializeField] BonusSpriteDatabase _bonusSpriteDatabase;
    ArcadeKart _kart = null;
    GrassCutter _cutter = null;
    private Coroutine _shieldRemoveCoroutine;
    private bool _isShielded = false;
    private bool _isInTornado = false;

    [SerializeField]
    [Tooltip("VFX that will spawn when dynamite bonus is picked up")]
    private GameObject DynamiteVFX;
    [Tooltip("VFX that will spawn when shield bonus item is picked up.")]
    public GameObject ShieldVFX;
    [Tooltip("VFX that will spawn when tornado bonus item is picked up.")]
    public GameObject TornadoVFX;

    private GameObject _dynamiteVFXObject;
    private GameObject _shieldVFXObject;
    private GameObject _tornadoVFXObject;

    [SerializeField]
    private AudioSource _tornadoAudioSource;

    [SerializeField]
    [Tooltip("Play area terrain")]
    private Terrain _terrain;

    private Vector4 _terrainBoundaries;

    private void Awake()
    {
        _kart = GetComponent<ArcadeKart>();
        _cutter = GetComponentInChildren<GrassCutter>();
        _startPosition = _kart.transform.position;
        _startRotation = _kart.transform.rotation;
        if (!_terrain)
            _terrain = FindFirstObjectByType<Terrain>();
        //min x, min z, max x, max z
        _terrainBoundaries = new Vector4(_terrain.transform.position.x, _terrain.transform.position.z,
            _terrain.transform.position.x + _terrain.terrainData.size.x, _terrain.transform.position.z + _terrain.terrainData.size.z);
    }
    internal void ApplyBonus(BonusItem bonusItem)
    {
        //Do not apply bad bonus when shield bonus is active
        if ((_isShielded && bonusItem.IsBad) || _isInTornado)
            return;
        //Play grab animation
        bonusItem.grab();
        //Get randomised bonus
        if (bonusItem.BonusType == BonusItem.BonusTypes.GoodRandom)
            bonusItem = GetRandomBonus(false);
        else if (bonusItem.BonusType == BonusItem.BonusTypes.BadRandom)
            bonusItem = GetRandomBonus(true);
        _bonusNotificationManager.ShowBonusNotification(bonusItem.BonusType, _bonusSpriteDatabase);
        switch (bonusItem.BonusType)
            {
                case BonusItem.BonusTypes.Fuel:
                    LevelLogic.addFuel(bonusItem.Value);
                    break;
                case BonusItem.BonusTypes.BladeSize:
                    if (_cutter)
                        _cutter.applySizeBonus(bonusItem.Duration);
                    break;
                case BonusItem.BonusTypes.Fire:
                    LevelLogic.SubstractFuel(bonusItem.Value);
                    _kart.ActivateSmokeVFX();
                    break;
                case BonusItem.BonusTypes.Accelerator:
                    if (_kart)
                    {
                        var powerup = new ArcadeKart.StatPowerup();
                        powerup.PowerUpID = BonusItem.BonusTypes.Accelerator.ToString();
                        powerup.MaxTime = bonusItem.Duration;
                        powerup.modifiers = new ArcadeKart.Stats
                        {
                            TopSpeed = bonusItem.Value,
                            Acceleration = bonusItem.Value / 2
                        };
                        _kart.AddPowerup(powerup);
                        _kart.IgnoreSpeedLimit.Activate(bonusItem.Duration);
                    }
                    break;
                case BonusItem.BonusTypes.Decelerator:
                    if (_kart)
                    {
                        var powerup = new ArcadeKart.StatPowerup();
                        powerup.PowerUpID = BonusItem.BonusTypes.Decelerator.ToString();
                        powerup.MaxTime = bonusItem.Duration;
                        powerup.modifiers = new ArcadeKart.Stats
                        {
                            TopSpeed = -bonusItem.Value,
                        };
                        _kart.AddPowerup(powerup);
                    }
                    break;
                case BonusItem.BonusTypes.Shield:
                    if (_shieldRemoveCoroutine != null)
                        StopCoroutine(_shieldRemoveCoroutine);
                    _isShielded = true;
                    ActivateShieldVFX(bonusItem.Duration);
                    _shieldRemoveCoroutine = StartCoroutine(RemoveShieldBonusRoutine(bonusItem.Duration));
                    break;
                case BonusItem.BonusTypes.InverseControl:
                    if (_kart)
                    {
                        _kart.ReverseControl.Activate(bonusItem.Duration);
                    }
                    break;
                case BonusItem.BonusTypes.Dynamite:
                    ActivateDynamiteVFX(bonusItem.Duration);
                    Invoke("MovePlayerToStartPosition", bonusItem.Duration);
                    foreach (var ps in GetComponentsInChildren<ParticleSystem>().Where(ps => ps.name.ToLower().Contains("explosion")))
                    {
                        var main = ps.main;
                        main.startDelay = bonusItem.Duration - 0.5f;
                    }
                    break;
                case BonusItem.BonusTypes.Oil:
                    if (_kart)
                    {
                        var powerup = new ArcadeKart.StatPowerup();
                        powerup.PowerUpID = BonusItem.BonusTypes.Oil.ToString();
                        powerup.MaxTime = bonusItem.Duration;
                        powerup.modifiers = new ArcadeKart.Stats
                        {
                            SidewayExtremumSlip = bonusItem.Value,
                        };
                        _kart.AddPowerup(powerup);
                    }
                    break;
                case BonusItem.BonusTypes.Tornado:
                    if (_kart)
                    {
                        var playerOffset = 10f;
                        var boundaryOffset = 10f;
                        Vector3 targetPos;
                        do
                        {
                            targetPos = GetRandomTerrainPosition(boundaryOffset);
                        }
                        while (Vector3.Distance(_kart.transform.position, targetPos) < playerOffset);
                        StartCoroutine(TornadoMoveRoutine(targetPos, bonusItem.Duration));
                        _isInTornado = true;
                        ActivateTornadoVFX(bonusItem.Duration);
                        StartCoroutine(RemoveTornadoBonusRoutine(bonusItem.Duration));
                    }
                    break;
                default: break;
            }
    }

    private BonusItem GetRandomBonus(bool isBad)
    {
        if (!_bonusContainer || _bonusContainer.BonusItems.Count == 0)
            return null;
        BonusItem bonus;
        do
        {
            bonus = _bonusContainer.BonusItems[UnityEngine.Random.Range(0, _bonusContainer.BonusItems.Count)];
        } while (bonus.IsBad != isBad || bonus.BonusType == BonusItem.BonusTypes.GoodRandom || bonus.BonusType == BonusItem.BonusTypes.BadRandom);
        return bonus;
    }

    #region VFX Methods
    private void ActivateDynamiteVFX(float duration)
    {
        if (_dynamiteVFXObject)
        {
            //Cancel delayed destroy and destroy now instead
            Destroy(_dynamiteVFXObject);
            CancelInvoke("DestroyDynamiteVFX");
        }
        _dynamiteVFXObject = Instantiate(DynamiteVFX, _kart.transform);
        Invoke("DestroyDynamiteVFX", duration);
    }

    private void DestroyDynamiteVFX()
    {
        Destroy(_dynamiteVFXObject);
    }

    private void ActivateShieldVFX(float duration)
    {
        if (_shieldVFXObject)
        {
            //Cancel delayed destroy and destroy now instead
            Destroy(_shieldVFXObject);
            CancelInvoke("DestroyShieldVFX");
        }
        _shieldVFXObject = Instantiate(ShieldVFX, transform);
        Invoke("DestroyShieldVFX", duration);
    }

    private void DestroyShieldVFX()
    {
        Destroy(_shieldVFXObject);
    }

    private void ActivateTornadoVFX(float duration)
    {
        if (_tornadoVFXObject)
        {
            //Cancel delayed destroy and destroy now instead
            Destroy(_tornadoVFXObject);
            CancelInvoke("DestroyTornadoVFX");
        }
        _tornadoVFXObject = Instantiate(TornadoVFX, transform);
        Invoke("DestroyTornadoVFX", duration);
    }

    private void DestroyTornadoVFX()
    {
        Destroy(_tornadoVFXObject);
    }
    #endregion

    #region Coroutines
    private IEnumerator TornadoMoveRoutine(Vector3 targetPositionRaw, float duration)
    {
        float time = 0;
        float rotationSpeed = 3f;
        float riseForce = 8f;
        Vector3 startPositionRaw = _kart.transform.position;
        bool gravityEnabled = false;
        _kart.GetComponent<Rigidbody>().useGravity = false;
        SetTornadoComponentsState(false);
        while (time < duration)
        {
            //Apply rotation
            _kart.transform.Rotate(Vector3.up * rotationSpeed, Space.World);
            _kart.transform.rotation = Quaternion.Euler(0, _kart.transform.rotation.eulerAngles.y, 0);
            //Apply rise
            if (time < duration * 0.2f)
            {
                _kart.transform.position = new Vector3(_kart.transform.position.x, _kart.transform.position.y + riseForce * Time.deltaTime,
                    _kart.transform.position.z);
            }
            Vector3 startPosition = new Vector3(startPositionRaw.x, _kart.transform.position.y, startPositionRaw.z);
            Vector3 targetPosition = new Vector3(targetPositionRaw.x, _kart.transform.position.y, targetPositionRaw.z);
            _kart.transform.position = Vector3.Lerp(startPosition, targetPosition, time / duration);
            if (!gravityEnabled && time > duration * 0.6f)
            {
                _kart.GetComponent<Rigidbody>().useGravity = true;
                gravityEnabled = true;
            }
            time += Time.deltaTime;
            yield return null;
        }
        SetTornadoComponentsState(true);
        _kart.transform.position = targetPositionRaw;
    }

    private IEnumerator RemoveShieldBonusRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        _isShielded = false;
    }

    private IEnumerator RemoveTornadoBonusRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        _isInTornado = false;
    }

    #endregion

    private void MovePlayerToStartPosition()
    {
        _kart.transform.position = _startPosition;
        _kart.transform.rotation = _startRotation;
    }

    private Vector3 GetRandomTerrainPosition(float boundaryOffset)
    {
        var randomPos = new Vector3(Random.Range(_terrainBoundaries.x + boundaryOffset, _terrainBoundaries.z - boundaryOffset), 0,
        Random.Range(_terrainBoundaries.y + boundaryOffset, _terrainBoundaries.w - boundaryOffset));
        return randomPos;
    }

    private void SetTornadoComponentsState(bool enable)
    {
        _kart.enabled = enable;
        _cutter.enabled = enable;
        SetAudioState(enable);
    }

    private void SetAudioState(bool enable)
    {
        _tornadoAudioSource.enabled = !enable;
        //Set state for each kart audio
        foreach (var audioSource in _kart.GetComponentInChildren<ArcadeEngineAudio>().GetComponentsInChildren<AudioSource>())
        {
            //Do not reset engine start audio
            if (!audioSource.name.ToLower().Contains("start"))
            {
                audioSource.enabled = enable;
            }
        }
    }
}
