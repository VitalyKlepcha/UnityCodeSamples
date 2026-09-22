using System;
using UnityEditor;

public class PrefsValue<T> where T : IConvertible, IEquatable<T>
{
	string _name = "";
	T _defVal = default(T);

	public PrefsValue(string name, T defVal = default(T))
	{
		_name = name;
		_defVal = defVal;
	}

	public T Value
	{
		get
		{
			Type tp = typeof(T);
			if (tp == typeof(bool))
				return (T)(object)EditorPrefs.GetBool(_name, _defVal.ToBoolean(null));
			if (tp == typeof(int))
				return (T)(object)EditorPrefs.GetInt(_name, _defVal.ToInt32(null));
			if (tp == typeof(float))
				return (T)(object)EditorPrefs.GetFloat(_name, _defVal.ToSingle(null));
			return (T)(object)EditorPrefs.GetString(_name, _defVal.ToString(null));
		}
		set
		{
			Type tp = typeof(T);
			if (tp == typeof(bool))
				EditorPrefs.SetBool(_name, value.ToBoolean(null));
			if (tp == typeof(int))
				EditorPrefs.SetInt(_name, value.ToInt32(null));
			if (tp == typeof(float))
				EditorPrefs.SetFloat(_name, value.ToSingle(null));
			if (tp == typeof(string))
				EditorPrefs.SetString(_name, value.ToString());
		}
	}

	public T showEditor(string label = null)
	{
		EditorGUI.BeginChangeCheck();
		T oldV = Value;
		T newV = Value;
		Type tp = typeof(T);
		if (tp == typeof(bool))
			newV = (T)(object)EditorGUILayout.Toggle(label, oldV.ToBoolean(null));
		if (tp == typeof(int))
			newV = (T)(object)(string.IsNullOrEmpty(label) ? EditorGUILayout.IntField(oldV.ToInt32(null)) : EditorGUILayout.IntField(label, oldV.ToInt32(null)));
		if (tp == typeof(float))
			newV = (T)(object)(string.IsNullOrEmpty(label) ? EditorGUILayout.FloatField(oldV.ToSingle(null)) : EditorGUILayout.FloatField(label, oldV.ToSingle(null)));
		if (tp == typeof(string))
			newV = (T)(object)(string.IsNullOrEmpty(label) ? EditorGUILayout.TextField(oldV.ToString()) : EditorGUILayout.TextField(label, oldV.ToString()));
		if (EditorGUI.EndChangeCheck())
		{
			if ((oldV == null && newV != null) || !oldV.Equals(newV))
				Value = newV;
		}
		return newV;
	}
}