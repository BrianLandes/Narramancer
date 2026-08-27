using Narramancer.OdinSerializer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Narramancer {

	[Serializable]
	public class SaveDataWrapper {
		public string title;
		public string data;
		public string thumbnail;
		public List<UnityEngine.Object> objects;
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

		/// <summary>
		/// Serializes <paramref name="data"/> to the payload string, emitting the external
		/// <see cref="UnityEngine.Object"/> references it points at into <paramref name="objects"/>. The two
		/// payload methods are the *only* place the serializer is called, so a serializer swap touches nothing
		/// else — and they let a test round-trip a story without needing a screenshot for the thumbnail.
		/// </summary>
		public static string SerializePayload<T>(T data, out List<UnityEngine.Object> objects) {
			var bytes = SerializationUtility.SerializeValue(data, DataFormat.JSON, out objects);
			return System.Text.Encoding.UTF8.GetString(bytes);
		}

		/// <inheritdoc cref="SerializePayload{T}"/>
		public static T DeserializePayload<T>(string payload, List<UnityEngine.Object> objects) {
			var bytes = System.Text.Encoding.UTF8.GetBytes(payload);
			return SerializationUtility.DeserializeValue<T>(bytes, DataFormat.JSON, objects);
		}

		public static string SerializeData<T>(T data) {
			var wrapper = new SaveDataWrapper();
			wrapper.title = DateTime.Now.ToString("dddd MMMM d yyyy, hh:mm tt");

			wrapper.data = SerializePayload(data, out wrapper.objects);

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

		public static T DeserializeData<T>(string jsonData) {
			var saveGameWrapper = JsonUtility.FromJson<SaveDataWrapper>(jsonData);

			return DeserializePayload<T>(saveGameWrapper.data, saveGameWrapper.objects);
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
