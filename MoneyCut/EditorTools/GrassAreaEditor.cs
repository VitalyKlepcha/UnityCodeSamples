using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using MoneyCut.Constructor;

[CustomEditor(typeof(GrassArea))]
public class GrassAreaEditor : Editor
{
		GrassArea Target => target as GrassArea;
	Terrain _terrain;

	StandardPropertiesList _props = null;

	PrefsValue<bool> _treesPanelOpened = new PrefsValue<bool>("Trees\\PanelOpened");
	PrefsValue<int> _treesImageSize = new PrefsValue<int>("Trees\\ImageSize", 128);
	PrefsValue<string> _treesFile = new PrefsValue<string>("Trees\\FileName", "Trees.png");
	PrefsValue<float> _treeHeightMin = new PrefsValue<float>("Trees\\HeightMin", 0.9f);
	PrefsValue<float> _treeHeightMax = new PrefsValue<float>("Trees\\HeightMax", 1.1f);
	PrefsValue<float> _treeWidthMin = new PrefsValue<float>("Trees\\WidthMin", 0.9f);
	PrefsValue<float> _treeWidthMax = new PrefsValue<float>("Trees\\WidthMax", 1f);

	PrefsValue<int> _treesPixelStep = new PrefsValue<int>("Trees\\PixelStep", 1);
	PrefsValue<int> _treesNumberOnPixel = new PrefsValue<int>("Trees\\NumberOnPixel", 1);
	PrefsValue<float> _treesPositionDispersion = new PrefsValue<float>("Trees\\PositionDispersion", 0.5f);
	PrefsValue<bool> _treesKeepOthers = new PrefsValue<bool>("Trees\\KeepOthers", true);
	PrefsValue<string> _treesProtosStr = new PrefsValue<string>("Trees\\ProtosStr", "");

	bool _specificGrassOnColor = false;
	Color _specificGrassColor = Color.red;

	static bool isInitialized;

    void Awake()
	{
		if (isInitialized)
			return;
		isInitialized = true;
		var constructor = FindObjectOfType<Constructor>();
		if (constructor)
		{
			constructor.OnImportButtonClicked += importTrees;
			constructor.OnBrowseButtonClicked += ChangeTreeFile;
			constructor.OnTreesOnPixelValueChange += (int value) => _treesNumberOnPixel.Value = value;
			constructor.OnPixelSteplValueChange += (int value) => _treesPixelStep.Value = value;
			constructor.OnWidthMinValueChange += (float value) => _treeWidthMin.Value = value;
			constructor.OnWidthMaxValueChange += (float value) => _treeWidthMax.Value = value;
			constructor.OnHeightMinValueChange += (float value) => _treeHeightMin.Value = value;
            constructor.OnHeightMaxValueChange += (float value) => _treeHeightMax.Value = value;
			constructor.OnUseSpecificColorChange += (bool value) => _specificGrassOnColor = value;
			constructor.OnSpecificColorHexEndEdit += (string value) =>
			{
				if (!ColorUtility.TryParseHtmlString(value, out Color newColor))
				{
					Debug.LogError("Constructor: Color hex value is invalid");
					return;
				}
				_specificGrassColor = newColor;
			};
		}
    }

    private void OnEnable()
	{
		if (Target == null)
			return;
		_terrain = Target.GetComponent<Terrain>();
		_props = new StandardPropertiesList(serializedObject
			, "_trimmedWidth"
			, "_trimmedHeight"
			, "_cutEffect"
			, "_cutEffectLifeTime"
			, "_cutEffectHeight"
			, "_effectsCacheSize"
			);
	}

	public override void OnInspectorGUI()
	{
		bool treeExport = false;
		bool treeImport = false;
		bool treeImportBrowse = false;
		Rect rect;
		bool hasProtos = false;

		EditorGUILayout.BeginVertical();
		{
			_props.showEditors();
			EditorGUILayout.Space();

			_treesPanelOpened.Value = EditorGUILayout.BeginFoldoutHeaderGroup(_treesPanelOpened.Value, "Import/Export");
			if (_treesPanelOpened.Value)
			{
				rect = EditorGUILayout.BeginVertical();
				{
					GUI.Box(rect, "");
					EditorGUILayout.LabelField("Export", EditorStyles.boldLabel);
					EditorGUILayout.BeginHorizontal();
					{
						_treesImageSize.showEditor("Image size");
						treeExport = GUILayout.Button("Export to...", EditorStyles.miniButtonRight);
					}
					EditorGUILayout.EndHorizontal();
					EditorGUILayout.Space();
				}
				EditorGUILayout.EndVertical();

				EditorGUILayout.Space();

				rect = EditorGUILayout.BeginVertical();
				{
					GUI.Box(rect, "");
					EditorGUILayout.LabelField("Import", EditorStyles.boldLabel);
					EditorGUILayout.BeginHorizontal();
					{
						_treesFile.showEditor("Image file");
						treeImportBrowse = GUILayout.Button(" ... ", EditorStyles.miniButtonRight, GUILayout.ExpandWidth(false));
					}
					EditorGUILayout.EndHorizontal();
					EditorGUILayout.BeginHorizontal();
					{
						_treeHeightMin.showEditor("Height scale, min.");
						GUILayout.Label("max.");
						_treeHeightMax.showEditor();
					}
					EditorGUILayout.EndHorizontal();
					EditorGUILayout.BeginHorizontal();
					{
						_treeWidthMin.showEditor("Width scale, min.");
						GUILayout.Label("max.");
						_treeWidthMax.showEditor();
					}
					EditorGUILayout.EndHorizontal();

					_treesPixelStep.showEditor("Pixel step");
					_treesNumberOnPixel.showEditor("Trees on pixel");
					_treesPositionDispersion.showEditor("Position dispersion");


					Rect rc = EditorGUILayout.BeginVertical();
					GUI.Box(rc, "");
					EditorGUILayout.LabelField("Use trees", EditorStyles.boldLabel);

					var td = _terrain.terrainData;
					string str = _treesProtosStr.Value ?? "";
					while (str.Length < td.treePrototypes.Length)
						str += "0";
					if (str.Length > td.treePrototypes.Length)
						str = str.Substring(0, td.treePrototypes.Length);

					char[] protosMarks = str.ToCharArray();
					EditorGUI.indentLevel++;
					for (int i = 0; i < td.treePrototypes.Length; i++)
					{
						string title = td.treePrototypes[i].prefab ? td.treePrototypes[i].prefab.name : "<Missing>";
						bool v = EditorGUILayout.ToggleLeft(title, protosMarks[i] != '0');
						hasProtos |= v;
						protosMarks[i] = v ? '1' : '0';
					}
					_treesProtosStr.Value = new string(protosMarks);
					EditorGUI.indentLevel--;
					EditorGUILayout.Space();
					EditorGUILayout.EndVertical();
					
					_treesKeepOthers.showEditor("Keep other types of trees");
				}
				_specificGrassOnColor = EditorGUILayout.Toggle("Use specific color prototype", _specificGrassOnColor);
				if (_specificGrassOnColor)
				{
					_specificGrassColor = EditorGUILayout.ColorField("Specific color", _specificGrassColor);
					if (_terrain.terrainData.treePrototypes.Length <= 1)
						EditorGUILayout.HelpBox("Terrain must have at least 2 tree prototypes", MessageType.Warning);

				}
				EditorGUILayout.Space();
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField("", GUILayout.ExpandWidth(true)); 
				GUI.enabled = hasProtos;
				treeImport = GUILayout.Button("Import", EditorStyles.miniButtonRight);
				GUI.enabled = true;
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.EndVertical();
			}
			EditorGUILayout.EndFoldoutHeaderGroup();

		}
		EditorGUILayout.EndVertical();

		serializedObject.ApplyModifiedProperties();

		if (treeExport)
			exportTrees();
		if (treeImport)
			importTrees();

		if (treeImportBrowse)
		{
			ChangeTreeFile();
		}
	}

	private void exportTrees()
	{
		var tm = System.DateTime.Now;
		string fn = $"Trees_{tm:HH-mm-ss}.png";
		fn = EditorUtility.SaveFilePanel("Export to...", "", fn, "png");
		if (string.IsNullOrEmpty(fn))
			return;

		TreeInstance[] trees = _terrain.terrainData.treeInstances;
		int size = _treesImageSize.Value;

		Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false, false);
		Color32[] clrs = new Color32[size * size];
		foreach (var t in trees)
		{
			int x = Mathf.FloorToInt(t.position.x * size);
			int y = Mathf.FloorToInt(t.position.z * size);
			try
			{
				clrs[y * size + x] = t.color;
			}
			catch (System.Exception ex)
			{
				Debug.Log($">> ({x}, {y}), {size}, {y * size + x}. {ex}");
			}
		}
		tex.SetPixels32(clrs);

		using (var fs = System.IO.File.Create(fn))
		{
			byte[] texData = tex.EncodeToPNG();
			fs.Write(texData, 0, texData.Length);
			fs.Flush();
		}
	}

	private void importTrees()
	{
		Texture2D tex = new Texture2D(1, 1);
		string fn = _treesFile.Value;
		try
		{
			using (var fs = System.IO.File.OpenRead(fn))
			{
				byte[] texData = new byte[fs.Length];
				fs.Read(texData, 0, texData.Length);
				if (!tex.LoadImage(texData))
					throw new System.Exception($"Failed to load image {fn}");
			}
		}
		catch(System.Exception ex)
		{
			Debug.LogError(ex);
			return;
		}

		var td = _terrain.terrainData;
		List<TreeInstance> trees = new List<TreeInstance>();
		List<int> protoIndexes = new List<int>();
		string protosStr = _treesProtosStr.Value ?? "";
		char[] protos = protosStr.ToCharArray();
		for (int i = 0; i < protos.Length; i++)
		{
			if (protos[i] != '0')
				protoIndexes.Add(i);
		}

		if (_treesKeepOthers.Value)
		{
			foreach (var tr in td.treeInstances)
			{
				if (!protoIndexes.Contains(tr.prototypeIndex))
					trees.Add(tr);
			}
		}

		Color32[] colors = tex.GetPixels32();

		int wi = tex.width;
		int hi = tex.height;
		
		float w = wi;
		float h = hi;

		int treeNum = _treesNumberOnPixel.Value;
		float disp = _treesPositionDispersion.Value;
		int step = _treesPixelStep.Value;
		float dispX = disp * step / w;
		float dispY = disp * step / h;

		float minH = _treeHeightMin.Value;
		float maxH = _treeHeightMax.Value;
		float minW = _treeWidthMin.Value;
		float maxW = _treeWidthMax.Value;

		for (int x = 0; x < wi; x += step)
		{
			for (int y = 0; y < hi; y += step)
			{
				Color32 clr = colors[y * wi + x];
				if (clr.a == 0 || (clr.r + clr.g + clr.b == 0))
					continue;
				Vector3 pos = new Vector3((0.5f + x) / w, 0, (0.5f + y) / h);
				for (int i = 0; i < treeNum; i++)
				{
					Vector3 d = new Vector3(Random.Range(-dispX, dispX), 0, Random.Range(-dispY, dispY));
					TreeInstance t = new TreeInstance { color = clr, position = pos + d };
					t.heightScale = Random.Range(minH, maxH);
					t.widthScale = Random.Range(minW, maxW);
					t.lightmapColor = new Color32(0xff, 0xff, 0xff, 0xff);
					int proto;
					if (_specificGrassOnColor)
					{
						if (clr == _specificGrassColor)
						{
							proto = 0;
						}
						else
						{
							proto = Random.Range(1, protoIndexes.Count);
						}
					}
					else
					{
						proto = Random.Range(0, protoIndexes.Count);
					}
					t.prototypeIndex = protoIndexes[proto];
					t.rotation = Random.value * 2.0f * Mathf.PI;
					trees.Add(t);
				}
			}
		}

		td.SetTreeInstances(trees.ToArray(), true);
		Debug.Log($">> Trees count: {_terrain.terrainData.treeInstanceCount}");
	}

	private string ChangeTreeFile()
    {
		string fn = EditorUtility.OpenFilePanel("Import from...", _treesFile.Value, "png");
		if (!string.IsNullOrEmpty(fn))
		{
			_treesFile.Value = fn;
			return fn;
		}
		return null;

	}
}
