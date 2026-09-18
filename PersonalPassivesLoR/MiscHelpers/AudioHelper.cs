using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace PersonalPassivesLoR.MiscHelpers
{
    public static class AudioHelper
    {
        private static readonly string[] SupportedExtensions = { ".ogg", ".wav", ".mp3" };

        public static Dictionary<string, AudioClip> AudioStorage { get; private set; } =
            new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);

        public static void ChangeBGM(string path, float volume = 1f, float trimEnd = 0f)
        {
            AudioClip[] array = new AudioClip[3];
            AudioHelper.AudioStorage.TryGetValue(path, out AudioClip audioClip);
            if (audioClip != null)
            {
                if (volume != 1f)
                {
                    audioClip.SetVolume(volume);
                }
                if (trimEnd > 0f)
                {
                    audioClip = Trim(audioClip, trimEndSeconds: trimEnd);
                }
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = audioClip;
                }
                SingletonBehavior<BattleSoundManager>.Instance.SetAllyTheme(array);
                SingletonBehavior<BattleSoundManager>.Instance.SetEnemyTheme(array);
                SingletonBehavior<BattleSoundManager>.Instance.ChangeEnemyTheme(0);
                return;
            }
            else
            {
                Debug.LogError("Bgm Not found: Custom BGM Loader");
            }
        }

        public static AudioClip Crop(this AudioClip clip, float startTimeSeconds, float endTimeSeconds)
        {
            if (clip == null) return null;

            int frequency = clip.frequency;
            int channels = clip.channels;
            float originalDuration = clip.length;

            // Clamp values to ensure valid boundaries
            startTimeSeconds = Mathf.Clamp(startTimeSeconds, 0f, originalDuration);
            endTimeSeconds = Mathf.Clamp(endTimeSeconds, startTimeSeconds, originalDuration);

            float newDuration = endTimeSeconds - startTimeSeconds;
            if (newDuration <= 0f) return clip;

            int startSample = (int)(startTimeSeconds * frequency);
            int totalNewSamples = (int)(newDuration * frequency);

            // Array size accounts for multi-channel audio (e.g., stereo = 2 channels)
            float[] sampleBuffer = new float[totalNewSamples * channels];

            // Retrieve raw samples starting from the target start offset
            clip.GetData(sampleBuffer, startSample);

            // Create new AudioClip with trimmed duration
            AudioClip croppedClip = AudioClip.Create(
                $"{clip.name}_Cropped",
                totalNewSamples,
                channels,
                frequency,
                false
            );

            croppedClip.SetData(sampleBuffer, 0);
            return croppedClip;
        }

        public static AudioClip Trim(this AudioClip clip, float trimStartSeconds = 0f, float trimEndSeconds = 0f)
        {
            if (clip == null) return null;

            float startTime = trimStartSeconds;
            float endTime = clip.length - trimEndSeconds;

            return clip.Crop(startTime, endTime);
        }

        public static void SetVolume(this AudioClip clip, float volume)
        {
            if (clip == null) return;

            // Retrieve raw floating-point audio samples (-1.0 to 1.0)
            float[] samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            // Scale each sample and clamp to prevent audio distortion/clipping
            for (int i = 0; i < samples.Length; i++)
            {
                samples[i] = Mathf.Clamp(samples[i] * volume, -1.0f, 1.0f);
            }

            // Apply modified samples back to the clip
            clip.SetData(samples, 0);
        }

        public static async Task LoadAllBGMAsync()
        {
            try
            {
                string assemblyPath = typeof(AudioHelper).Assembly.Location;
                if (string.IsNullOrEmpty(assemblyPath))
                {
                    assemblyPath = Assembly.GetExecutingAssembly().CodeBase.Replace("file:///", "");
                }

                string assemblyDir = Path.GetDirectoryName(assemblyPath);
                string bgmDir = Path.Combine(assemblyDir, "BGM");

                if (!Directory.Exists(bgmDir))
                {
                    Debug.LogError($"[AudioHelper] BGM folder not found: {bgmDir}");
                    return;
                }

                string[] files = Directory.GetFiles(bgmDir, "*.*", SearchOption.AllDirectories)
                    .Where(file => SupportedExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()))
                    .ToArray();

                AudioStorage.Clear();

                foreach (string filePath in files)
                {
                    string relativePath = filePath.Substring(bgmDir.Length)
                                                  .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                                                  .Replace('\\', '/');

                    string key = Path.ChangeExtension(relativePath, null);

                    AudioClip clip = await LoadClipFromFileAsync(filePath);
                    if (clip != null)
                    {
                        clip.name = Path.GetFileNameWithoutExtension(filePath);
                        AudioStorage[key] = clip;
                        Debug.Log($"[AudioHelper] Successfully loaded key: '{key}'");
                    }
                }

                Debug.Log($"[AudioHelper] Loading complete. Total audio clips stored: {AudioStorage.Count}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AudioHelper] Exception during audio loading: {ex}");
            }
        }

        private static async Task<AudioClip> LoadClipFromFileAsync(string fullPath)
        {
            string ext = Path.GetExtension(fullPath).ToLowerInvariant();
            if (ext == ".mp3")
            {
                Debug.LogWarning($"[AudioHelper] Unity Standalone PC does not support runtime MP3 streaming ('{fullPath}'). Convert this file to .ogg or .wav.");
                return null;
            }

            string fileUri = new Uri(fullPath).AbsoluteUri;
            AudioType audioType = GetAudioType(fullPath);

            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(fileUri, audioType))
            {
                // Force non-streamed loading to avoid stream errors on desktop standalone
                ((DownloadHandlerAudioClip)www.downloadHandler).streamAudio = false;

                var operation = www.SendWebRequest();

                while (!operation.isDone)
                {
                    await Task.Yield();
                }

#if UNITY_2020_1_OR_NEWER
            if (www.result != UnityWebRequest.Result.Success)
#else
                if (www.isNetworkError || www.isHttpError)
#endif
                {
                    Debug.LogError($"[AudioHelper] WebRequest error for '{fullPath}': {www.error}");
                    return null;
                }

                return DownloadHandlerAudioClip.GetContent(www);
            }
        }

        private static AudioType GetAudioType(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            switch (ext)
            {
                case ".ogg":
                    return AudioType.OGGVORBIS;
                case ".wav":
                    return AudioType.WAV;
                default:
                    return AudioType.UNKNOWN;
            }
        }
    }
}
