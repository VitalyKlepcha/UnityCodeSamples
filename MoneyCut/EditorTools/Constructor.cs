using UnityEngine;
using System;
using UnityEngine.UI;
using TMPro;
using Zenject;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MoneyCut.Constructor
{
    public class Constructor : MonoBehaviour
    {

#if UNITY_EDITOR
        [SerializeField]
        private Terrain _terrain;
        [SerializeField]
        private GrassArea _grassArea;
        [SerializeField]
        private Transform _bonusContainer;
        [SerializeField]
        private ConstructorWarnings _warnings;
        [SerializeField]
        private Transform _player;

        [Header("Input Fields")]
        [SerializeField]
        private TMP_Dropdown _levelTypeDropDown;
        [SerializeField]
        private TMP_InputField _levelNumberInputField;
        [SerializeField]
        private TMP_InputField _timeInput;
        [SerializeField]
        private TMP_InputField _grassPercentInput;
        [SerializeField]
        private TMP_InputField _grassNameInput;
        [SerializeField]
        private TMP_InputField _treesOnPixelInput;
        [SerializeField]
        private TMP_InputField _pixelStepInput;
        [SerializeField]
        private TMP_InputField _widthMinInput;
        [SerializeField]
        private TMP_InputField _widthMaxInput;
        [SerializeField]
        private TMP_InputField _heightMinInput;
        [SerializeField]
        private TMP_InputField _heightMaxInput;
        [SerializeField]
        private Toggle _useSpecificGrassToggle;
        [SerializeField]
        private TMP_InputField _specificColorHexInput;
        [SerializeField]
        private Image _terrainBrowseImage;
        [SerializeField]
        private Image _terrainLayerBrowseImage;

        private LevelType _levelType = 0;
        private LevelBonusData _levelBonusData = new LevelBonusData();

        public event Action OnImportButtonClicked;
        public event Action<int> OnTreesOnPixelValueChange;
        public event Action<int> OnPixelSteplValueChange;
        public event Action<float> OnWidthMinValueChange;
        public event Action<float> OnWidthMaxValueChange;
        public event Action<float> OnHeightMinValueChange;
        public event Action<float> OnHeightMaxValueChange;
        public event Action<bool> OnUseSpecificColorChange;
        public event Action<string> OnSpecificColorHexEndEdit;
        public delegate string GetFilePath();
        public GetFilePath OnBrowseButtonClicked;

        private string _pathToLayer;
        private TerrainLayer _terrainLayer;
        //If layer is created this file would be saved in project assets
        private Texture2D _layerTexture;

        private bool _terrainImported;
        private bool _terrainImportedWithSpecGrass;

        IRemoteDatabase _remoteDatabase;

        [Inject]
        private DiContainer _diContainer;

        [Inject(Id = "PopUpCanvas")]
        private Canvas _popUpCanvas;

        [Inject]
        private void Setup(IRemoteDatabase remoteDatabase)
        {
            _remoteDatabase = remoteDatabase;
        }

        private void Awake()
        {
            if (_grassArea)
            {
                Editor.CreateEditor(_grassArea);
            }
            AddListeners();
            LoadPreferences();
        }

        void Start()
        {
            CreateAndAssignTerrainDataClone();
        }


        private void AddListeners()
        {
            _levelTypeDropDown.onValueChanged.AddListener(OnLevelTypeDropDownValueChange);
            _treesOnPixelInput.onValueChanged.AddListener(TreesOnPixelValueChange);
            _pixelStepInput.onValueChanged.AddListener(PixelStepValueChange);
            _widthMinInput.onValueChanged.AddListener(WidthMinValueChange);
            _widthMaxInput.onValueChanged.AddListener(WidthMaxValueChange);
            _heightMinInput.onValueChanged.AddListener(HeightMinValueChange);
            _heightMaxInput.onValueChanged.AddListener(HeightMaxValueChange);
            _useSpecificGrassToggle.onValueChanged.AddListener(UseSpecificColorValueChange);
            _specificColorHexInput.onEndEdit.AddListener(SpecificColorHexValueEndEdit);
        }

        private void LoadPreferences()
        {
            _treesOnPixelInput.text = EditorPrefs.GetInt("Trees\\NumberOnPixel", 1).ToString();
            _pixelStepInput.text = EditorPrefs.GetInt("Trees\\PixelStep", 1).ToString(); ;
            _widthMinInput.text = EditorPrefs.GetFloat("Trees\\WidthMin", 0.9f).ToString();
            _widthMaxInput.text = EditorPrefs.GetFloat("Trees\\WidthMax", 1f).ToString();
            _heightMinInput.text = EditorPrefs.GetFloat("Trees\\HeightMin", 0.9f).ToString();
            _heightMaxInput.text = EditorPrefs.GetFloat("Trees\\HeightMax", 1.1f).ToString();
        }

        /// <summary>
        /// Creates a copy out of current terrainData
        /// </summary>
        private void CreateAndAssignTerrainDataClone()
        {
            var terrainDataClone = TerrainDataCloner.Clone(_terrain.terrainData);
            CreateLayerClone(terrainDataClone);
            AssetDatabase.CreateAsset(terrainDataClone, "Assets/Resources/Terrains/Editor/Temp/TempTerrain.asset");
            AssetDatabase.SaveAssets();
            _terrain.terrainData = terrainDataClone;
            _terrain.GetComponent<TerrainCollider>().terrainData = terrainDataClone;
        }

        private void CreateAndAssignTerrainDataClone(TerrainData td)
        {
            var terrainDataClone = TerrainDataCloner.Clone(td);
            CreateLayerClone(terrainDataClone);
            AssetDatabase.CreateAsset(terrainDataClone, "Assets/Resources/Terrains/Editor/Temp/TempTerrain.asset");
            AssetDatabase.SaveAssets();
            _terrain.terrainData = terrainDataClone;
            _terrain.GetComponent<TerrainCollider>().terrainData = terrainDataClone;
        }

        private void CreateLayerClone(TerrainData terrainDataClone)
        {
            var layer = new TerrainLayer();
            layer.diffuseTexture = terrainDataClone.terrainLayers[0].diffuseTexture;
            layer.tileSize = new Vector2(64, 64);
            terrainDataClone.terrainLayers = new TerrainLayer[] { layer };
            AssetDatabase.CreateAsset(layer, "Assets/Resources/Terrains/Editor/Temp/TempTerrainLayer.asset");
        }

        /// <summary>
        /// Creates terrainData clone asset with unique name
        /// </summary>
        /// <returns>Created terrainData copy</returns>
        private TerrainData SaveTerrain()
        {
            var newTerrainClone = TerrainDataCloner.Clone(_terrain.terrainData);
            var uniquePath = AssetDatabase.GenerateUniqueAssetPath("Assets/Resources/Terrains/UniqueLevel.asset");
            AssetDatabase.CreateAsset(newTerrainClone, uniquePath);
            return newTerrainClone;
        }


        private TerrainData SaveTerrain(string path)
        {
            var newTerrainClone = TerrainDataCloner.Clone(_terrain.terrainData);
            AssetDatabase.CreateAsset(newTerrainClone, path);
            return newTerrainClone;
        }

        /// <returns>true if terrain has been succesefully loaded</returns>
        private bool LoadTerrain(int num)
        {
            TerrainData td = (TerrainData)AssetDatabase.LoadAssetAtPath(string.Format("Assets/Resources/Terrains/Level {0}.asset", num), typeof(TerrainData));
            if (td == null)
            {
                Debug.LogError("Constructor: Terrain with num " + num + " doesn't exist");
                return false;
            }
            CreateAndAssignTerrainDataClone(td);
            return true;
        }

        public async void LoadLevelData()
        {
            if (!int.TryParse(_levelNumberInputField.text, out int levelNum))
            {
                Debug.LogError("Constructor: Level number is incorrect");
                return;
            }
            this.ShowActivityIndicator(_popUpCanvas.transform);
            LevelData ld = await _remoteDatabase.LoadLevel(levelNum);
            this.HideActivityIndicator();
            if (ld == null)
            {
                Debug.LogError("Constructor: Level with num " + levelNum + " doesn't exist");
                return;
            }
            CreateAndAssignTerrainDataClone();
            _terrain.terrainData.treeInstances = ld.TerrainDetails.Trees.Select(tree => tree.ToCoreTree()).ToArray();
            _terrain.terrainData.terrainLayers[0].diffuseTexture = ld.TerrainDetails.LayerDiffuseTexture;
            _levelTypeDropDown.value = (int)ld.LevelType;
            _grassPercentInput.text = ld.GrassPercentToWin.ToString();
            _grassNameInput.text = ld.SpecificColorName;
            _timeInput.text = ld.LevelTime.ToString();
            _player.position = ld.StartPlayerPosition;
            LoadBonusData(ld);
        }

        private void LoadBonusData(LevelData ld)
        {
            ClearBonuses();
            var fieldItemPopUp = _diContainer.ResolveId<GameObject>("FieldItemPopUp");
            var popUpCanvas = _diContainer.ResolveId<Canvas>("PopUpCanvas");
            foreach (var bonus in ld.Bonuses)
            {
                var constructorItem = Instantiate(bonus.BonusPrefab, bonus.Position, Quaternion.identity, _bonusContainer).AddComponent<ConstructorBonusFieldItem>();
                constructorItem.FieldItemPopUpPrefab = fieldItemPopUp;
                constructorItem.Canvas = popUpCanvas;
                constructorItem.BonusPrefab = bonus.BonusPrefab;
                if (bonus.BonusOverridenParameters.ToVector2() != Vector2.zero)
                {
                    constructorItem.OverridenParams = bonus.BonusOverridenParameters;
                }
            }
        }

        private void ClearBonuses()
        {
            foreach (Transform go in _bonusContainer)
            {
                Destroy(go.gameObject);
            }
        }

        public async void SaveLevelData()
        {
            LevelData ld = new LevelData();
            if (!SetLevelDataParameters(ld))
            {
                return;
            }
            if (!int.TryParse(_levelNumberInputField.text, out int levelNum))
            {
                Debug.LogError("Constructor: Level number is incorrect");
                return;
            }
            ld.Num = levelNum;
            ld.Uuid = levelNum.ToString();
            if (await _remoteDatabase.HasLevel(levelNum))
            {
                isReplacing = true;
                if (!EditorUtility.DisplayDialog("Level exist",
                    String.Format("Level {0} is aleready exist. Do you want to override this level?", levelNum), "Yes", "Cancel"))
                {
                    return;
                }
            }
            SaveTerrainData(ld);
            ld.StartPlayerPosition = _player.position;
            SaveBonusData(ld);
            this.ShowActivityIndicator(_popUpCanvas.transform);
            await _remoteDatabase.SaveLevel(ld);
            if (!isReplacing)
            {
                _remoteDatabase.UpdateRemoteLevelCount(1).Forget();
            }
            this.HideActivityIndicator();
        }

        private void SaveTerrainData(LevelData ld)
        {
            if (_terrain.terrainData.treeInstanceCount > 0)
            {
                ld.TerrainDetails.Trees = new TreeInstanceData[_terrain.terrainData.treeInstanceCount];
                for (int i = 0; i < _terrain.terrainData.treeInstanceCount; i++)
                {
                    ld.TerrainDetails.Trees[i] = _terrain.terrainData.treeInstances[i].ToWrappedTree();
                }
            }
            if (_terrain.terrainData.terrainLayers[0] == null)
            {
                Debug.LogError("Constuctor: terrain layer is not set on initial terrain");
                return;
            }
            Texture2D diffuseTexture = _terrain.terrainData.terrainLayers[0].diffuseTexture;
            if (diffuseTexture != null)
            {
                ld.TerrainDetails.LayerDiffuseTexture = diffuseTexture;
            }
        }

        public void SaveTerrainLayer(int levelNum)
        {
            if (_layerTexture == null)
                return;
            byte[] textureBytes = _layerTexture.EncodeToPNG();
            File.WriteAllBytes(string.Format("Assets/Resources/Terrains/Art/Texture {0}.png", levelNum), textureBytes);
            AssetDatabase.Refresh();
            _terrain.terrainData.terrainLayers[0].diffuseTexture = (Texture2D)Resources.Load(string.Format("Terrains/Art/Texture {0}", levelNum), typeof(Texture2D));
        }

        private void SaveBonusData(LevelData ld)
        {
            foreach (var bonus in _bonusContainer.GetComponentsInChildren<BonusItem>())
            {
                LevelBonusData lbd = new LevelBonusData();
                var bonusFieldItem = bonus.GetComponent<ConstructorBonusFieldItem>();
                lbd.BonusPrefab = bonusFieldItem.BonusPrefab;
                lbd.Position = bonus.transform.position;
                lbd.Type = bonus.BonusType;
                if (bonusFieldItem.OverridenParams.ToVector2() != Vector2.zero)
                {
                    lbd.BonusOverridenParameters = bonusFieldItem.OverridenParams;
                }
                ld.Bonuses.Add(lbd);
            }
        }

        /// <returns>true if data has been succesefully set</returns>
        private bool SetLevelDataParameters(LevelData ld)
        {
            ld.LevelType = _levelType;
            switch (ld.LevelType)
            {
                case LevelType.TimeBound:
                    if (!int.TryParse(_timeInput.text, out int levelTime))
                    {
                        Debug.LogError("Constructor: Level time is invalid");
                        return false;
                    }
                    ld.LevelTime = levelTime;
                    break;
                case LevelType.SpecificGrass:
                    if (!int.TryParse(_grassPercentInput.text, out int grassPercent))
                    {
                        Debug.LogError("Constructor: Grass percent is invalid");
                        return false;
                    }
                    ld.GrassPercentToWin = Mathf.Clamp(grassPercent, 1, 100);
                    ld.SpecificColorName = _grassNameInput.text.ToLower();
                    break;
                default: break;
            }
            return true;
        }

        #region Input
        public void BrowseButtonClicked()
        {
            var filePath = OnBrowseButtonClicked?.Invoke();
            var sprite = IMG2Sprite.LoadNewSprite(filePath);
            _terrainBrowseImage.sprite = sprite;
        }

        public void ImportButtonClicked()
        {
            OnImportButtonClicked?.Invoke();
            _terrainImported = true;
            _terrainImportedWithSpecGrass = _useSpecificGrassToggle.isOn;
            if (_terrainImportedWithSpecGrass)
            {
                _warnings.SetSpecGrassWarningState(false);
            }
            else if (_levelType == LevelType.Message || _levelType == LevelType.SpecificGrass)
            {
                _warnings.SetSpecGrassWarningState(true);
            }
        }

        public void LayerBrowseButtonClicked()
        {
            string filePath = EditorUtility.OpenFilePanel("Import from...", _pathToLayer, "png,jpg");
            if (!string.IsNullOrEmpty(filePath))
            {
                _pathToLayer = filePath;
                var sprite = IMG2Sprite.LoadNewSprite(filePath);
                _terrainLayerBrowseImage.sprite = sprite;
            }
        }

        public void CreateLayerButtonClicked()
        {
            if (string.IsNullOrEmpty(_pathToLayer))
            {
                Debug.LogError("Constructor: Layer file is not chosen");
                return;
            }
            _layerTexture = IMG2Sprite.LoadTexture(_pathToLayer);
            _terrain.terrainData.terrainLayers[0].diffuseTexture = _layerTexture;
        }

        public void OnLevelTypeDropDownValueChange(int num)
        {
            _levelType = (LevelType)num;
            if (_levelType == LevelType.Message || _levelType == LevelType.SpecificGrass)
            {
                _useSpecificGrassToggle.isOn = true;
                if (_terrainImported && !_terrainImportedWithSpecGrass)
                {
                    _warnings.SetSpecGrassWarningState(true);
                }
            }
            else
            {
                _warnings.SetSpecGrassWarningState(false);
            }
        }

        public void TreesOnPixelValueChange(string num)
        {
            if (!int.TryParse(num, out int result))
            {
                Debug.LogError("Constructor: Trees on pixel value is invalid");
                return;
            }
            OnTreesOnPixelValueChange?.Invoke(result);
        }
        public void PixelStepValueChange(string num)
        {
            if (!int.TryParse(num, out int result))
            {
                Debug.LogError("Constructor: Pixel step value is invalid");
                return;
            }
            OnPixelSteplValueChange?.Invoke(result);
        }
        public void WidthMinValueChange(string num)
        {
            if (!float.TryParse(num, out float result))
            {
                Debug.LogError("Constructor: Width min value is invalid");
                return;
            }
            OnWidthMinValueChange?.Invoke(result);
        }
        public void WidthMaxValueChange(string num)
        {
            if (!float.TryParse(num, out float result))
            {
                Debug.LogError("Constructor: Width max value is invalid");
                return;
            }
            OnWidthMaxValueChange?.Invoke(result);
        }
        public void HeightMinValueChange(string num)
        {
            if (!float.TryParse(num, out float result))
            {
                Debug.LogError("Constructor: Height min value is invalid");
                return;
            }
            OnHeightMinValueChange?.Invoke(result);
        }
        public void HeightMaxValueChange(string num)
        {
            if (!float.TryParse(num, out float result))
            {
                Debug.LogError("Constructor: Height max value is invalid");
                return;
            }
            OnHeightMaxValueChange?.Invoke(result);
        }

        public void UseSpecificColorValueChange(bool value)
        {
            _specificColorHexInput.transform.parent.gameObject.SetActive(value);
            OnUseSpecificColorChange?.Invoke(value);
        }

        public void SpecificColorHexValueEndEdit(string value)
        {
            OnSpecificColorHexEndEdit?.Invoke(value);
        }

        #endregion
#endif
    }
}