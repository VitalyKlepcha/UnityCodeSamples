using UnityEngine;
using Zenject;

public class AppInstaller : MonoInstaller
{
    [SerializeField]
    private GameObject _popUpCanvasPrefab;

    [SerializeField]
    private GameObject _popUpManagerPrefab;

    public override void InstallBindings()
    {
        var popUpCanvas = Instantiate(_popUpCanvasPrefab).GetComponent<PopUpCanvas>();
        Container.Bind<IRemoteConfig>().To<FirebaseRemoteConfig>().AsSingle().NonLazy();
        Container.Bind<IAdsManager>().FromInstance(AdsManager.Instance).AsSingle();
        Container.QueueForInject(AdsManager.Instance);
        Container.Bind<IPilotLevelService>().To<PilotLevelService>().AsSingle().NonLazy();
        Container.Bind<PopUpCanvas>().FromInstance(popUpCanvas);
        Container.Bind<IPopUpManager>().To<PopUpManager>().FromComponentInNewPrefab(_popUpManagerPrefab).AsSingle().NonLazy();
    }
}
