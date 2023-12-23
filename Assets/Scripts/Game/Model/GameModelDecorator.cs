using System;
using System.Linq;
using UniRx;
using UnityEngine.Assertions;

namespace Game.Model
{
	/// <summary>
	/// GameModel extension class. Provides some additional reactive properties for the UI interaction.
	/// </summary>
	public sealed class GameModelDecorator : IDisposable
	{
		private readonly CompositeDisposable _handlers;

		private readonly ReactiveProperty<IStepRecord> _duplicateFromHistory;
		private readonly BoolReactiveProperty _hasValidCombination;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="gameModel">Game model.</param>
		public GameModelDecorator(IGameModel gameModel)
		{
			Assert.IsNotNull(gameModel);

			_duplicateFromHistory = new ReactiveProperty<IStepRecord>(GetCloneFromHistory());
			_hasValidCombination = new BoolReactiveProperty(gameModel.CurrentStep.CheckStep());

			// All IDisposables must be added to the CompositeDisposable.
			_handlers = new CompositeDisposable(_duplicateFromHistory, _hasValidCombination);

			// Listen for current idols color changes and update reactive properties.
			new[]
				{
					gameModel.CurrentStep.Idol1Color,
					gameModel.CurrentStep.Idol2Color,
					gameModel.CurrentStep.Idol3Color,
					gameModel.CurrentStep.Idol4Color
				}
				.CombineLatest()
				.ThrottleFrame(1)
				.Subscribe(_ =>
				{
					_duplicateFromHistory.Value = GetCloneFromHistory();
					_hasValidCombination.Value = gameModel.CurrentStep.CheckStep();
				})
				.AddTo(_handlers);

			// Reactive flag that indicates that current idols combination has no duplicates in the History.
			var hasNotDuplicate = DuplicateFromHistory
				.Select(record => record == null)
				.ToReadOnlyReactiveProperty()
				.AddTo(_handlers);

			ReadyToMove = new[]
				{
					hasNotDuplicate,
					HasValidCombination
				}
				.CombineLatestValuesAreAllTrue()
				.ToReadOnlyReactiveProperty()
				.AddTo(_handlers);

			return;

			// Find a duplicate of the current idols combination in the History.
			IStepRecord GetCloneFromHistory()
			{
				return gameModel.History.FirstOrDefault(record => record.Equals(gameModel.CurrentStep));
			}
		}

		void IDisposable.Dispose()
		{
			_handlers.Dispose();
		}

		/// <summary>
		/// Repeating combination of idols in History, if exists, otherwise null.
		/// </summary>
		public IReadOnlyReactiveProperty<IStepRecord> DuplicateFromHistory => _duplicateFromHistory;

		/// <summary>
		/// The flag indicates that current idols has valid combination: no empty and colors aren't repeated.
		/// </summary>
		public IReadOnlyReactiveProperty<bool> HasValidCombination => _hasValidCombination;

		/// <summary>
		/// The flag indicates that user can make move (no duplicates and current combination is valid).
		/// </summary>
		public IReadOnlyReactiveProperty<bool> ReadyToMove { get; }
	}
}