using System;
using UnityEngine;

public class GrassCutter : MonoBehaviour
{
	[SerializeField]
	float _radius = 1.5f;
	[SerializeField]
	float _maxRadius = 3f;
    [SerializeField]
    GameObject _model;

    GrassArea[] _areas = null;

    float _bonusStart = 0;
    float _bonusDuration = 1;
    Vector3 _modelScale = Vector3.one;
    int _zeroPrototypeCutDown = 0;
    int _zeroPrototypeTotal = 0;

    internal int TreesCutDownLastFrame { get; private set; }
    internal int TreesTotal { get; private set; }
    internal int TreesCutDownTotal { get; private set; }

    internal Color TreesLastColor { get; private set; } = Color.white;

    internal bool IsBonusActive => _bonusStart > 0;
    internal float Radius => IsBonusActive ? _maxRadius : _radius;

    public int ZeroPrototypeCutDown { get => _zeroPrototypeCutDown; }
    public int ZeroPrototypeTotal { get => _zeroPrototypeTotal; }

    private void Awake()
	{
        _areas = FindObjectsOfType<GrassArea>();
    }

	void Start()
    {
        if (_model)
            _modelScale = _model.transform.localScale;
    }

    private void OnEnable()
    {
        LevelLoader.AfterLoad += AfterLevelLoad;
    }

    private void OnDisable()
    {
        LevelLoader.AfterLoad -= AfterLevelLoad;
    }


    private void AfterLevelLoad(LevelData levelData)
    {
        TreesTotal = 0;
        foreach (var a in _areas)
            TreesTotal += a.prepare(_maxRadius, ref _zeroPrototypeTotal);
        TreesCutDownLastFrame = TreesCutDownTotal = 0;
    }
    internal void applySizeBonus(float duration)
    {
        _bonusStart = Time.time;
        _bonusDuration = duration;
        if (_model)
            _model.transform.localScale = _modelScale * _maxRadius / _radius;
    }

    private void releaseBonusSize()
    {
        if (_model)
            _model.transform.localScale = _modelScale;
        _bonusStart = 0;
    }

    void Update()
    {
        if (_bonusStart > 0 && Time.time - _bonusStart > _bonusDuration)
            releaseBonusSize();

        TreesCutDownLastFrame = 0;
        ColorAccumulator ca = new ColorAccumulator();
        float r = Radius;
        foreach (var a in _areas)
			TreesCutDownLastFrame += a.cutTrees(transform.position.to2D(), r, ref ca, ref _zeroPrototypeCutDown);

        TreesCutDownTotal += TreesCutDownLastFrame;
        if (TreesCutDownLastFrame > 0)
            TreesLastColor = ca.AsColor;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
	{
		UnityEditor.Handles.color = Color.yellow;
		UnityEditor.Handles.DrawWireArc(transform.position, Vector3.up, transform.forward, 360, Radius);
		UnityEditor.Handles.color = Color.green;
		UnityEditor.Handles.DrawWireArc(transform.position, Vector3.up, transform.forward, 360, _maxRadius);
	}
#endif
}
