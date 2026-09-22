using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using UnityEngine.UI;

public class CollectionItemDirectionService : MonoBehaviour
{
    #region Variables

    [SerializeField]
    private GameObject _directionPrefab;

    //Parent for instantiated direction boxes
    [SerializeField]
    private Transform _directionBoxesGroup;

    //Number of direction boxes rendered on screen
    [SerializeField]
    private int _limit = 3;

    [SerializeField]
    private Transform _cam;

    [SerializeField]
    private Color _badItemColor = Color.red;

    [SerializeField]
    private Color _goodItemColor = Color.green;

    private List<CollectionItemDirection> _directionBoxes = new List<CollectionItemDirection>();

    private Dictionary<CollectionItemType, RectTransform> _scoreTransforms = new Dictionary<CollectionItemType, RectTransform>();

    public Dictionary<CollectionItemType, RectTransform> ScoreTransforms { get => _scoreTransforms; }

    public event Action<CollectionItem> OnCollectedAnimationEnded;

    #endregion

    #region Methods

    public void HandleDirectionBoxes(List<GameObject> itemsList, Dictionary<CollectionItemType, Sprite> itemTypeSprites)
    {
        UpdateDirectionBoxes();
        if (_directionBoxes.Count >= _limit)
            return;
        CreateDirectionBoxes(itemsList, itemTypeSprites);
    }

    private void UpdateDirectionBoxes()
    {
        foreach(var box in _directionBoxes)
        {
            box.Timer.fillAmount = box.Item.IsLongLiving ?  box.Item.LifeTime / CollectionItemGenerationService.LongLivingItemLifeTime
                : box.Item.LifeTime / CollectionItemGenerationService.ItemLifeTime;
            UpdateArrows(box);
        }        
    }

    private void UpdateArrows(CollectionItemDirection box)
    {
        var target = box.Item.transform;
        var arrow = box.Arrow.transform;
        var dir3D = (target.transform.position - _cam.position);
        //If object is behind set turn behind image
        bool isBehind = Vector3.Dot(_cam.forward, dir3D) > 0 ? false : true;
        box.TurnBehind.SetActive(isBehind);
        box.Arrow.SetActive(!isBehind);
        if (isBehind)
            return;
        // Get the position of the object in screen space
        Vector3 objScreenPos = Camera.main.WorldToScreenPoint(target.transform.position);

        // Get the directional vector between center of camera and object
        Vector3 dir = (objScreenPos - new Vector3(Screen.width / 2, Screen.height / 2, 0)).normalized;
        // Calculate the angle 
        // We assume the default arrow position at 0° is "right"
        float angle = Mathf.Rad2Deg * Mathf.Acos(Vector3.Dot(dir, Vector3.right));

        // Use the cross product to determine if the angle is clockwise
        // or anticlockwise
        Vector3 cross = Vector3.Cross(dir, Vector3.right);
        angle = -Mathf.Sign(cross.z) * angle;

        // Update rotation of arrow
        arrow.localEulerAngles = new Vector3(arrow.localEulerAngles.x, arrow.localEulerAngles.y, angle);
    }
    private void CreateDirectionBoxes(List<GameObject> itemsList, Dictionary<CollectionItemType, Sprite> itemTypeSprites)
    {
        //Create CollectionItem list
        List<CollectionItem> items = new List<CollectionItem>();
        foreach (var item in itemsList)
        {
            items.Add(item.GetComponent<CollectionItem>());
        }
        //Get missing number of items
        var newItems = items.Where(i =>
        {
            //Check if item is already tracked
            foreach (var box in _directionBoxes)
            {
                if (box.Item == i)
                    return false;
            }
            return true;
        }).OrderByDescending(i => i.LifeTime).Take(_limit - _directionBoxes.Count).ToList();
        if (newItems.Count == 0)
            return;
        //Instantiate and configure direction box for each new item
        foreach (var item in newItems)
        {
            var directionBox = Instantiate(_directionPrefab, _directionBoxesGroup).GetComponent<CollectionItemDirection>();
            directionBox.Item = item;
            //If sprite for this item type exists
            if(itemTypeSprites[item.Type])
                directionBox.Icon.sprite = itemTypeSprites[item.Type];
            //Change timer color
            directionBox.Timer.color = item.Type == CollectionItemType.Bad ? _badItemColor : _goodItemColor;
            _directionBoxes.Add(directionBox);
        }
    }

    public void RemoveDirectionBox(GameObject item, bool isCollected)
    {
        var collectionItem = item.GetComponent<CollectionItem>();
        if (_directionBoxes.Count == 0)
        {
            return;
        }
        //Loop through copy since original list might be modified while looping
        foreach(var box in _directionBoxes.ToList())
        {
            if(box.Item == collectionItem)
            {
                if (isCollected)
                {
                    if (ScorePanel.CurrentState.Type != ScorePanelType.Separate || collectionItem.Type == CollectionItemType.Bad)
                    {
                        _directionBoxes.Remove(box);
                        //Remove direction box from scene
                        Destroy(box.gameObject);
                    }
                    else
                    {
                        StartCoroutine(LerpPositionAndRemoveRoutine(box, _scoreTransforms[box.Item.Type].position, 1.7f, collectionItem));
                    }
                }
                else
                {
                    StartCoroutine(ShowAnimationAndRemoveRoutine(box, "NotCollected"));
                }
            }
        }
    }

    public bool HasDirectionBox(CollectionItem item)
    {
        if (_directionBoxes.Count == 0)
        {
            return false;
        }
        foreach (var box in _directionBoxes.ToList())
        {
            if (box.Item == item)
            {
                return true;
            }
        }
        return false;
    }

    public IEnumerator ShowAnimationAndRemoveRoutine(CollectionItemDirection box, string animationTrigger)
    {
        if (_directionBoxes.Contains(box))
        {
            _directionBoxes.Remove(box);
        }
        var animator = box.gameObject.GetComponent<Animator>();
        animator.SetTrigger(animationTrigger);
        //Wait for animation to start
        yield return new WaitForSeconds(0.2f);
        yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 1 && !animator.IsInTransition(0));
        //Remove direction box from scene
        Destroy(box.gameObject);
    }

    IEnumerator LerpPositionAndRemoveRoutine(CollectionItemDirection box, Vector2 targetPosition, float duration, CollectionItem collectionItem)
    {
        if (_directionBoxes.Contains(box))
        {
            _directionBoxes.Remove(box);
            box.gameObject.GetComponent<LayoutElement>().ignoreLayout = true;
        }
        box.Timer.gameObject.SetActive(false);
        box.Arrow.gameObject.SetActive(false);
        box.TurnBehind.gameObject.SetActive(false);

        //Move to screen center
        float time = 0;
        Vector2 startPosition = box.GetComponent<RectTransform>().position;
        var animator = box.gameObject.GetComponent<Animator>();
        bool scaleAnimationStarted = false;
        Vector2 screenCenterPosition = new Vector2(Screen.width / 2, Screen.height / 2);
        while (time < duration)
        {
            if (!scaleAnimationStarted && time > duration / 3)
            {
                animator.SetTrigger("Scale");
            }
            box.GetComponent<RectTransform>().position = Vector2.Lerp(startPosition, screenCenterPosition, time / duration);
            time += Time.deltaTime;
            yield return null;
        }

        //Move to score panel
        time = 0;
        startPosition = box.GetComponent<RectTransform>().position;
        duration /= 2;
        animator.SetTrigger("ScaleDown");
        while (time < duration)
        {
            box.GetComponent<RectTransform>().position = Vector2.Lerp(startPosition, targetPosition, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        OnCollectedAnimationEnded?.Invoke(collectionItem);
        box.GetComponent<RectTransform>().position = targetPosition;
        Destroy(box.gameObject);         //Remove direction box from scene
    }
    #endregion
}
