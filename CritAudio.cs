using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace BlackFlashCrit {
	internal static class CritAudio {
		private static readonly List<AudioClip> s_Clips = new List<AudioClip>();
		private static readonly List<AudioSource> s_Sources = new List<AudioSource>();
		private static int s_NextVoice;
		private static System.Random s_Rng = new System.Random();

		private static GameObject s_AudioRoot;
		private static CritAudioRunner s_Runner;

		private const string AudioFolderName = "sounds";

		internal static void Init (ConfigFile config, string pluginDir) {
			EnsureRootAndRunner();
			BuildAudioPool();

			var audioDir = Path.Combine(pluginDir, AudioFolderName);
			s_Runner.StartCoroutine(LoadAllClipsCoroutine(audioDir));
		}

		private static void EnsureRootAndRunner () {
			if (s_AudioRoot != null && s_Runner != null) return;
			s_AudioRoot = new GameObject("BlackFlashCrit_Audio");
			UnityEngine.Object.DontDestroyOnLoad(s_AudioRoot);
			s_Runner = s_AudioRoot.AddComponent<CritAudioRunner>();
		}

		private static void BuildAudioPool () {
			// Clear existing
			for (int i = 0; i < s_Sources.Count; i++) {
				if (s_Sources[i]) UnityEngine.Object.Destroy(s_Sources[i].gameObject);
			}
			s_Sources.Clear();
			s_NextVoice = 0;

			int count = Mathf.Clamp(AudioSetting.MaxVoices.Value, 1, 16);
			for (int i = 0; i < count; i++) {
				var go = new GameObject($"BlackFlashCrit_Audio_{i}");
				go.transform.SetParent(s_AudioRoot.transform, false);
				var src = go.AddComponent<AudioSource>();
				src.playOnAwake = false;
				src.loop = false;
				// At the moment, 2D audio only
				src.spatialBlend = 0f;
				src.rolloffMode = AudioRolloffMode.Linear;
				s_Sources.Add(src);
			}
		}

		private static System.Collections.IEnumerator LoadAllClipsCoroutine (string audioDir) {
			s_Clips.Clear();

			if (!Directory.Exists(audioDir)) {
				Log.Warn($"Audio directory not found at {audioDir}. No crit sounds loaded.");
				yield break;
			}

			string[] files;
			try {
				files = Directory.GetFiles(audioDir);
			}
			catch (Exception e) {
				Log.Error($"Error enumerating audio files: {e.Message}");
				yield break;
			}

			foreach (var path in files) {
				string ext = Path.GetExtension(path).ToLowerInvariant();
				AudioType type;
				switch (ext) {
					case ".wav": type = AudioType.WAV; break;
					case ".ogg": type = AudioType.OGGVORBIS; break;
					case ".mp3": type = AudioType.MPEG; break;
					default: continue;
				}

				string uri = new Uri(path).AbsoluteUri;
				using (var req = UnityWebRequestMultimedia.GetAudioClip(uri, type)) {

					yield return req.SendWebRequest();
					bool ok = req.result == UnityWebRequest.Result.Success;

					if (!ok) {
						Log.Warn($"Audio load failed: {Path.GetFileName(path)} ({req.error})");
						continue;
					}

					var clip = DownloadHandlerAudioClip.GetContent(req);
					if (clip == null) {
						Log.Warn($"Audio decode returned null: {Path.GetFileName(path)}");
						continue;
					}

					clip.name = $"BFC_{Path.GetFileNameWithoutExtension(path)}";
					s_Clips.Add(clip);
					Log.Info($"Crit audio loaded: {Path.GetFileName(path)}");
				}
			}

			if (s_Clips.Count == 0) Log.Warn("No crit audio clips found. Drop .wav/.ogg/.mp3 files into the 'sounds' folder.");
		}

		internal static void PlayRandomCritSFX (Vector3 worldPos) {
			if (!BlackFlashCrit.ModEnabled.Value) return;
			if (!AudioSetting.EnableCritSounds.Value) return;
			if (s_Clips.Count == 0) return;
			if (s_Sources.Count == 0) return;

			// Choose a free voice if possible
			AudioSource src = null;
			for (int i = 0; i < s_Sources.Count; i++) {
				if (!s_Sources[i].isPlaying) { src = s_Sources[i]; break; }
			}
			if (src == null) {
				src = s_Sources[s_NextVoice];
				s_NextVoice = (s_NextVoice + 1) % s_Sources.Count;
			}

			// Pick a random clip
			int idx = s_Rng.Next(s_Clips.Count);
			var clip = s_Clips[idx];
			if (clip == null) return;

			// Configure and play
			float vol = Mathf.Clamp01(AudioSetting.CritSoundVolume.Value);
			float pMin = Mathf.Min(AudioSetting.CritSoundPitchMin.Value, AudioSetting.CritSoundPitchMax.Value);
			float pMax = Mathf.Max(AudioSetting.CritSoundPitchMin.Value, AudioSetting.CritSoundPitchMax.Value);
			src.pitch = UnityEngine.Random.Range(pMin, pMax);

			// 2D audio ignores position, left to support 3D in future
			src.transform.position = worldPos;

			src.PlayOneShot(clip, vol);
		}

		private class CritAudioRunner : MonoBehaviour { }
	}
}