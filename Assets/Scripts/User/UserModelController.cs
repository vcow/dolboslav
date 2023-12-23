using System;
using Base.Assignments.Initable;
using UniRx;
using UnityEngine;
using UnityEngine.Assertions;
using User.Model;
using Zenject;

namespace User
{
	/// <summary>
	/// Controller for work with the UserModel. Must be initialized before using.
	/// </summary>
	public sealed class UserModelController : IInitable, IDisposable
	{
		private readonly CompositeDisposable _handlers;
		private readonly Subject<bool> _invalidateObservable;

		private readonly DiContainer _container;
		private readonly GameConfig _gameConfig;
		private UserModel _userModel;

		private bool _isInited;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="container">Dependency injection container.</param>
		/// <param name="gameConfig">Game presets.</param>
		/// <param name="userModel">Current user model.</param>
		public UserModelController(DiContainer container, GameConfig gameConfig, [InjectOptional] UserModel userModel)
		{
			_container = container;
			_gameConfig = gameConfig;
			_userModel = userModel;

			_invalidateObservable = new Subject<bool>();
			_handlers = new CompositeDisposable(_invalidateObservable);

			// Multiple save protection.
			_invalidateObservable.ThrottleFrame(1)
				.Where(b => b)
				.Subscribe(_ =>
				{
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
					_userModel.Save();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
				}).AddTo(_handlers);
		}

		// IInitable

		async void IInitable.Init(params object[] args)
		{
			Assert.IsFalse(IsInited, "UserController must be initialized once.");
			if (_userModel == null)
			{
				_userModel = await UserModel.Restore() ?? new UserModel(
					_gameConfig.AdditionalGameSteps,
					0,
					_gameConfig.StartNumGames,
					_gameConfig.StartNumBonusGames,
					DateTimeOffset.Now.ToUnixTimeSeconds());
				_container.BindInterfacesAndSelfTo<UserModel>().FromInstance(_userModel).AsSingle();
			}

			IsInited = true;
		}

		public bool IsInited
		{
			get => _isInited;
			private set
			{
				if (value == _isInited)
				{
					return;
				}

				_isInited = value;
				Assert.IsTrue(_isInited);
				InitCompleteEvent?.Invoke(this);
			}
		}

		public event InitCompleteHandler InitCompleteEvent;

		// \IInitable

		void IDisposable.Dispose()
		{
			_invalidateObservable.OnCompleted();
			_handlers.Dispose();
		}

		/// <summary>
		/// Register play the game.
		/// </summary>
		/// <param name="gameConfig">Game presets.</param>
		/// <param name="nextGameSpawnDelaySec">Time in seconds after which the new game will be available.</param>
		/// <returns>Returns true if game is available.</returns>
		public bool PlayTheGame(GameConfig gameConfig, out float? nextGameSpawnDelaySec)
		{
			Assert.IsTrue(IsInited, "Controller must be initialized first.");

			var numGames = _userModel.GetNumGames(gameConfig, false, out nextGameSpawnDelaySec);
			if (numGames > 0)
			{
				if (_userModel.NumBonusGames > 0)
				{
					_userModel.NumBonusGames -= 1;
				}
				else
				{
					_userModel.NumGames = numGames - 1;
					_userModel.LastGameTimestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
				}

				_invalidateObservable.OnNext(true);
				return true;
			}

			return false;
		}

		/// <summary>
		/// Change the user's score.
		/// </summary>
		/// <param name="value">Delta score.</param>
		/// <returns>New value of the user's score.</returns>
		public long AddScore(long value)
		{
			Assert.IsTrue(IsInited, "Controller must be initialized first.");

			var newScore = Math.Max(0L, _userModel.Score + value);
			if (_userModel.Score != newScore)
			{
				_userModel.Score = newScore;
				_invalidateObservable.OnNext(true);
			}

			return _userModel.Score;
		}

		/// <summary>
		/// Add number of the games.
		/// </summary>
		/// <param name="value">Additional games number.</param>
		/// <param name="gameConfig">Game presets.</param>
		/// <returns>New number of the games.</returns>
		public int AddNumGames(int value, GameConfig gameConfig)
		{
			Assert.IsTrue(IsInited, "Controller must be initialized first.");
			Assert.IsTrue(value > 0, "Try to add zero or negative game number.");

			_userModel.NumGames = _userModel.GetNumGames(gameConfig, false, out _) + value;
			_invalidateObservable.OnNext(true);

			return _userModel.NumGames;
		}

		/// <summary>
		/// Add bonus games.
		/// </summary>
		/// <param name="value">Delta number of the bonus games.</param>
		/// <returns>New number of the bonus games.</returns>
		public int AddNumBonusGames(int value)
		{
			Assert.IsTrue(IsInited, "Controller must be initialized first.");

			var newBonusGames = Mathf.Max(0, _userModel.NumBonusGames + value);
			if (_userModel.NumBonusGames != newBonusGames)
			{
				_userModel.NumBonusGames = newBonusGames;
				_invalidateObservable.OnNext(true);
			}

			return _userModel.NumBonusGames;
		}

		/// <summary>
		/// Add additional steps.
		/// </summary>
		/// <param name="value">Additional steps number.</param>
		/// <returns>New number of the additional steps.</returns>
		public int AddAdditionalSteps(int value)
		{
			Assert.IsTrue(IsInited, "Controller must be initialized first.");
			Assert.IsTrue(value > 0, "Try to add zero or negative additional steps.");

			_userModel.AdditionalGameSteps += value;
			_invalidateObservable.OnNext(true);

			return _userModel.AdditionalGameSteps;
		}

		/// <summary>
		/// Decrease the number of the additional steps.
		/// </summary>
		/// <returns>New number of the additional steps.</returns>
		public int DecrementAdditionalSteps()
		{
			Assert.IsTrue(IsInited, "Controller must be initialized first.");

			if (_userModel.AdditionalGameSteps <= 0)
			{
				Debug.LogError("Try to decrement empty additional steps.");
				return 0;
			}

			--_userModel.AdditionalGameSteps;
			_invalidateObservable.OnNext(true);

			return _userModel.AdditionalGameSteps;
		}
	}
}