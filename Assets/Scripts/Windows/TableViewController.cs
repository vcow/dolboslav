using System.Collections.Generic;
using System.Linq;
using Game.Model;
using UniRx;
using UnityEngine;
using User.Model;
using Zenject;

namespace Windows
{
	/// <summary>
	/// Controller for History table UI.
	/// </summary>
	[DisallowMultipleComponent]
	public sealed class TableViewController : MonoBehaviour
	{
		private readonly CompositeDisposable _handlers = new();

		// The view of additional table line for "Favor of the gods" option.
		[SerializeField] private GameObject _additionalBlock;
		[SerializeField] private List<TableRecordViewController> _records;

		[Inject] private readonly IGameModel _gameModel;
		[Inject] private readonly IUserModel _userModel;

		private void Start()
		{
			_additionalBlock.SetActive(_userModel.HasAdditionalGameStep);

			// Append current state to the History.
			var steps = _gameModel.History.Append(_gameModel.CurrentStep).ToArray();

			// Bind records to the lines
			var numRecords = Mathf.Min(_records.Count, steps.Length);
			var i = 0;
			for (; i < numRecords; ++i)
			{
				_records[i].StepRecord = steps[i];
			}

			for (; i < _records.Count; ++i)
			{
				_records[i].StepRecord = null;
			}

			_gameModel.History.ObserveAdd().Subscribe(_ =>
			{
				// Round is over when the new line added to the History. Stop watching changes here.
				foreach (var record in _records)
				{
					record.Dispose();
				}
			}).AddTo(_handlers);
		}

		private void OnDestroy()
		{
			_handlers.Dispose();
		}
	}
}