using System;
using System.Collections.Generic;
using Base.Localization;
using Base.Localization.Template;
using UnityEngine;
using UnityEngine.Assertions;
using Zenject;

namespace Localization
{
	/// <summary>
	/// See https://github.com/vcow/lib-localizer for details.
	/// </summary>
	[CreateAssetMenu(fileName = "LocalizationProvider", menuName = "Localization Manager/Localization Provider")]
	public class LocalizationProvider : ScriptableObjectInstaller<LocalizationProvider>,
		ILocalizationProvider, IInitializable
	{
		private CSVLocalizationProvider _provider;

		[SerializeField] private List<TextAsset> _csvLocalizationTables;

		public override void InstallBindings()
		{
			Container.BindInterfacesTo<LocalizationProvider>().FromInstance(this).AsSingle();
		}

		public IEnumerable<LocaleEntry> Locales
		{
			get
			{
				if (!(_provider is { IsInited: true }))
				{
					throw new Exception("Localization provider isn't initialized.");
				}

				return _provider.Locales;
			}
		}

		void IInitializable.Initialize()
		{
			Assert.IsNull(_provider, "The localization provider is already initialized.");
			_provider = new CSVLocalizationProvider();
			_provider.Init(_csvLocalizationTables);
		}

		public bool IsInitialized => _provider?.IsInited ?? false;
	}
}