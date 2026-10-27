using Narramancer.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Narramancer {

	[Serializable]
	public class SaveDataWrapper {

		/// <summary>
		/// The format <see cref="data"/> is written in. Absent from a save file means <c>0</c>, which is the
		/// OdinSerializer format this plugin used before 1.0 — worth naming, because both formats use <c>$id</c>
		/// and <c>$type</c>, so an old payload handed to the current reader fails on a type name rather than on
		/// anything that says "old save".
		/// </summary>
		public int version;

		public string title;
		public string data;
		public string thumbnail;

		/// <summary>
		/// The build index of the scene the save was made in. Kept out here, beside <see cref="data"/> rather than
		/// inside it, because a load has to reopen that scene <em>before</em> reading the story: the story can name
		/// scene objects, and those only resolve once their scene is loaded.
		/// </summary>
		public int sceneIndex = -1;

		/// <summary>The format written before 1.0, by OdinSerializer. No longer readable.</summary>
		public const int OdinVersion = 0;

		/// <summary>The oldest format this build reads. Format 1 predates <see cref="sceneIndex"/> and the saved marker.</summary>
		public const int MinimumReadableVersion = 2;
	}

	public static class SaveLoadUtilities {

		public static string GetSaveDataDirectory() {
#if UNITY_EDITOR
			return Application.dataPath + "/Saves";
#else
            return Application.persistentDataPath + "/Saves";
#endif
		}

		public static void WriteSaveData(string saveName, string jsonData) {
			var saveDirectory = GetSaveDataDirectory();

			Directory.CreateDirectory(saveDirectory);

			var filePath = $"{saveDirectory}/{saveName}.json";

			File.WriteAllText(filePath, jsonData);

			// Persist to IndexedDB on WebGL, where the write above only lands in memory. No-op elsewhere.
			// If a delete-save path is ever added, flush after the File.Delete too, or the deleted save
			// reappears on the next page load.
			SaveFileSync.Flush();
		}

		public static string ReadSaveData(string saveName) {
			var saveDirectory = GetSaveDataDirectory();

			var filePath = $"{saveDirectory}/{saveName}.json";

			var jsonData = File.ReadAllText(filePath);
			return jsonData;
		}

		private static NarramancerSerializer serializer = null;

		/// <summary>
		/// The serializer every save goes through. References are keyed by asset GUID, Resources path, or a
		/// runtime registry key — never by <c>instanceID</c>, which is handed out per session and so broke every
		/// saved reference whenever a game shipped a patch.
		/// </summary>
		public static NarramancerSerializer Serializer {
			get {
				if (serializer == null) {
					// PersistentId owns the shared resolver, so saved references and blackboard keys
					// cannot disagree about what an asset is called.
					serializer = new NarramancerSerializer(PersistentId.Resolver);
				}
				return serializer;
			}
		}

		/// <summary>
		/// Drops the cached serializer, so the next save picks up a freshly baked asset manifest. Only needed
		/// after rebuilding the manifest in the editor.
		/// </summary>
		public static void ResetSerializer() {
			serializer = null;
			PersistentId.Reset();
		}

		/// <summary>
		/// Serializes <paramref name="data"/> to the payload string. This and its counterpart are the *only*
		/// place the serializer is called, so a serializer change touches nothing else — and they let a test
		/// round-trip a story without needing a screenshot for the thumbnail.
		/// </summary>
		public static string SerializePayload<T>(T data) {
			return Serializer.Serialize(data);
		}

		/// <inheritdoc cref="SerializePayload{T}"/>
		public static T DeserializePayload<T>(string payload) {
			return Serializer.Deserialize<T>(payload);
		}

		public static string SerializeData<T>(T data) {
			var wrapper = new SaveDataWrapper();
			wrapper.version = NarramancerSerializer.CurrentSchemaVersion;
			wrapper.title = DateTime.Now.ToString("dddd MMMM d yyyy, hh:mm tt");

			wrapper.data = SerializePayload(data);
			if (data is StoryInstance story) {
				wrapper.sceneIndex = story.sceneIndex;
			}

			var thumbnailTexture = Screenshot();
			wrapper.thumbnail = Convert.ToBase64String(thumbnailTexture.EncodeToJPG());

			var jsonString = JsonUtility.ToJson(wrapper);
			return jsonString;
		}

		private static RenderTexture thumbnailRenderTexture;

		public static Texture2D Screenshot() {
			var texture2d = ScreenCapture.CaptureScreenshotAsTexture();
			return texture2d;
		}

		public static SaveDataWrapper DeserializeWrapper(string jsonData) {
			var saveGameWrapper = JsonUtility.FromJson<SaveDataWrapper>(jsonData);
			return saveGameWrapper;
		}

		/// <summary>Reads a save file's envelope and checks this build can read it, without reading the story.</summary>
		public static SaveDataWrapper ReadSaveWrapper(string saveName) {
			var wrapper = DeserializeWrapper(ReadSaveData(saveName));
			RequireReadableVersion(wrapper);
			return wrapper;
		}

		public static T DeserializeData<T>(string jsonData) {
			var saveGameWrapper = JsonUtility.FromJson<SaveDataWrapper>(jsonData);
			RequireReadableVersion(saveGameWrapper);
			return DeserializePayload<T>(saveGameWrapper.data);
		}

		/// <summary>
		/// Rejects a save this build cannot read, with a message that says so. Without the check an old
		/// OdinSerializer payload would surface as a type-resolution failure deep in the reader — true, but no
		/// help to anyone reading the log.
		/// </summary>
		public static void RequireReadableVersion(SaveDataWrapper wrapper) {
			if (wrapper == null) {
				throw new Serialization.SerializationException("The save file could not be read at all — it is not a Narramancer save.");
			}
			if (wrapper.version == SaveDataWrapper.OdinVersion) {
				throw new Serialization.SerializationException(
					$"'{wrapper.title}' was written by a pre-1.0 version of Narramancer, which used a different save format. It cannot be loaded by this build.");
			}
			if (wrapper.version < SaveDataWrapper.MinimumReadableVersion) {
				throw new Serialization.SerializationException(
					$"'{wrapper.title}' was written by an earlier pre-release build (save format {wrapper.version}); this build reads formats {SaveDataWrapper.MinimumReadableVersion} to {NarramancerSerializer.CurrentSchemaVersion}.");
			}
			if (wrapper.version > NarramancerSerializer.CurrentSchemaVersion) {
				throw new Serialization.SerializationException(
					$"'{wrapper.title}' was written by a newer version of the game (save format {wrapper.version}, this build reads {NarramancerSerializer.CurrentSchemaVersion}).");
			}
		}

		public static string DeserializeTitle(string jsonData) {
			var saveGameWrapper = JsonUtility.FromJson<SaveDataWrapper>(jsonData);
			return saveGameWrapper.title;
		}

		public static T ReadAndDeserializeSaveData<T>(string saveName) {
			var saveData = ReadSaveData(saveName);
			var classData = DeserializeData<T>(saveData);
			return classData;
		}

		public static Texture2D DeserializeThumbnail(string thumbnail) {
			var texture = new Texture2D(2, 2);
			texture.wrapMode = TextureWrapMode.Clamp;
			texture.LoadImage(Convert.FromBase64String(thumbnail));
			return texture;
		}

		public static int CountSaveData() {
			return GetSaveDataNames().Length;
		}

		public static string[] GetSaveDataNames() {
			var saveDirectory = GetSaveDataDirectory();
			if (!Directory.Exists(saveDirectory)) {
				return Array.Empty<String>();
			}
			var files = Directory.GetFiles(saveDirectory, "*.json");
			files = files.Select(file => Path.GetFileNameWithoutExtension(file)).ToArray();
			return files;
		}

		public static string[] GetSaveDataTitles() {
			var saveNames = GetSaveDataNames();
			var data = saveNames.Select(saveName => ReadSaveData(saveName));
			var titles = data.Select(x => DeserializeTitle(x)).ToArray();
			return titles;
		}

		public static Tuple<string, SaveDataWrapper>[] GetSaveDataInWrappers() {
			var saveNames = GetSaveDataNames();
			var data = saveNames.Select(saveName => ReadSaveData(saveName));
			var wrappers = data.Select(x => DeserializeWrapper(x));
			return saveNames.Zip(wrappers, (name, wrapper) => Tuple.Create(name, wrapper)).ToArray();
		}
	}
}
