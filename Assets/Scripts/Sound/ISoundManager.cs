namespace Sound
{
	/// <summary>
	/// Sound manager interface.
	/// </summary>
	public interface ISoundManager
	{
		/// <summary>
		/// Play short sound. Can play multiple sounds simultaneously.
		/// </summary>
		/// <param name="soundName">The name of the sound.</param>
		/// <param name="delaySec">Start delay in seconds.</param>
		void PlaySound(string soundName, float? delaySec = null);

		/// <summary>
		/// Play background music. Can play only one music at the time.
		/// </summary>
		/// <param name="musicName">The name of the music.</param>
		/// <param name="fadeDurationSec">Start fade duration.</param>
		void PlayMusic(string musicName, float fadeDurationSec = 0.75f);

		/// <summary>
		/// Sound on/off switcher.
		/// </summary>
		bool SoundIsOn { get; set; }

		/// <summary>
		/// Music on/off switcher.
		/// </summary>
		bool MusicIsOn { get; set; }

		/// <summary>
		/// Common volume of the music (0..1).
		/// </summary>
		float MusicVolume { get; set; }

		/// <summary>
		/// Common volume of the sounds (0..1)
		/// </summary>
		float SoundVolume { get; set; }

		/// <summary>
		/// Suppress music FX.
		/// </summary>
		/// <param name="suppressionValue">Value of the FX.</param>
		void SuppressMusic(float suppressionValue);
	}
}