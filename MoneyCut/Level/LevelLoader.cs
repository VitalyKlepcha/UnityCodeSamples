using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Zenject;

public class LevelLoader : ILevelLoader
{
    [Inject(Id = "MainTerrain")]
    private Terrain _terrain;

    [Inject(Id = "Player")]
    private Transform _player;

    [Inject(Id = "DefaultDiffuseTexture")]
    private Texture2D _defautDiffuseTexture;

    private IlevelManager _levelManager;
    private IBonusLoader _bonusLoader;

    public static event Action<LevelData> BeforeLoad;
    public static event Action<LevelData> AfterLoad;

    [Inject]
    public void Setup(IBonusLoader bonusLoader, IlevelManager levelManager)
    {
       _bonusLoader = bonusLoader;
        _levelManager = levelManager;
    }

    public async void LoadLevel(int num)
    {
        var levelData = await _levelManager.GetLevel(num);
        BeforeLoad?.Invoke(levelData);
        await _levelManager.SetCurrentLevel(num);
        _terrain.terrainData.treeInstances = levelData.TerrainDetails.Trees.Select(tree=> tree.ToCoreTree()).ToArray();
        if (levelData.TerrainDetails.LayerDiffuseTexture)
        {
            _terrain.terrainData.terrainLayers[0].diffuseTexture = levelData.TerrainDetails.LayerDiffuseTexture;
        }
        else
        {
            if(_defautDiffuseTexture)
                _terrain.terrainData.terrainLayers[0].diffuseTexture = _defautDiffuseTexture;
        }
        _bonusLoader.ReloadBonuses(levelData.Bonuses);
        _player.position = levelData.StartPlayerPosition;
        AfterLoad?.Invoke(levelData);
    }
}