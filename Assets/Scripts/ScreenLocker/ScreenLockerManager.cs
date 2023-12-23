using System;
using Base.WindowManager.Extensions.ScreenLockerExtension;
using UnityEngine;
using Zenject;

namespace ScreenLocker
{
	/// <summary>
	/// See https://github.com/vcow/lib-window-manager#how-to-use-screenlockermanager for details.
	/// </summary>
	public class ScreenLockerManager : ScreenLockerManagerBase, IScreenLockerManagerExt
	{
		private readonly DiContainer _container;
		private DiContainer _extContainer;

		public ScreenLockerManager(ScreenLockerSettings screenLockerSettings, DiContainer container)
			: base(screenLockerSettings.ScreenLockers)
		{
			_container = container;
		}

		public void Lock(DiContainer container, LockerType type, Action completeCallback)
		{
			_extContainer = container;
			Lock(type, completeCallback);
		}

		protected override void InitLocker(IScreenLocker locker)
		{
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

			container.InjectGameObject(((MonoBehaviour)locker).gameObject);
		}
	}
}