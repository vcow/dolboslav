using System.Collections.Generic;
using Base.WindowManager;
using UnityEngine;

namespace WindowManager
{
	/// <summary>
	/// See https://github.com/vcow/lib-window-manager for details.
	/// </summary>
	[CreateAssetMenu(fileName = "WindowProvider", menuName = "Window Manager/Window Provider")]
	public class WindowProvider : WindowProviderBase
	{
#pragma warning disable 649
		[SerializeField] private Window[] _windows;
#pragma warning restore 649

		public override IReadOnlyList<Window> Windows => _windows;
	}
}