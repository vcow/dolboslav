using System;
using System.Linq;
using UniRx;

namespace Game.Model
{
	/// <summary>
	/// Model of the one Game round.
	/// </summary>
	public sealed class GameModel : IGameModel, IDisposable
	{
		private readonly CompositeDisposable _handlers;

		/// <summary>
		/// Current state r/w (for controller only).
		/// </summary>
		public readonly StepRecord CurrentStep;

		/// <summary>
		/// List of the all previous steps r/w (for controller only).
		/// </summary>
		public readonly ReactiveCollection<IStepRecord> History;

		// IGameModel

		public IStepRecord Target { get; }
		IStepRecord IGameModel.CurrentStep => CurrentStep;
		IReadOnlyReactiveCollection<IStepRecord> IGameModel.History => History;
		public int MaxSteps { get; }

		// \IGameModel

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="maxSteps">Max round steps.</param>
		public GameModel(int maxSteps)
		{
			MaxSteps = maxSteps;

			// Fill Target with random colors
			var rnd = new Random();
			var allColors = Enum.GetValues(typeof(IdolColor)).Cast<IdolColor>()
				.Except(new[] { IdolColor.Undefined }).ToList();

			var index = rnd.Next(allColors.Count);
			var c1 = allColors[index];
			allColors.RemoveAt(index);

			index = rnd.Next(allColors.Count);
			var c2 = allColors[index];
			allColors.RemoveAt(index);

			index = rnd.Next(allColors.Count);
			var c3 = allColors[index];
			allColors.RemoveAt(index);

			index = rnd.Next(allColors.Count);
			var c4 = allColors[index];
			allColors.RemoveAt(index);

			Target = new StepRecord(c1, c2, c3, c4);

			// Initial current state is empty.
			CurrentStep = new StepRecord(IdolColor.Undefined, IdolColor.Undefined,
				IdolColor.Undefined, IdolColor.Undefined);

			// All IDisposables must be added to the CompositeDisposable.
			_handlers = new CompositeDisposable(new[]
				{
					Target,
					CurrentStep,
				}
				.Cast<IDisposable>());

			History = new ReactiveCollection<IStepRecord>();
			_handlers.Add(History);
		}

		void IDisposable.Dispose()
		{
			_handlers.Dispose();
		}

		/// <summary>
		/// Implementation of the IStepRecord interface.
		/// </summary>
		public class StepRecord : IStepRecord, IDisposable
		{
			private readonly CompositeDisposable _handlers;

			public readonly ReactiveProperty<IdolColor> Idol1Color;
			public readonly ReactiveProperty<IdolColor> Idol2Color;
			public readonly ReactiveProperty<IdolColor> Idol3Color;
			public readonly ReactiveProperty<IdolColor> Idol4Color;
			public (int guess, int correct)? Result;

			/// <summary>
			/// Constructor.
			/// </summary>
			/// <param name="idol1Color">Color of the first idol.</param>
			/// <param name="idol2Color">Color of the second idol.</param>
			/// <param name="idol3Color">Color of the third idol.</param>
			/// <param name="idol4Color">Color or the fourth idol.</param>
			public StepRecord(IdolColor idol1Color, IdolColor idol2Color, IdolColor idol3Color, IdolColor idol4Color)
			{
				Idol1Color = new ReactiveProperty<IdolColor>(idol1Color);
				Idol2Color = new ReactiveProperty<IdolColor>(idol2Color);
				Idol3Color = new ReactiveProperty<IdolColor>(idol3Color);
				Idol4Color = new ReactiveProperty<IdolColor>(idol4Color);

				// All IDisposables must be added to the CompositeDisposable.
				_handlers = new CompositeDisposable(Idol1Color, Idol2Color, Idol3Color, Idol4Color);
			}

			// IStepRecord

			IReadOnlyReactiveProperty<IdolColor> IStepRecord.Idol1Color => Idol1Color;
			IReadOnlyReactiveProperty<IdolColor> IStepRecord.Idol2Color => Idol2Color;
			IReadOnlyReactiveProperty<IdolColor> IStepRecord.Idol3Color => Idol3Color;
			IReadOnlyReactiveProperty<IdolColor> IStepRecord.Idol4Color => Idol4Color;
			(int guess, int correct)? IStepRecord.Result => Result;

			// \IStepRecord

			public bool Equals(IStepRecord other)
			{
				return other == this ||
				       other != null &&
				       Idol1Color.Value == other.Idol1Color.Value &&
				       Idol2Color.Value == other.Idol2Color.Value &&
				       Idol3Color.Value == other.Idol3Color.Value &&
				       Idol4Color.Value == other.Idol4Color.Value;
			}

			void IDisposable.Dispose()
			{
				_handlers.Dispose();
			}

			public override string ToString()
			{
				return $"[{Idol1Color.Value}], [{Idol2Color.Value}], [{Idol3Color.Value}], [{Idol4Color.Value}]" +
				       (Result.HasValue ? $"({Result.Value.guess}/{Result.Value.correct})" : string.Empty);
			}
		}
	}
}