using System.Collections;
using Base.WindowManager.Extensions.ScreenLockerExtension;
using DG.Tweening;
using Game.Model;
using ScreenLocker;
using Sound;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using Zenject;

namespace Final
{
	/// <summary>
	/// Game result final scene controller.
	/// </summary>
	[DisallowMultipleComponent]
	public sealed class FinalInstaller : MonoInstaller<FinalInstaller>
	{
		private Vector2 _backButtonDestination, _tableDestination;
		private Button _backButton;

		[Inject] private readonly IScreenLockerManager _screenLockerManager;
		[Inject] private readonly ISoundManager _soundManager;
		[Inject] private readonly IGameModel _gameModel;
		[Inject] private readonly ScreenLockerSettings _screenLockerSettings;
		[Inject] private readonly ZenjectSceneLoader _sceneLoader;

		[SerializeField] private RectTransform _table;
		[SerializeField] private RectTransform _backButtonContainer;

		public override void InstallBindings()
		{
			Container.BindInterfacesAndSelfTo<GameModelDecorator>().FromNew().AsCached();
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

			if (_gameModel.IsGameOver(out var isWin))
			{
				_soundManager.PlayMusic(isWin ? "win" : "fail");
			}

			// Prepare initial animation for scene and UI components.
			Assert.IsTrue(_table && _backButtonContainer, "Table and BackButton transform must have.");
			_backButtonDestination = _backButtonContainer.anchoredPosition;
			_tableDestination = _table.anchoredPosition;

			_backButtonContainer.anchoredPosition = _backButtonDestination + Vector2.left *
				(_backButtonContainer.sizeDelta.x + _backButtonDestination.x + 50f);
			_table.anchoredPosition = _tableDestination + Vector2.down *
				(_table.sizeDelta.y + _tableDestination.y);

			_backButton = _backButtonContainer.GetComponentInChildren<Button>();
			Assert.IsTrue(_backButton, "Back button must have.");
			_backButton.interactable = false;
		}

		private void SceneIsReadyHandler(LockerType lockerType)
		{
			// Set common scene screen locker for transition to the Start scene.
			_screenLockerManager.SetScreenLocker(LockerType.SceneLoader,
				_screenLockerSettings.GetScreenLocker(LockerType.SceneLoader));

			StartCoroutine(PlayUiAnimationRoutine());
		}

		private IEnumerator PlayUiAnimationRoutine()
		{
			yield return new WaitForSeconds(1f);
			// Show table.
			_table.DOAnchorPos(_tableDestination, 1.5f).SetEase(Ease.OutQuad).SetLink(_table.gameObject);

			yield return new WaitForSeconds(2f);
			// Show Back button.
			_backButtonContainer.DOAnchorPos(_backButtonDestination, 1f).SetEase(Ease.OutBack)
				.SetLink(_backButtonContainer.gameObject).OnComplete(() => _backButton.interactable = true);
			_soundManager.PlaySound("wzuch1", 0.1f);
		}

		private void OnDestroy()
		{
			StopAllCoroutines();
		}

		/// <summary>
		/// Back button handler.
		/// </summary>
		public void OnBack()
		{
			_screenLockerManager.Lock(LockerType.SceneLoader, () => _sceneLoader.LoadSceneAsync("StartScene"));
		}
	}
}