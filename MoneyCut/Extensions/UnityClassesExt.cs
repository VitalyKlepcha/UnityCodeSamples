using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using static UniTaskExt;

public static class UnityClassesExt
{
    #region Activity Indicator
    public static void WithActivityIndicator(this MonoBehaviour monoBehaviour, Action action)
    {
        try
        {
            monoBehaviour.ShowActivityIndicator();
            action?.Invoke();
        }
        finally
        {
            monoBehaviour.HideActivityIndicator();
        }
    }

    public static async UniTask WithActivityIndicator(this MonoBehaviour monoBehaviour, ActionAsync action)
    {
        try
        {
            monoBehaviour.ShowActivityIndicator();
            if (action != null)
                await action.Invoke();
        }
        finally
        {
            monoBehaviour.HideActivityIndicator();
        }
    }

    public static void ShowActivityIndicator(this MonoBehaviour monoBehaviour, Transform parentTransform = null)
    {
        if (parentTransform == null)
            parentTransform = monoBehaviour.transform;

        var canvas = parentTransform.root.GetComponent<Canvas>();
        var transform = canvas != null ? canvas.transform : parentTransform;

        var activityIndicatorPrefab = Resources.Load<GameObject>("Prefabs/Shared/ActivityIndicator");

        var activityIndicator = UnityEngine.Object.Instantiate(activityIndicatorPrefab, transform);
        activityIndicator.tag = "ActivityIndicator";
    }

    public static void HideActivityIndicator(this MonoBehaviour monoBehaviour)
    {
        if (monoBehaviour == null || !monoBehaviour.isActiveAndEnabled)
            return;

        var activityIndicator = GameObject.FindGameObjectWithTag("ActivityIndicator");
        if (activityIndicator != null)
            UnityEngine.Object.Destroy(activityIndicator);
    }

    public static void HideAllActivityIndicators(this MonoBehaviour monoBehaviour)
    {
        if (monoBehaviour == null || !monoBehaviour.isActiveAndEnabled)
            return;

        var activityIndicators = GameObject.FindGameObjectsWithTag("ActivityIndicator");
        if (activityIndicators != null)
            foreach (var activityIndicator in activityIndicators)
                UnityEngine.Object.Destroy(activityIndicator);
    }
    #endregion
}