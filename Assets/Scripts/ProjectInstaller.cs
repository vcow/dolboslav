using System;
using Base.Localization;
using Base.WindowManager;
using Localization;
using ScreenLocker;
using UnityEngine;
using User;
using WindowManager;
using Zenject;

[DisallowMultipleComponent]
public class ProjectInstaller : MonoInstaller<ProjectInstaller>
{
	public override void InstallBindings()
	{
		Container.Bind(typeof(IWindowManager), typeof(IWindowManagerExt))
			.FromComponentInNewPrefabResource(@"WindowManager").AsSingle();
		Container.BindInterfacesTo<ScreenLockerManager>().AsSingle().Lazy();
		Container.Bind<ILocalizationManager>().To<LocalizationManager>().AsSingle()
			.WithArguments(Application.systemLanguage);
		Container.Bind(typeof(UserModelController), typeof(IDisposable)).To<UserModelController>()
			.FromNew().AsSingle();

		SignalBusInstaller.Install(Container);
	}
}