using System;

namespace Game.Model
{
	/// <summary>
	/// The color of idols.
	/// </summary>
	[Serializable]
	public enum IdolColor
	{
		Undefined = 0x00,
		Red = 0x01,
		Green = 0x02,
		Blue = 0x04,
		Brown = 0x08,
		Yellow = 0x10,
		Black = 0x20
	}
}