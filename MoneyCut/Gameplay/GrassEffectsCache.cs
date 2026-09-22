using System.Collections.Generic;
using UnityEngine;

internal class GrassEffectsCache
{
	Queue<CutEffectInfo> _cache;

	internal GrassEffectsCache(int size, GameObject proto, float effectLifeTime)
	{
		_cache = new Queue<CutEffectInfo>(size);
		while (_cache.Count < size)
			_cache.Enqueue(new CutEffectInfo(proto, effectLifeTime));
	}

	internal void launchEffect(Vector3 position, Color color)
	{
		var eff = _cache.Dequeue();
		eff.restart(position, color);
		_cache.Enqueue(eff);
	}

	internal void update()
	{
		foreach (var eff in _cache)
			eff.update();
	}
}

internal class CutEffectInfo
{
	GameObject _effect;
	float _startTime;
	float _lifeTime;

	internal CutEffectInfo(GameObject prefab, float lifeTime)
	{
		_effect = Object.Instantiate(prefab);
		_lifeTime = lifeTime;
		_startTime = 0;
		stop();
	}

	internal bool IsAlive => _effect.activeSelf;

	internal void restart(Vector3 position, Color color)
	{
		_startTime = Time.time;
		_effect.transform.position = position;
		ParticleSystem ps = _effect.GetComponent<ParticleSystem>();
		if (ps)
			restartEffect(ps, color);
		ParticleSystem[] childPS = _effect.GetComponentsInChildren<ParticleSystem>();
		foreach (var child in childPS)
			restartEffect(child, color);
		_effect.SetActive(true);
	}

	internal void update()
	{
		if (_startTime == 0)
			return;
		float life = Time.time - _startTime;
		if (life >= _lifeTime)
		{
			stop();
			_startTime = 0;
		}
	}

	internal void stop()
	{
		_effect.SetActive(false);
	}

	private void restartEffect(ParticleSystem ps, Color color)
	{
		ps.Stop();
		ps.Clear();
		var settings = ps.main;
		settings.startColor = color;
		ps.Play();
	}
}
