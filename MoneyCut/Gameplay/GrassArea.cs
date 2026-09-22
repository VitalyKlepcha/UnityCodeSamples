using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[RequireComponent(typeof(Terrain))]
public class GrassArea : MonoBehaviour
{
	[SerializeField, Range(0.0f, 2.0f)]
	float _trimmedWidth = 0.5f;
	[SerializeField, Range(0.0f, 1.0f)]
	float _trimmedHeight = 0.5f;
	[SerializeField]
	GameObject _cutEffect;
	[SerializeField, Min(0.1f)]
	float _cutEffectLifeTime = 2.0f;
	[SerializeField]
	float _cutEffectHeight = 0.6f;
	[SerializeField, Min(2)]
	int _effectsCacheSize = 32;
	Terrain _terrain;
	TerrainData _data;

	GrassSquare[] _squares = null;
	int _rowStride = 1;
	float _cellSize = 1;

	internal Terrain Terrain => _terrain;

	GrassEffectsCache _effects;

#if UNITY_EDITOR
	TreeInstance[] _storedTrees = null;
#endif

	private void Awake()
	{
		_terrain = GetComponent<Terrain>();
		if (_cutEffect)
			_effects = new GrassEffectsCache(_effectsCacheSize, _cutEffect, _cutEffectLifeTime);
	}
    private void OnEnable()
    {
		LevelLoader.AfterLoad += AfterLevelLoad;
    }

    private void OnDisable()
    {
		LevelLoader.AfterLoad -= AfterLevelLoad;
	}

    private void OnDestroy()
	{
#if UNITY_EDITOR
		if (Application.isPlaying && _storedTrees != null)
			_data.treeInstances = _storedTrees;
#endif
	}

	private void Update()
	{
		_effects.update();
	}

	private void AfterLevelLoad(LevelData levelData)
    {
		_data = _terrain.terrainData;
		Debug.Log("AfterLevelLoad data: " + _data);
#if UNITY_EDITOR
		if (Application.isPlaying)
			_storedTrees = _data.treeInstances;
#endif
	}

	internal float CellsSize
	{
		get => _cellSize;
	}

	public int prepare(float bladeSize, ref int zeroPrototypeTotal)
	{
		float sz = bladeSize * 4;

		float w = _data.size.x;
		float h = _data.size.z;
		int dx = Mathf.FloorToInt(w / sz);
		_cellSize = Mathf.RoundToInt(w / dx);
		_rowStride = Mathf.RoundToInt(w / _cellSize);
		_squares = new GrassSquare[_rowStride * _rowStride];

		var trees = _data.treeInstances;
		var terrPos = _terrain.GetPosition().to2D();
		for (int i = 0, count = trees.Length; i < count; i++)
		{
			var t = trees[i];
			Vector2 pos = new Vector2(terrPos.x + t.position.x * w, terrPos.y + t.position.z * h);
			GrassSquare sq = getSquare(pos, true);
			sq.addTree(pos, i);
		}
		zeroPrototypeTotal = trees.Where(tr => tr.prototypeIndex == 0).Count();
		return trees.Length;
	}

	private GrassSquare getSquare(Vector2 pos, bool createIfNo)
	{
		Vector2 diff = pos - _terrain.GetPosition().to2D();
		int x = Mathf.FloorToInt(diff.x / _cellSize);
		int y = Mathf.FloorToInt(diff.y / _cellSize);
		if (x >= _rowStride)
			x = _rowStride - 1;
		if (y >= _rowStride)
			y = _rowStride - 1;
		int idx = _rowStride * y + x;

		if (createIfNo && _squares[idx] == null)
		{
			_squares[idx] = new GrassSquare(_terrain.GetPosition().to2D(), x, y, _cellSize);
		}
		return _squares[idx];
	}

	internal Rect TerrainRect
	{
		get
		{
			var trPos = _terrain.GetPosition().to2D();
			return new Rect(trPos, _data.size.to2D());
		}
	}
    internal bool overlaps(Vector2 pos, float radius)
	{
		var rc = new Rect(pos.x - radius, pos.y - radius, radius * 2, radius * 2);
		return TerrainRect.Overlaps(rc);
	}

	internal int cutTrees(Vector2 pos, float radius, ref ColorAccumulator accum, ref int zeroPrototypeCount)
	{
		var rc = new Rect(pos.x - radius, pos.y - radius, radius * 2, radius * 2);
		if (_data == null || !TerrainRect.Overlaps(rc))
			return 0;

		var intersect = TerrainRect.intersect(rc);
		List<GrassSquare> sq = new List<GrassSquare>();
		var seg = getSquare(new Vector2(intersect.xMin, intersect.yMin), false);
		int total = 0;
		if (seg != null)
		{
			total += cutTrees(seg, pos, radius, ref accum, ref zeroPrototypeCount);
			sq.Add(seg);
		}

		seg = getSquare(new Vector2(intersect.xMax, intersect.yMin), false);
		if (seg != null && !sq.Contains(seg))
		{
			total += cutTrees(seg, pos, radius, ref accum, ref zeroPrototypeCount);
			sq.Add(seg);
		}

		seg = getSquare(new Vector2(intersect.xMax, intersect.yMax), false);
		if (seg != null && !sq.Contains(seg))
		{
			total += cutTrees(seg, pos, radius, ref accum, ref zeroPrototypeCount);
			sq.Add(seg);
		}

		seg = getSquare(new Vector2(intersect.xMin, intersect.yMax), false);
		if (seg != null && !sq.Contains(seg))
		{
			total += cutTrees(seg, pos, radius, ref accum, ref zeroPrototypeCount);
			sq.Add(seg);
		}

#if UNITY_EDITOR
		segForGizmo = sq;
#endif

		return total;
	}

	int[] cache = new int[100];
	private int cutTrees(GrassSquare seg, Vector2 pos, float radius, ref ColorAccumulator accum, ref int zeroPrototypeCount)
	{
		int total = 0;
		int count = seg.cut(pos, radius, cache);
		while (count > 0)
		{
			total += count;
			for (int i = 0; i < count; i++)
			{
				cutTree(cache[i], ref accum , ref zeroPrototypeCount);
			}

			if (count < cache.Length)
				break;
			count = seg.cut(pos, radius, cache);
		}
		if (seg.IsEmpty)
			removeSquare(seg);
		return total;
	}

	private void cutTree(int idx, ref ColorAccumulator accum, ref int zeroPrototypeCount)
	{
		TreeInstance tree = _data.GetTreeInstance(idx);
		tree.heightScale = _trimmedHeight;
		tree.widthScale = _trimmedWidth;
		if (tree.prototypeIndex == 0)
			zeroPrototypeCount++;
		if (_effects != null)
		{
			Vector3 pos = _terrain.GetPosition() + new Vector3(tree.position.x * _data.size.x, tree.position.y * _data.size.y + _cutEffectHeight, tree.position.z * _data.size.z);
			accum.add(tree.color);
			_effects.launchEffect(pos, tree.color);
		}

		_data.SetTreeInstance(idx, tree);
	}

	private void removeSquare(GrassSquare seg)
	{
		for (int i = 0, count = _squares.Length; i < count; i++)
		{
			if (_squares[i] == seg)
			{
				_squares[i] = null;
				break;
			}
		}
	}

#if UNITY_EDITOR
	List<GrassSquare> segForGizmo = null;
	private void OnDrawGizmos()
	{
		if (_terrain == null || segForGizmo == null)
			return;
		Gizmos.color = Color.red;
		foreach (var seg in segForGizmo)
		{
			Vector3 p0 = new Vector3(seg.Square.xMin, 0, seg.Square.yMin);
			Vector3 p1 = new Vector3(seg.Square.xMax, 0, seg.Square.yMin);
			Vector3 p2 = new Vector3(seg.Square.xMax, 0, seg.Square.yMax);
			Vector3 p3 = new Vector3(seg.Square.xMin, 0, seg.Square.yMax);

			Gizmos.DrawLine(p0, p1);
			Gizmos.DrawLine(p1, p2);
			Gizmos.DrawLine(p2, p3);
			Gizmos.DrawLine(p3, p0);
		}
	}
#endif
}

internal class GrassSquare
{
	internal Rect Square { get; private set; }
	internal float SideSize => Square.width;

	List<TreeData> _rawData = null;
	TreeData[] _trees = null;
	int _count = 0;

	internal GrassSquare(Vector2 origin, int x, int y, float size)
	{
		Square = new Rect(origin.x + size * x, origin.y + size * y, size, size);
		_rawData = new List<TreeData>();
	}

	internal void addTree(Vector2 pos, int i)
	{
		if (_rawData != null)
			_rawData.Add(new TreeData { position = pos, index = i });
	}

	internal void prepare()
	{
		if (_rawData != null)
		{
			_trees = _rawData.ToArray();
			_rawData = null;
			_count = _trees.Length;
		}
	}

	internal int cut(Vector2 pos, float radius, int[] result)
	{
		prepare();
		int idx = 0;
		float sqR = radius * radius;
		for (int i = 0; i < _count; )
		{
			TreeData tree = _trees[i];
			if ((tree.position - pos).sqrMagnitude <= sqR)
			{
				result[idx++] = tree.index;
				removeAt(i);
				if (idx >= result.Length)
					return idx;
			}
			else
			{
				i++;
			}
		}
		return idx;
	}

	private void removeAt(int idx)
	{
		int last = _count - 1;
		if (idx < last)
			_trees[idx] = _trees[last];
		_count--;
	}

	internal int Count => _count;
	internal bool IsEmpty => _count == 0;
}

internal struct TreeData
{
	internal Vector2 position;
	internal int index;
}

internal struct ColorAccumulator
{
	internal int r;
	internal int g;
	internal int b;
	internal int a;
	internal int count;

	internal void add(Color clr)
	{
		add((Color32)clr);
	}

	internal void add(Color32 clr)
	{
		count++;
		r += clr.r;
		g += clr.g;
		b += clr.b;
		a += clr.a;
	}

	internal Color32 AsColor32
	{
		get
		{
			if (count > 0)
			{
				int dr = Mathf.Clamp(r / count, 0, 0xff);
				int dg = Mathf.Clamp(g / count, 0, 0xff);
				int db = Mathf.Clamp(b / count, 0, 0xff);
				int da = Mathf.Clamp(a / count, 0, 0xff);
				return new Color32((byte)dr, (byte)dg, (byte)db, (byte)da);
			}
			return Color.white;
		}
	}

	internal Color AsColor => count > 0 ? (Color)AsColor32 : Color.white;
}