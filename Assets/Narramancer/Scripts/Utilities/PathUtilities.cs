
using System.IO;
using UnityEngine;

namespace Narramancer {

	public static class PathUtilities {

		public static string AsAbsolutePath(string path) {

			// if the path already contains the dataPath -> already absolute
			if (path.Contains(Application.dataPath)) {
				return path;
			}

			//If this path is an assets path, it probably starts with "Assets/"
			if (path.Contains("Assets")) {
				path = path.RemoveSubstringAndRemoveBefore("Assets");
			}

			if (!path.StartsWith("/")) {
				path = "/" + path;
			}

			path = Application.dataPath + path;

			if (!path.EndsWith("/")) {
				path += "/";
			}

			return ApplyDirectorySeperator(path);
		}

		public static string AsAssetsPath(string path) {

			// if the path already contains 'Assets'
			if (path.Contains("Assets")) {
				// Remove anything that might come before the 'Assets' part and return
				path = path.RemoveBefore("Assets");
			}
			else {
				// if the path does NOT contain the folder 'Assets'
				// we can still take a stab at the path and assume that we are starting the path from the 'Assets' folder
				path = path.TrimStart('/');
				path = Path.Combine("Assets", path);
			}

			if (!path.EndsWith("/")) {
				path += "/";
			}

			return ApplyDirectorySeperator(path);
		}

		public static string CreateNewAssetPath(string directory, string assetName) {

			directory = AsAssetsPath(directory);

			if (!assetName.EndsWith(".asset")) {
				assetName += ".asset";
			}

			var path = Path.Combine(directory, assetName);

			return path;
		}

		public static string ApplyDirectorySeperator(string path) {
			path = path.Replace('/', Path.DirectorySeparatorChar);
			path = path.Replace('\\', Path.DirectorySeparatorChar);
			return path;
		}

		public static bool IsPathWithinResources( string path ) {

			if ( string.IsNullOrEmpty(path) ) {
				return false;
			}

			// ensure the path has the correct separators that we're looking for
			path = ApplyDirectorySeperator(path);

			var separator = Path.DirectorySeparatorChar;

			return path.Contains(separator + "Resources" + separator);
		}
	}
}