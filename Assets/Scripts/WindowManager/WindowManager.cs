using Base.WindowManager;
using UnityEngine;
using Zenject;

namespace WindowManager
{
	/// <summary>
	/// See https://github.com/vcow/lib-window-manager for details.
	/// </summary>
	public sealed class WindowManager : WindowManagerBase, IWindowManagerExt
	{
		private DiContainer _extContainer;

		[Inject] private readonly DiContainer _container;

		protected override int StartCanvasSortingOrder => 1000;

		protected override void InitWindow(IWindow window, object[] args)
		{
			base.InitWindow(window, args);
			DiContainer container;
			if (_extContainer != null)
			{
				container = _extContainer;
				_extContainer = null;
			}
			else
			{
				container = _container;
			}

			container.InjectGameObject(((MonoBehaviour)window).gameObject);
		}

		public IWindow ShowWindow(DiContainer container, string windowId, object[] args = null, bool? isUnique = null,
			bool? overlap = null, string windowGroup = null)
		{
			_extContainer = container;
			return ShowWindow(windowId, args, isUnique, overlap, windowGroup);
		}
	}
}