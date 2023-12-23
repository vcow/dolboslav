using System.Collections.Generic;
using System.Linq;
using Game.Model;
using UniRx;
using UnityEngine;
using Zenject;
using Random = System.Random;

namespace Game
{
	/// <summary>
	/// Controller of the step result view in Scene. Displays the step result in the form of mushrooms.
	/// </summary>
	[DisallowMultipleComponent]
	public sealed class StepResultViewController : MonoBehaviour
	{
		private readonly CompositeDisposable _handlers = new();

		[SerializeField] private List<MushroomViewController> _redMushrooms;
		[SerializeField] private List<MushroomViewController> _brownMushrooms;

		[Inject] private readonly IGameModel _gameModel;

		private void Start()
		{
			_gameModel.History.ObserveAdd().Subscribe(record =>
			{
				_handlers.Clear();
				SpawnMushrooms(record.Value.Result);
			}).AddTo(_handlers);

			var allMushrooms = new[]
			{
				_redMushrooms,
				_brownMushrooms
			}.SelectMany(list => list);

			foreach (var mushroom in allMushrooms)
			{
				mushroom.Hide();
			}
		}

		private void SpawnMushrooms((int, int)? result)
		{
			if (!result.HasValue)
			{
				Debug.LogError("Can't visualize empty result.");
				return;
			}

			var (guess, correct) = result.Value;

			var exclude = new List<int>();
			var available = _redMushrooms.ToList();
			var rnd = new Random();

			while (guess-- > 0)
			{
				var mushroom = available[rnd.Next(available.Count)];
				available.Remove(mushroom);
				exclude.Add(mushroom.RandomizeView(exclude));
				ListenTriggerExitAndAppear(mushroom, 0.5f + (float)rnd.NextDouble());
			}

			exclude.Clear();
			available = _brownMushrooms.ToList();
			while (correct-- > 0)
			{
				var mushroom = available[rnd.Next(available.Count)];
				available.Remove(mushroom);
				exclude.Add(mushroom.RandomizeView(exclude));
				ListenTriggerExitAndAppear(mushroom, 0.5f + (float)rnd.NextDouble());
			}
		}

		private void ListenTriggerExitAndAppear(MushroomViewController mushroom, float delay)
		{
			mushroom.triggerExitEvent.AddListener(_ =>
			{
				if (!_gameModel.IsGameOver(out var _))
				{
					mushroom.Show(delay);
				}
			});
		}

		private void OnDestroy()
		{
			_handlers.Dispose();
		}
	}
}