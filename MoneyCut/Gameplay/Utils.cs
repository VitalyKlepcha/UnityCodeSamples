using System.Linq;
using System.Collections.Generic;
using UnityEngine;

internal static class Utils
{
	internal static void destroy(Object obj, float time = -1)
	{
		if (time < 0)
		{
			Object.Destroy(obj);
		}
		else
		{
			Object.Destroy(obj, time);
		}
	}

	internal class TerrainInfo
	{
		internal int[] layers = null;
	}
	static Dictionary<Terrain, TerrainInfo> _terrains = null;

	public static List<Terrain> detectTerrain(Vector3 origin, float radius = 10.0f)
	{
		List<Terrain> terrainList = new List<Terrain>();
		checkTerrainsCache();
		Rect rc = new Rect(origin.x - radius, origin.z - radius, radius * 2, radius * 2);
		foreach (var terr in _terrains.Keys)
		{
			Vector3 pos = terr.GetPosition();
			Rect terrRc = new Rect(pos.x, pos.z, terr.terrainData.size.x, terr.terrainData.size.z);
			if (terrRc.Overlaps(rc))
				terrainList.Add(terr);
		}
		return terrainList;
	}

	public static List<Terrain> detectTerrain(GameObject go, float radius = 10.0f)
	{
		Vector3 origin = go.transform.position;
		return detectTerrain(origin, radius);
	}

	public static Vector3 getPositionOnTerrain(Terrain terrain, int x, int z, float multiplierX, float multiplierZ)
	{
		Vector3 terrainPos = terrain.GetPosition();
		float worldX = x / multiplierX + terrainPos.x;
		float worldZ = z / multiplierZ + terrainPos.z;
		float worldY = terrain.SampleHeight(new Vector3(worldX, terrainPos.y + 10, worldZ)) + terrainPos.y;
		return new Vector3(worldX, worldY, worldZ);
	}

	public static float getHeightOnTerrain(Terrain terrain, Vector3 worldPos)
	{
		float worldY = terrain.SampleHeight(worldPos) + terrain.GetPosition().y;
		return worldY;
	}

	internal static int[] getTerrainLayers(Terrain terrain)
	{
		TerrainInfo info = getTerrainInfo(terrain);
		return info.layers;
	}

	static TerrainInfo getTerrainInfo(Terrain terrain)
	{
		if (_terrains != null && !_terrains.ContainsKey(terrain))
			_terrains = null;

		if (_terrains == null)
			fillTerrainsCache();

		if (_terrains.TryGetValue(terrain, out TerrainInfo trInfo))
			return trInfo;

		Debug.LogAssertion($"Unknown terrain {terrain.name}");
		return null;
	}

	internal static void checkTerrainsCache()
	{
		if (_terrains != null)
		{
			if (_terrains.Keys.Count == 0)
				return;
			Terrain terr = _terrains.Keys.First();
			if (terr)
				return;
		}
		fillTerrainsCache();
	}

	private static void fillTerrainsCache()
	{
		_terrains = new Dictionary<Terrain, TerrainInfo>();
		var terrs = Object.FindObjectsOfType<Terrain>();
		foreach (var tr in terrs)
		{
			TerrainInfo info = makeTerrainInfo(tr);
			_terrains[tr] = info;
		}
	}

	static private TerrainInfo makeTerrainInfo(Terrain tr)
	{
		var data = tr.terrainData;
		var layers = data.GetSupportedLayers(0, 0, data.detailWidth, data.detailHeight);
		return new TerrainInfo { layers = layers };
	}

	internal static Vector2 to2D(this Vector3 v)
	{
		return new Vector2(v.x, v.z);
	}

	internal static Vector3 to3D(this Vector2 v)
	{
		return new Vector3(v.x, 0, v.y);
	}

	internal static Rect intersect(this Rect r1, Rect r2)
	{
		float xMin = Mathf.Max(r1.xMin, r2.xMin);
		float xMax = Mathf.Min(r1.xMax, r2.xMax);
		float yMin = Mathf.Max(r1.yMin, r2.yMin);
		float yMax = Mathf.Min(r1.yMax, r2.yMax);

		return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
	}
}
