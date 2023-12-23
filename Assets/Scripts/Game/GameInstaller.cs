using Windows;
using Base.WindowManager.Extensions.ScreenLockerExtension;
using Game.Model;
using Game.Signals;
using ScreenLocker;
using Sound;
using Start.Signals;
using UniRx;
using UnityEngine;
using User;
using User.Model;
using WindowManager;
using Zenject;

namespace Game
{
	/// <summary>
	/// Game round scene UI controller.
	/// </summary>
	[DisallowMultipleComponent]
	public class GameInstaller : MonoInstaller<GameInstaller>
	{
		private readonly CompositeDisposable _handlers = new();
		private GameModelController _gameModelController;
		private GameModel _gameModel;

		private long _deltaScore;

		[Inject] private readonly IScreenLockerManagerExt _screenLockerManager;
		[Inject] private readonly IWindowManagerExt _windowManager;
		[Inject] private readonly ISoundManager _soundManager;
		[Inject] private readonly ScreenLockerSettings _screenLockerSettings;
		[Inject] private readonly ZenjectSceneLoader _sceneLoader;
		[Inject] private readonly GameConfig _gameConfig;
		[Inject] private readonly UserModelController _userModelController;
		[Inject] private readonly IUserModel _userModel;

		public override void InstallBindings()
		{
			Container.BindInterfacesAndSelfTo<GameModelDecorator>().FromNew().AsCached();

			Container.DeclareSignal<SettingsWindowCloseSignal>();
			Container.DeclareSignal<CloseButtonDownSignal>();
			Container.DeclareSignal<CloseButtonUpSignal>();
			Container.DeclareSignal<SelectIdolColorSignal>();
			Container.DeclareSignal<PraySignal>();
			Container.DeclareSignal<FinishStepSignal>();
		}

		[Inject]
		private void Construct(GameModel gameModel)
		{
			_gameModel = gameModel;
			_gameModelController = new GameModelController(_gameModel);
		}

		public override void Start()
		{
			// Subscribe signals from other UI elements.
			var signalBus = Container.Resolve<SignalBus>();
			signalBus.Subscribe<SelectIdolColorSignal>(OnSelectIdolColor);
			signalBus.Subscribe<PraySignal>(OnPray);
			signalBus.Subscribe<FinishStepSignal>(OnFinishStep);

			// Unlock the scene on start.
			if (_screenLockerManager.IsLocked)
			{
				_screenLockerManager.Unlock(SceneIsReadyHandler);
			}
			else
			{
				SceneIsReadyHandler(LockerType.Undefined);
			}

			_soundManager.PlayMusic("main_title");

			Debug.Log(_gameModel.Target);
		}

		private void OnDestroy()
		{
			FinishGameRound();
		}

		// Round is over. Stop listen any signals and don't react the model.
		private void FinishGameRound()
		{
			_handlers.Dispose();

			var signalBus = Container.Resolve<SignalBus>();
			signalBus.TryUnsubscribe<SelectIdolColorSignal>(OnSelectIdolColor);
			signalBus.TryUnsubscribe<PraySignal>(OnPray);
			signalBus.TryUnsubscribe<FinishStepSignal>(OnPray);
		}

		// User select new color for the some idol.
		private void OnSelectIdolColor(SelectIdolColorSignal signal)
		{
			_gameModelController.SetCurrentStepColor(signal.IdolNumber, signal.IdolColor);
		}

		private void OnPray()
		{
			if (_gameModelController.Move())
			{
				// If move success, the round is over.

				if (_userModel.HasAdditionalGameStep && _gameModel.History.Count >= _gameModel.MaxSteps)
				{
					// User has exceeded the limit and spent the "Favor of the gods"
					_userModelController.DecrementAdditionalSteps();
				}

				if (_gameModel.IsGameOver(out var isWin) && isWin)
				{
					// Add winning
					var bless = _gameConfig.GetBlessSettingsForSteps(_gameModel.History.Count);
					_deltaScore = bless.blessValue;
					_userModelController.AddScore(_deltaScore);
				}

				FinishGameRound();
			}
		}

		private void OnFinishStep()
		{
			// Set screen locker for the Game over final scene or for transition between the Game steps.
			if (_gameModel.IsGameOver(out _))
			{
				var screenLocker = _screenLockerSettings.FinalScreenLocker;
				if (!screenLocker)
				{
					Debug.LogError("Screen locker for the Game final scene isn't specified.");
					screenLocker = _screenLockerSettings.GetScreenLocker(LockerType.SceneLoader);
				}

				// Load the Final scene and pass there current Game model. Bind Game model for sure with
				// IDisposable to dispose the model when Final scene will be destroyed.
				_screenLockerManager.SetScreenLocker(LockerType.SceneLoader, screenLocker);
				_screenLockerManager.Lock(Container, LockerType.SceneLoader, () =>
					_sceneLoader.LoadSceneAsync("FinalScene",
						extraBindings: container =>
						{
							container.BindInterfacesTo<GameModel>().FromInstance(_gameModel).AsCached();
							if (_deltaScore > 0)
							{
								// Bind delta score if user win.
								container.Bind<long>().FromInstance(_deltaScore).AsCached();
							}
						}));
			}
			else
			{
				var screenLocker = _screenLockerSettings.StepScreenLocker;
				if (!screenLocker)
				{
					Debug.LogError("Screen locker for the Game's step isn't specified.");
					screenLocker = _screenLockerSettings.GetScreenLocker(LockerType.SceneLoader);
				}

				// Load Game scene again for the next step and pass there current Game model.
				_screenLockerManager.SetScreenLocker(LockerType.SceneLoader, screenLocker);
				_screenLockerManager.Lock(Container, LockerType.SceneLoader, () =>
					_sceneLoader.LoadSceneAsync("GameScene",
						extraBindings: container => container.Bind(typeof(GameModel), typeof(IGameModel))
							.FromInstance(_gameModel).AsCached()));
			}
		}

		private void SceneIsReadyHandler(LockerType lockerType)
		{
			//TODO: Something, that can only work after the scene unlocked.
		}

		public void OnSettingsButton()
		{
			// Show the Settings window if it isn't already shown.
			if (_windowManager.GetWindow(SettingsWindow.Id) == null)
			{
				_windowManager.ShowWindow(Container, SettingsWindow.Id);
			}
		}

		public void OnShowTableWindow()
		{
			// Show the Table window if it isn't already shown.
			if (_windowManager.GetWindow(TableWindow.Id) == null)
			{
				_windowManager.ShowWindow(Container, TableWindow.Id);
			}
		}
	}
}