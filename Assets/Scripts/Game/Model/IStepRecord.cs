using System;
using UniRx;

namespace Game.Model
{
	/// <summary>
	/// Interface of the idols state record.
	/// </summary>
	public interface IStepRecord : IEquatable<IStepRecord>
	{
		/// <summary>
		/// Readonly reactive color of the first idol.
		/// </summary>
		IReadOnlyReactiveProperty<IdolColor> Idol1Color { get; }

		/// <summary>
		/// Readonly reactive color of the second idol.
		/// </summary>
		IReadOnlyReactiveProperty<IdolColor> Idol2Color { get; }

		/// <summary>
		/// Readonly reactive color of the third idol.
		/// </summary>
		IReadOnlyReactiveProperty<IdolColor> Idol3Color { get; }

		/// <summary>
		/// Readonly reactive color of the fourth idol.
		/// </summary>
		IReadOnlyReactiveProperty<IdolColor> Idol4Color { get; }

		/// <summary>
		/// The step result. Exists only for finished steps. The first value (guess) is the number of guessed colors,
		/// the second value (correct) is the number of correctly placed colors.
		/// </summary>
		(int guess, int correct)? Result { get; }
	}
}