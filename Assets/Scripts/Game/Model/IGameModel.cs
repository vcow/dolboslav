using UniRx;

namespace Game.Model
{
	/// <summary>
	/// Interface of the one game round model.
	/// </summary>
	public interface IGameModel
	{
		/// <summary>
		/// Target idols combination.
		/// </summary>
		IStepRecord Target { get; }

		/// <summary>
		/// Current idols combination.
		/// </summary>
		IStepRecord CurrentStep { get; }

		/// <summary>
		/// List of the all previous round steps.
		/// </summary>
		IReadOnlyReactiveCollection<IStepRecord> History { get; }

		/// <summary>
		/// Max available round steps.
		/// </summary>
		int MaxSteps { get; }
	}
}