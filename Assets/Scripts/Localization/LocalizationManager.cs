using Base.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Localization
{
	/// <summary>
	/// See https://github.com/vcow/lib-localizer for details.
	/// </summary>
	public class LocalizationManager : LocalizationManagerBase<LocaleTextController, LocaleTMPController>
	{
		[Inject] private readonly DiContainer _container;

		public LocalizationManager(ILocalizationProvider localizationProvider, SystemLanguage defaultLanguage)
			: base(localizationProvider, defaultLanguage)
		{
		}

		protected override void ApplyLocaleTextController(Text text)
		{
			base.ApplyLocaleTextController(text);
			_container.InjectGameObjectForComponent<LocaleTextController>(text.gameObject);
		}

		protected override void ApplyLocaleTMPController(TextMeshProUGUI text)
		{
			base.ApplyLocaleTMPController(text);
			_container.InjectGameObjectForComponent<LocaleTMPController>(text.gameObject);
		}
	}
}