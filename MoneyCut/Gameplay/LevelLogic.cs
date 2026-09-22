using UnityEngine;

public class LevelLogic : MonoBehaviour
{
	const string MONEY_PREF = "Money";

	[SerializeField]
	float _fuelTotal = 100;
	[SerializeField]
	float _fuelConsumption = 0.1f;
	[SerializeField]
	float _onGrassConsumption = 0.15f;

	static LevelLogic _inst = null;
	MowerLogic _player = null;
	float _fuel = 0;

	int _money = 0;

	internal static LevelLogic Inst
	{
		get
		{
			if (!_inst)
			{
				_inst = FindObjectOfType<LevelLogic>();
				if (!_inst)
				{
					var go = new GameObject("Level Logic");
					_inst = go.AddComponent<LevelLogic>();
				}
			}
			return _inst;
		}
	}

	internal static void register(MowerLogic mower)
	{
		Inst._player = mower;
	}

	private void Awake()
	{
		_money = PlayerPrefs.GetInt(MONEY_PREF, 0);
	}

	private void Start()
	{
		_fuel = _fuelTotal;
	}

	internal static MowerLogic Player => Inst._player;

	internal static float GrassPercent
	{
		get
		{
			var cutter = Player ? Player.Cutter : null;
			if (!cutter)
				return 100;

			return 100.0f - 100.0f * cutter.TreesCutDownTotal / cutter.TreesTotal;
		}
	}

	internal static float ZeroPrototypePercent
	{
		get
		{
			var cutter = Player ? Player.Cutter : null;
			if (!cutter)
				return 100;

			return 100.0f - 100.0f * cutter.ZeroPrototypeCutDown / cutter.ZeroPrototypeTotal;
		}
	}


	internal static float FuelPercent => 100.0f * Mathf.Clamp01(Inst._fuel / Inst._fuelTotal);

	internal static void fuelBurned(float metersDriven, bool onGrass)
	{
		Inst.subtractFuel(metersDriven, onGrass);
	}

	internal static int Money => Inst._money;

	internal static int addMoney(int value)
	{
		int m = Inst._money + value;
		Inst._money = m;
		PlayerPrefs.SetInt(MONEY_PREF, m);
		return m;
	}

	internal static void addFuel(float value)
	{
		Inst._fuel = Mathf.Clamp(Inst._fuel + value, 0, Inst._fuelTotal);
	}

	internal static void SubstractFuel(float value)
    {
		Inst._fuel = Mathf.Clamp(Inst._fuel - value, 0, Inst._fuelTotal);
	}

	private void subtractFuel(float metersDriven, bool onGrass)
	{
		float subV = onGrass ? _onGrassConsumption : _fuelConsumption;
		_fuel -= metersDriven * subV;
		if (_fuel < 0)
			_fuel = 0;
	}
}

public enum LevelType
{
	//Mow all grass
	Classic,
	//Mow all grass at specific time period
	TimeBound,
	//Score is given for specific grass type
	SpecificGrass,
	//Mow message made of specific grass type
	Message
}