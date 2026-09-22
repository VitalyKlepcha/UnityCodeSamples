using System;
using System.Linq;
using UnityEngine;

public class BonusItem : MonoBehaviour
{
    public enum BonusTypes
    {
        Fuel,
        BladeSize,
        Fire,
        Accelerator,
        Decelerator,
        Shield,
        GoodRandom,
        BadRandom,
        InverseControl,
        Dynamite,
        Oil,
        Tornado
    }

    [Serializable]
    public class BonusParameters
    {
        [SerializeField]
        public float Value;
        [SerializeField]
        public float Duration;
       

        public Vector2 ToVector2()
        {
            return new Vector2(Value, Duration);
        }
    }

    [SerializeField]
    BonusParameters _bonusParameters = new BonusParameters { Value = 1, Duration = 1 };

    [SerializeField]
    BonusTypes _type;

    [SerializeField]
    GameObject _grabEffect;
	[SerializeField]
	float _grabDuration = 3;
	[SerializeField]
	float _hideDuration = 1.5f;

    MeshRenderer[] _hideRenders = null;
    float _grabTime = 0;

    //Determines whether bonus considered as positive or negative bonus
    internal bool IsBad { get => _type.IsBad(); }
    public float HideDuration { get => _hideDuration; set => _hideDuration = value; }
    public float GrabDuration { get => _grabDuration; set => _grabDuration = value; }
    public GameObject GrabEffect { get => _grabEffect; set => _grabEffect = value; }
    public float Duration { get => _bonusParameters.Duration; set => _bonusParameters.Duration = value; }
    public float Value { get => _bonusParameters.Value; set => _bonusParameters.Value = value; }
    public BonusTypes BonusType { get => _type; set => _type = value; }
    public BonusParameters BonusParams { get => _bonusParameters; set => _bonusParameters = value; }

    private void OnTriggerEnter(Collider other)
	{
		BonusLogic player = null;
        for (Transform tr = other.transform; tr; tr = tr.parent)
        {
            player = tr.GetComponent<BonusLogic>();
            if (player)
                break;
        }

        if (player)
            player.ApplyBonus(this);
    }

    private void Update()
	{
        if (_hideRenders != null)
        {
            float diff = Time.time - _grabTime;
            if (diff > _hideDuration)
            {
                foreach (var r in _hideRenders)
					r.gameObject.SetActive(true);
                _hideRenders = null;
                return;
            }
            float a = 1.0f - diff / _hideDuration;
            foreach (var r in _hideRenders)
            {
                Color clr = r.sharedMaterial.color;
                clr.a = a;
                r.sharedMaterial.color = clr;
            }
		}
	}

	internal void grab()
    {
        if (_grabEffect)
            _grabEffect.SetActive(true);

        var cl = GetComponent<Collider>();
        if (cl)
            Destroy(cl);

        _grabTime = Time.time;
        _hideRenders = GetComponentsInChildren<MeshRenderer>();
        if (_hideRenders.Length > 0)
        {
            for (int i = 0; i < _hideRenders.Length; i++)
            {
                _hideRenders[i].sharedMaterial = new Material(_hideRenders[i].sharedMaterial);
            }
                
        }
        else
        {
            _hideRenders = null;
        }

        Destroy(gameObject, _grabDuration);
    }

    public static void CopyBonus(BonusItem from, BonusItem to)
    {
        to.BonusType = from.BonusType;
        to.Value = from.Value;
        to.Duration = from.Duration;
        to.GrabEffect = from.GrabEffect;
        to.HideDuration = from.HideDuration;
        to.GrabDuration = from.GrabDuration;
    }
}

public static class BonusTypeExtensions
{
    public static bool IsBad(this BonusItem.BonusTypes bonusType)
    {
        switch (bonusType)
        {
            case BonusItem.BonusTypes.Accelerator:
            case BonusItem.BonusTypes.BladeSize:
            case BonusItem.BonusTypes.Shield:
            case BonusItem.BonusTypes.GoodRandom:
            case BonusItem.BonusTypes.Fuel:
                return false;
            case BonusItem.BonusTypes.BadRandom:
            case BonusItem.BonusTypes.Decelerator:
            case BonusItem.BonusTypes.Fire:
            case BonusItem.BonusTypes.InverseControl:
            case BonusItem.BonusTypes.Dynamite:
            case BonusItem.BonusTypes.Oil:
            case BonusItem.BonusTypes.Tornado:
                return true;
            default: return false;
        }
    }

    public static GameObject GetStandartPrefab(this BonusItem.BonusTypes bonusType)
    {
        return Resources.LoadAll<BonusItem>("Items").Where(bonus => bonus.BonusType == bonusType).FirstOrDefault().gameObject;
    }

#if UNITY_EDITOR
    public static Sprite GetSprite(this BonusItem.BonusTypes bonusType)
    {
        return Resources.Load<Sprite>(string.Format("Sprites/BonusSprites/{0}", bonusType.ToString()));
    }
#endif
}