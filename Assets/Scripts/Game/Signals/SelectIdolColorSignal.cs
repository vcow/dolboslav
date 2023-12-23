using Game.Model;

namespace Game.Signals
{
	/// <summary>
	/// The signal from UI if user change idol's color.
	/// </summary>
	public class SelectIdolColorSignal
	{
		/// <summary>
		/// The number of idol.
		/// </summary>
		public int IdolNumber { get; }

		/// <summary>
		/// New color of the idol.
		/// </summary>
		public IdolColor IdolColor { get; }

		public SelectIdolColorSignal(int idolNumber, IdolColor idolColor)
		{
			IdolNumber = idolNumber;
			IdolColor = idolColor;
		}
	}
}