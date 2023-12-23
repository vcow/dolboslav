using System;
using Windows;
using Base.WindowManager;
using Base.WindowManager.Extensions.ScreenLockerExtension;
using Game.Model;
using Sound;
using Start.Signals;
using UniRx;
using UnityEngine;
using User;
using User.Model;
using WindowManager;
using Zenject;

namespace Start
{
	/// <summary>
	/// Start scene controller.
	/// </summary>
	[DisallowMultipleComponent]
	public class StartInstaller : MonoInstaller<StartInstaller>
	{
		private readonly CompositeDisposable _handlers = new();

		[Inject] private readonly IScreenLockerManager _screenLockerManager;
		[Inject] private readonly ZenjectSceneLoader _sceneLoader;
		[Inject] private readonly IWindowManagerExt _windowManager;
		[Inject] private readonly ISoundManager _soundManager;
		[Inject] private readonly GameConfig _gameConfig;
		[Inject] private readonly IUserModel _userModel;
		[Inject] private readonly UserModelController _userModelController;

		private static readonly int Out = Animator.StringToHash("Out");

		public override void InstallBindings()
		{
			Container.DeclareSignal<SettingsWindowCloseSignal>();
			Container.DeclareSignal<CloseButtonDownSignal>();
			Container.DeclareSignal<CloseButtonUpSignal>();
		}

		public override void Start()
		{
			if (_screenLockerManager.IsLocked)
			{
				_screenLockerManager.Unlock(SceneIsReadyHandler);
			}
			else
			{
				SceneIsReadyHandler(LockerType.Undefined);
			}
		}

		private void OnDestroy()
		{
			_handlers.Dispose();
		}

		private void SceneIsReadyHandler(LockerType lockerType)
		{
			_soundManager.PlayMusic("intro", 2.5f);
		}

		public void OnSettingsButton()
		{
			if (_windowManager.GetWindow(SettingsWindow.Id) == null)
			{
				_windowManager.ShowWindow(Container, SettingsWindow.Id);
			}
		}

		/// <summary>
		/// Play button handler.
		/// </summary>
		public void OnPlayButton()
		{
			if (!_userModelController.PlayTheGame(_gameConfig, out var nextGameSpawnDelaySec))
			{
				// Can't start new game for some reasons.
				if (!nextGameSpawnDelaySec.HasValue)
				{
					Debug.LogError("Failed to calculate next Game spawn time.");
					return;
				}

				var wnd = _windowManager.ShowWindow(GamesOverWindow.Id,
					new object[] { nextGameSpawnDelaySec.Value });

				IDisposable handler = null;
				// Introduce the CloseWindowEvent as Observable and subscribe it.
				handler = Observable.FromEvent<CloseWindowHandler, (IWindow sender, object result)>(
						h => (sender, result) => h((sender, result)),
						h => wnd.CloseWindowEvent += h, h => wnd.CloseWindowEvent -= h)
					.Subscribe(result =>
					{
						// ReSharper disable once AccessToModifiedClosure
						_handlers.Remove(handler);

						if ((bool)result.result)
						{
							// User can play the Game. Try again.
							OnPlayButton();
						}
					}).AddTo(_handlers);
				return;
			}

			PlayOutAnimation();

			// Crete a game model for the new game. 
			var maxSteps = _gameConfig.MaxSteps + (_userModel.HasAdditionalGameStep ? 1 : 0);
			var newGameModel = new GameModel(maxSteps);

			// And bind this model to the game scene.
			_screenLockerManager.Lock(LockerType.SceneLoader,
				() => _sceneLoader.LoadSceneAsync("GameScene",
					extraBindings: container => container.Bind(typeof(GameModel), typeof(IGameModel))
						.FromInstance(newGameModel).AsCached()));
		}

		private void PlayOutAnimation()
		{
			var animator = GetComponentInChildren<Animator>();
			if (!animator)
			{
				return;
			}

			animator.SetTrigger(Out);
		}
	}
}