using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Audio;
using Zenject;

namespace Sound
{
	/// <summary>
	/// Sound manager. This component must be presented in the prefab which must be added to the ProjectContext
	/// Prefab Installers list.
	/// </summary>
	[DisallowMultipleComponent, RequireComponent(typeof(AudioSource))]
	public class SoundManager : MonoInstaller<SoundManager>, ISoundManager
	{
		[Flags]
		private enum Mute
		{
			None = 0x00,
			MuteMusic = 0x01,
			MuteSound = 0x02
		}

		private const string MusicVol = "MusicVolume";
		private const string SoundVol = "SoundVolume";
		private const string MusicCutoffFreq = "MusicCutoffFreq";
		private const string MutePrefKey = "MuteSettings";
		private const float VolMin = -80f;
		private const float VolMax = 0f;
		private const float FreqMin = 500f;
		private const float FreqMax = 5000f;
		private const float MusicOffDuration = 0.5f;

		private AudioSource _musicAudioSource;
		private readonly Lazy<Dictionary<string, AudioClip>> _clipsMap;

		private Tween _tween;

		private Mute _muteState = Mute.None;

		private readonly List<AudioSource> _soundPool = new();

		[SerializeField] private AudioMixer _audioMixer;
		[SerializeField, Header("Prefabs")] private AudioSource _soundSourcePrefab;
		[SerializeField, Header("Sound list")] private List<AudioClip> _clips;

		public SoundManager()
		{
			_clipsMap = new Lazy<Dictionary<string, AudioClip>>(() => (_clips ?? new List<AudioClip>())
				.Select(clip => (clip.name, clip))
				.GroupBy(tuple => tuple.name)
				.ToDictionary(tuples => tuples.Key, tuples => tuples.First().clip)
			);
		}

		public override void InstallBindings()
		{
			Container.Bind<ISoundManager>().FromInstance(this).AsSingle();
		}

		private void Awake()
		{
			_musicAudioSource = GetComponent<AudioSource>();
			Assert.IsTrue(_musicAudioSource && _audioMixer, "AudioSource and Audio Mixer reference must have.");
		}

		public override void Start()
		{
			if (PlayerPrefs.HasKey(MutePrefKey))
			{
				var muteState = (Mute)PlayerPrefs.GetInt(MutePrefKey);
				MusicIsOn = (muteState & Mute.MuteMusic) == 0;
				SoundIsOn = (muteState & Mute.MuteSound) == 0;
			}
		}

		private void OnDestroy()
		{
			_tween?.Kill(true);
		}

		// ISoundManager

		public void PlaySound(string soundName, float? delaySec = null)
		{
			if (!SoundIsOn)
			{
				return;
			}

			var clip = _clipsMap.Value.TryGetValue(soundName, out var c) ? c : null;
			if (!clip)
			{
				Debug.LogErrorFormat("There is no audio clip with the name {0}.", soundName);
				return;
			}

			var src = _soundPool.FirstOrDefault(source => !source.isPlaying);
			if (!src)
			{
				Assert.IsTrue(_soundSourcePrefab, "Sound source prefab must have.");
				src = Container.InstantiatePrefab(_soundSourcePrefab, transform).GetComponent<AudioSource>();
				_soundPool.Add(src);
			}

			src.clip = clip;

			if (delaySec.HasValue && delaySec.Value > 0)
			{
				src.PlayDelayed(delaySec.Value);
			}
			else
			{
				src.Play();
			}
		}

		public void PlayMusic(string musicName, float fadeDurationSec = 0.75f)
		{
			if (musicName == CurrentMusicName)
			{
				return;
			}

			if (_musicAudioSource.clip)
			{
				_tween?.Kill();
				_tween = DOTween.To(() => MusicVolume, vol => MusicVolume = vol, 0f, MusicOffDuration)
					.SetEase(Ease.InQuad)
					.OnComplete(() =>
					{
						_tween = null;
						_musicAudioSource.Stop();
						_musicAudioSource.clip = null;

						StartMusic(musicName, fadeDurationSec);
					});
			}
			else
			{
				StartMusic(musicName, fadeDurationSec);
			}
		}

		public bool SoundIsOn
		{
			get => (_muteState & Mute.MuteSound) == 0;
			set
			{
				if (value == SoundIsOn)
				{
					return;
				}

				if (!value)
				{
					_soundPool.ForEach(source => source.Stop());
				}

				_muteState = (value ? Mute.None : Mute.MuteSound) | (MusicIsOn ? Mute.None : Mute.MuteMusic);
				PlayerPrefs.SetInt(MutePrefKey, (int)_muteState);
			}
		}

		public bool MusicIsOn
		{
			get => (_muteState & Mute.MuteMusic) == 0;
			set
			{
				if (value == MusicIsOn)
				{
					return;
				}

				_musicAudioSource.enabled = value;
				_muteState = (SoundIsOn ? Mute.None : Mute.MuteSound) | (value ? Mute.None : Mute.MuteMusic);
				PlayerPrefs.SetInt(MutePrefKey, (int)_muteState);
			}
		}

		public float MusicVolume
		{
			get => _audioMixer.GetFloat(MusicVol, out var v) ? Mathf.Clamp01((v - VolMin) / (VolMax - VolMin)) : 0f;
			set => _audioMixer.SetFloat(MusicVol, Mathf.Lerp(VolMin, VolMax, value));
		}

		public float SoundVolume
		{
			get => _audioMixer.GetFloat(SoundVol, out var v) ? Mathf.Clamp01((v - VolMin) / (VolMax - VolMin)) : 0f;
			set => _audioMixer.SetFloat(SoundVol, Mathf.Lerp(VolMin, VolMax, value));
		}

		public void SuppressMusic(float suppressionValue)
		{
			_audioMixer.SetFloat(MusicCutoffFreq, Mathf.Lerp(FreqMax, FreqMin, Mathf.Clamp01(suppressionValue)));
		}

		// \ISoundManager

		private string CurrentMusicName => _musicAudioSource.clip != null ? _musicAudioSource.clip.name : null;

		private void StartMusic(string musicName, float fadeDuration)
		{
			Assert.IsFalse(_musicAudioSource.clip, "Previous music must stopped first.");
			var clip = _clipsMap.Value.TryGetValue(musicName, out var c) ? c : null;
			if (!clip)
			{
				Debug.LogErrorFormat("There is no audio clip with the name {0}.", musicName);
				return;
			}

			if (string.IsNullOrEmpty(musicName))
			{
				_musicAudioSource.Stop();
			}
			else
			{
				_musicAudioSource.clip = clip;

				_tween?.Kill();
				if (fadeDuration > 0)
				{
					MusicVolume = 0f;
					_tween = DOTween.To(() => MusicVolume, vol => MusicVolume = vol, 1f, fadeDuration)
						.SetEase(Ease.OutQuad)
						.OnComplete(() => _tween = null);
				}
				else
				{
					MusicVolume = 1f;
					_tween = null;
				}

				_musicAudioSource.Play();
			}
		}
	}
}