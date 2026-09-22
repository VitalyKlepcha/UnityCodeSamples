using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class AppInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        Container.Bind<ILocalDatabase>().To<SQLiteDatabase>().AsSingle().NonLazy();
        Container.Bind<IRemoteDatabase>().To<FirebaseRemoteDatabase>().AsSingle();
        Container.Bind<IWalletService>().To<GameFoundationWalletService>().AsSingle();
        Container.Bind<IInAppPurchaseService>().To<InAppPurchaseService>().AsSingle();
        Container.Bind<IInAppPurchaseManager>().To<InAppPurchaseManager>().AsSingle();
        Container.Bind<IlevelManager>().To<LevelManager>().AsSingle();
        Container.Bind<IAdsService>().To<IronSourceService>().AsSingle();
        Container.Bind<IAdsManager>().FromInstance(AdsManager.Instance).AsSingle();
        Container.Bind<IRemoteConfig>().To<FirebaseRemoteConfig>().AsSingle();

        Container.QueueForInject(AdsManager.Instance);
    }
}
