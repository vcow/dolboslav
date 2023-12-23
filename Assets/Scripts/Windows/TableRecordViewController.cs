using System;
using System.Collections.Generic;
using DG.Tweening;
using Game.Model;
using TMPro;
using UniRx;
using UnityEngine;
using Zenject;

namespace Windows
{
	/// <summary>
	/// Controller for the one record of the table in the table in scene.
	/// </summary>
	[DisallowMultipleComponent]
	public class TableRecordViewController : MonoBehaviour, IDisposable
	{
		private const float PulseDelay = 1f;

		private readonly CompositeDisposable _handlers = new();
		private IStepRecord _stepRecord;
		private Tween _pulseTween;

		[SerializeField] private TextMeshProUGUI _result;
		[SerializeField, Header("Points")] private TablePointViewController _point1;
		[SerializeField] private TablePointViewController _point2;
		[SerializeField] private TablePointViewController _point3;
		[SerializeField] private TablePointViewController _point4;

		[Inject] private readonly GameModelDecorator _gameModelDecorator;

		[Inject]
		private void Construct([InjectOptional] IStepRecord stepRecord)
		{
			if (stepRecord != null)
			{
				StepRecord = stepRecord;
			}
		}

		/// <summary>
		/// Pulse points in the record to attract attention.
		/// </summary>
		/// <param name="play">Start pulse animation if true, otherwise stop.</param>
		protected virtual void PulseRecord(bool play)
		{
			if (play && _pulseTween == null)
			{
				var delay = 0f;
				const float delta = 0.1f;
				var seq = DOTween.Sequence();
				foreach (var point in ToPointsEnumerable())
				{
					delay += delta;
					seq.Join(DOVirtual.DelayedCall(delay, () => point.Pulse()));
				}

				seq.SetDelay(PulseDelay).SetLoops(-1);
				_pulseTween = seq;
			}
			else if (!play && _pulseTween != null)
			{
				_pulseTween.Kill();
				_pulseTween = null;

				foreach (var point in ToPointsEnumerable())
				{
					point.Pulse(false);
				}
			}
		}

		/// <summary>
		/// Record to view.
		/// </summary>
		public IStepRecord StepRecord
		{
			get => _stepRecord;
			set
			{
				if (value == _stepRecord)
				{
					return;
				}

				_stepRecord = value;
				ClearRecord();

				if (_stepRecord != null)
				{
					if (_stepRecord.Result.HasValue)
					{
						var (guess, correct) = _stepRecord.Result.Value;
						_result.text = $"<color=#006d08>{correct}</color>/{guess + correct}";
					}
					else
					{
						_result.text = string.Empty;
					}

					_stepRecord.Idol1Color.Subscribe(color => _point1.Color = color).AddTo(_handlers);
					_stepRecord.Idol2Color.Subscribe(color => _point2.Color = color).AddTo(_handlers);
					_stepRecord.Idol3Color.Subscribe(color => _point3.Color = color).AddTo(_handlers);
					_stepRecord.Idol4Color.Subscribe(color => _point4.Color = color).AddTo(_handlers);

					_gameModelDecorator.DuplicateFromHistory
						.Subscribe(duplicate => PulseRecord(_stepRecord.Equals(duplicate)))
						.AddTo(_handlers);
				}
			}
		}

		private void Start()
		{
			if (_stepRecord == null)
			{
				ClearRecord();
			}
		}

		private void ClearRecord()
		{
			_handlers.Clear();

			_result.text = string.Empty;
			foreach (var point in ToPointsEnumerable())
			{
				point.Color = IdolColor.Undefined;
			}
		}

		protected virtual void OnDestroy()
		{
			Dispose();
		}

		public void Dispose()
		{
			PulseRecord(false);
			_handlers.Dispose();
		}

		protected IEnumerable<TablePointViewController> ToPointsEnumerable()
		{
			yield return _point1;
			yield return _point2;
			yield return _point3;
			yield return _point4;
		}
	}
}