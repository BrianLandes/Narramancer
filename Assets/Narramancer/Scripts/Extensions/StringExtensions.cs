
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Narramancer {

	public static class StringExtensions {

		private static StringBuilder stringBuilder = new StringBuilder();

		public static string Nicify(this string text) {

			if (text.IsNullOrEmpty()) {
				return text;
			}

			text = Capitalize(text);

			// TODO: consider single-quotes -> track open and close, put the space outside

			var stringBuilder = new StringBuilder();

			var character = text[0];
			stringBuilder.Append(character);

			for (int i = 1; i < text.Length; i++) {
				character = text[i];
				if (Char.IsUpper(character)) {
					if (i > 0) {
						var lastCharacter = text[i - 1];
						if (!Char.IsUpper(lastCharacter) && lastCharacter != ' ' && lastCharacter != '_') {
							stringBuilder.Append(' ');
						}
					}
				}
				else
				if (character == '_') {
					if (i > 0) {
						character = ' ';
					}
					else {
						// leading underscore -> skip the character
						continue;
					}
				}
				stringBuilder.Append(character);
			}

			return stringBuilder.ToString();
		}

		public static string Capitalize(this string text) {
			if (text.IsNullOrEmpty()) {
				return text;
			}
			return Char.ToUpper(text[0]) + text.Substring(1);
		}

		public static string Uncapitalize(this string text) {
			if (text.IsNullOrEmpty()) {
				return text;
			}
			return Char.ToLower(text[0]) + text.Substring(1);
		}

		public static bool IsNotNullOrEmpty(this string text) {
			return !string.IsNullOrEmpty(text);
		}

		public static bool IsNullOrEmpty(this string text) {
			return string.IsNullOrEmpty(text);
		}

		public static string Remove(this string original, string removeSubstring) {
			return original.Replace(removeSubstring, "");
		}

		public static string RemoveAny(this string original, IEnumerable<string> removeSubstrings) {
			var resultString = original;
			foreach (var substring in removeSubstrings) {
				resultString = resultString.Remove(substring);
			}
			return resultString;
		}

		public static bool Contains(this string original, string substring) {
			var index = original.IndexOf(substring);
			return index >= 0;
		}

		public static string RemoveSubstringAndRemoveBefore(this string original, string substring) {
			var index = original.IndexOf(substring);
			if (index >= 0) {
				// Chop it off
				var length = substring.Length;
				return original.Substring(index + length);
			}
			return original;
		}

		public static string RemoveBefore(this string original, string substring) {
			var index = original.IndexOf(substring);
			if (index >= 0) {
				return original.Substring(index);
			}
			return original;
		}

		public static string CommaSeparated(this IEnumerable<string> strings) {
			stringBuilder.Clear();

			foreach (var value in strings) {
				stringBuilder.Append($" {value},");
			}

			var result = stringBuilder.ToString();
			result = result.Trim(',');
			return result;
		}

		public static string Sanitized(this string original) {
			return original.Replace(' ', '_').RemoveSpecialCharacters();
		}

		public static string RemoveSpecialCharacters(this string str) {
			stringBuilder.Clear();
			foreach (char c in str) {
				if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '.' || c == '_') {
					stringBuilder.Append(c);
				}
			}
			return stringBuilder.ToString();
		}


		public static string ReplaceAll(this string original, char oldChar, char newChar) {
			if (oldChar == newChar) {
				return original;
			}
			var index = original.IndexOf(oldChar);
			while (index != -1) {
				original = original.Replace(oldChar, newChar);
				index = original.IndexOf(oldChar);
			}
			return original;
		}
	}
}