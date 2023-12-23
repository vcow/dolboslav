using System;
using System.Linq;
using Game.Model;
using UnityEngine;

namespace Game
{
	/// <summary>
	/// Controller for the GameModel which is only one that should change the GameModel.
	/// All other classes must receive the GameModel through the read-only IGameModel interface.
	/// </summary>
	public class GameModelController
	{
		private readonly GameModel _gameModel;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="gameModel">Game model to work.</param>
		public GameModelController(GameModel gameModel)
		{
			_gameModel = gameModel;
		}

		/// <summary>
		/// Get the color of the specified idol.
		/// </summary>
		/// <param name="idolNum">Index of the idol (1..4).</param>
		/// <returns>Color or the specified idol.</returns>
		/// <exception cref="NotSupportedException">Index out of range.</exception>
		public IdolColor GetCurrentStepColor(int idolNum)
		{
			return idolNum switch
			{
				1 => _gameModel.CurrentStep.Idol1Color.Value,
				2 => _gameModel.CurrentStep.Idol2Color.Value,
				3 => _gameModel.CurrentStep.Idol3Color.Value,
				4 => _gameModel.CurrentStep.Idol4Color.Value,
				_ => throw new NotSupportedException()
			};
		}

		/// <summary>
		/// Set color for the specified idol.
		/// </summary>
		/// <param name="idolNum">Index of the idol (1..4).</param>
		/// <param name="color">New idols color.</param>
		public void SetCurrentStepColor(int idolNum, IdolColor color)
		{
			var idols = new[]
			{
				_gameModel.CurrentStep.Idol1Color,
				_gameModel.CurrentStep.Idol2Color,
				_gameModel.CurrentStep.Idol3Color,
				_gameModel.CurrentStep.Idol4Color
			};

			// Set new color for specified idol and set color of other idol of the same color
			// (if exists) to undefined.
			for (var i = 1; i <= 4; ++i)
			{
				var clr = idols[i - 1];
				if (i == idolNum)
				{
					clr.Value = color;
				}
				else if (clr.Value == color)
				{
					clr.Value = IdolColor.Undefined;
				}
			}
		}

		/// <summary>
		/// Make move.
		/// </summary>
		/// <returns>Returns true if move succeeded.</returns>
		/// <exception cref="Exception">The step can't be finished due to an incorrect combination.</exception>
		public bool Move()
		{
			if (_gameModel.IsGameOver(out _))
			{
				Debug.LogError("Try to pray when Game is over.");
				return false;
			}

			if (!_gameModel.CurrentStep.CheckStep())
			{
				throw new Exception("Try to pray with invalid current step.");
			}

			if (_gameModel.History.Any(record => record.Equals(_gameModel.CurrentStep)))
			{
				throw new Exception("Try to pray with the duplicate step.");
			}

			if (_gameModel.History.Count >= _gameModel.MaxSteps)
			{
				throw new Exception("Steps overflow.");
			}

			var templateSum = _gameModel.Target.ToColors()
				.Aggregate(0, (acc, src) => acc | (int)src);
			var stepSum = _gameModel.CurrentStep.ToColors()
				.Aggregate(0, (acc, src) => acc | (int)src);
			var guess = Convert.ToString(templateSum & stepSum, 2)
				.Count(c => c == '1');
			var correct = _gameModel.Target.ToColors()
				.Zip(_gameModel.CurrentStep.ToColors(), (c1, c2) => c1 == c2 ? 1 : 0)
				.Aggregate((i1, i2) => i1 + i2);
			guess -= correct;

			var newRecord = new GameModel.StepRecord(
				_gameModel.CurrentStep.Idol1Color.Value,
				_gameModel.CurrentStep.Idol2Color.Value,
				_gameModel.CurrentStep.Idol3Color.Value,
				_gameModel.CurrentStep.Idol4Color.Value)
			{
				Result = (guess, correct)
			};

			_gameModel.History.Add(newRecord);

			_gameModel.CurrentStep.Idol1Color.Value = IdolColor.Undefined;
			_gameModel.CurrentStep.Idol2Color.Value = IdolColor.Undefined;
			_gameModel.CurrentStep.Idol3Color.Value = IdolColor.Undefined;
			_gameModel.CurrentStep.Idol4Color.Value = IdolColor.Undefined;

			return true;
		}
	}
}