using Base.Localization;
using Base.Localization.Template;
using Zenject;

namespace Localization
{
	/// <summary>
	/// See https://github.com/vcow/lib-localizer for details.
	/// </summary>
	public sealed class LocaleTMPController : LocaleTMPControllerBase
	{
		[Inject] private readonly ILocalizationManager _localizationManager;
		protected override ILocalizationManager LocalizationManager => _localizationManager;
	}
}