using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Image))]
public class CardFlip : MonoBehaviour, IPointerClickHandler
{
    private Image _image;

    [SerializeField]
    private GameObject _front;

    [SerializeField]
    private GameObject _back;

    public GameObject Front { get => _front; set => _front = value; }
    public GameObject Back { get => _back; set => _back = value; }
    public GameObject ScrollArea { get => _scrollArea; set => _scrollArea = value; }
    public bool IsFront { get => _isFront; }
    public bool IsRotating { get => _isRotating; }

    [SerializeField]
    private GameObject _scrollArea;

    private bool _isRotating;

    private bool _isFront = true;

    private Touch _beganTouch;

    [SerializeField]
    private GameObject _slideMenu;

    [SerializeField]
    private GameObject _emoji;

    private Vector2 _emojiDefaultPos;

    [SerializeField]
    private Sprite _frontSprite;

    [SerializeField]
    private Sprite _backSprite;

    void Start()
    {
        if (_emoji)
            _emojiDefaultPos = _emoji.transform.position;
        _image = GetComponent<Image>();
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if(Mathf.Abs(eventData.position.x -eventData.pressPosition.x) < SlideManager.SWIPE_MIN_DISTANCE)
            TryFlipCard();
    }


    public void TryFlipCard()
    {
        if (!_isRotating && !_slideMenu.activeInHierarchy)
        {
            Flip();
        }
    }

    private void Flip()
    {
        if (_emoji)
            _emoji.SetActive(false);
        if (_isFront)
        {
            StartCoroutine(ShowBack());
        }
        else
        {
            StartCoroutine(ShowFront());
        }
    }

    private IEnumerator ShowFront(){
        _isRotating = true;
        for (int i = 180; i > 0; i -= 5)
        {
            transform.rotation = Quaternion.Euler(0, i, 0);
            if (i == 90f)
            {
                SetCardSprite();
                _back.SetActive(false);
                _front.SetActive(true);
            }
            yield return null;
        }
        _isFront = true;
        _isRotating = false;
        EnableEmoji();
    }

     private IEnumerator ShowBack(){
         _isRotating = true;
        for (int i = 0; i < 180; i += 5)
            {
                transform.rotation = Quaternion.Euler(0, i, 0);
                if (i == 90f)
                {
                    SetCardSprite(false);
                    _back.SetActive(true);
                    _front.SetActive(false);
                }
                yield return null;
            }
        _isFront = false;
        _isRotating = false;
        EnableEmoji();
    }

    public void Reset()
    {
        StopCoroutine("ShowFront");
        StopCoroutine("ShowBack");
        _isFront = true;
        _back.SetActive(false);
        _front.SetActive(true);
        _isRotating = false;
        transform.rotation = Quaternion.Euler(0, 0, 0);
    }

    public void SetCardSprite(bool isFront = true)
    {
        _image.sprite = isFront == true ? _frontSprite : _backSprite;
    }

    private void EnableEmoji()
    {
        if (_emoji)
        {
            _emoji.transform.position = _emojiDefaultPos;
            _emoji.SetActive(true);
        }
    }

}