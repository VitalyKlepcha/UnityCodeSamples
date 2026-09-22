using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CollectionItemUIController : MonoBehaviour
{
    #region Variables

    [SerializeField]
    private TextMeshProUGUI _mutualScore;

    [SerializeField]
    private TextMeshProUGUI _mutualScoreCertificate;

    [SerializeField]
    private GameObject _scoreBoxPrefab;

    [SerializeField]
    private Transform _separateScorePanel;

    //Contains Sprite for each ItemType
    private Dictionary<CollectionItemType, Sprite> _itemTypeSprites = new Dictionary<CollectionItemType, Sprite>();

    //Serialized lists which will be converted into _itemTypeSprites Dictionary
    [SerializeField]
    private List<Sprite> _itemTypeSpritesTemp;

    [SerializeField]
    private List<CollectionItemType> _itemTypeTemp;

    private bool _itemTypeSpritesInitialized;

    private Dictionary<CollectionItemType, ScoreBox> _scoreBoxes = new Dictionary<CollectionItemType, ScoreBox>();

    private Animator _mutualScoreAnimator;

    public Dictionary<CollectionItemType, Sprite> ItemTypeSprites { get => _itemTypeSprites; set => _itemTypeSprites = value; }

    #endregion

    #region Methods

    void Start()
    {
        if( _itemTypeSpritesInitialized == false)
        {
            InitializeItemTypeSpritesDictionary();
        }
        _mutualScoreAnimator = _mutualScore.GetComponent<Animator>(); 
    }

    public void UpdateUIScore(int mutualScore, Dictionary<CollectionItemType, int> scores, CollectionItemType collectedItem)
    {
        //Update mutual score
        UpdateUIScore(mutualScore);

        if (_scoreBoxes.ContainsKey(collectedItem))
        {
            //Update score of specific collected item
            var collectedItemScoreBox = _scoreBoxes[collectedItem];
            collectedItemScoreBox.Text.text = scores[collectedItem].ToString();
            collectedItemScoreBox.ScoreAnimator.SetTrigger("Expand");
            collectedItemScoreBox.IconAnimator.SetTrigger("Expand");
        }

    }

    public void UpdateUIScore(int mutualScore)
    {
        //Update mutual score
        if(_mutualScore)
            _mutualScore.text = mutualScore.ToString();
        if(_mutualScoreCertificate)
            _mutualScoreCertificate.text = mutualScore.ToString();
        if (_mutualScoreAnimator)
            _mutualScoreAnimator.SetTrigger("Expand");

    }


    public void InitializeSeparateScorePanel(Dictionary<CollectionItemType, int> scores, CollectionItemDirectionService directionService)
    {
        if (_itemTypeSpritesInitialized == false)
        {
            InitializeItemTypeSpritesDictionary();
        }

        foreach (var item in scores)
        {
            if (item.Key != CollectionItemType.Bad)
            {
                var scoreBox = Instantiate(_scoreBoxPrefab, _separateScorePanel).GetComponent<ScoreBox>();
                scoreBox.Text.text = item.Value.ToString();
                if (_itemTypeSprites.ContainsKey(item.Key))
                    scoreBox.Image.sprite = _itemTypeSprites[item.Key];
                directionService.ScoreTransforms.Add(item.Key, scoreBox.Image.rectTransform);
                _scoreBoxes.Add(item.Key, scoreBox);
            }
        }
    }

    private void InitializeItemTypeSpritesDictionary()
    {
        //Since dictionary is non-serializable we convers 2 serialized lists into dictionary
        if (_itemTypeSpritesTemp.Count != _itemTypeTemp.Count)
        {
            Debug.LogError("ZP CollectionItemUIController: Type list and Sprites list have different number of elements");
            return;
        }
        for (int i = 0; i < _itemTypeTemp.Count; i++)
        {
            _itemTypeSprites.Add(_itemTypeTemp[i], _itemTypeSpritesTemp[i]);
        }

        _itemTypeSpritesInitialized = true;
    }

    #endregion

}
