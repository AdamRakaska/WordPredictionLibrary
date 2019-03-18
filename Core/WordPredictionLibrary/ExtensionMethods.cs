﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordPredictionLibrary.Core
{
	public static class FileInfoExtensionMethods
	{
		public static FileInfo RenameIfExists(this FileInfo source)
		{
			if (source != null)
			{
				int counter = 1;
				string newFilename = source.FullName;

				while (File.Exists(newFilename))
				{
					newFilename = Path.Combine(source.DirectoryName, string.Format("{0} ({1}){2}", Path.GetFileNameWithoutExtension(source.Name), counter++, Path.GetExtension(source.Name)));
				}
				return new FileInfo(newFilename);
			}
			return default(FileInfo);
		}
	}

	public static class StringExtensionMethods
	{
		public static string TryToLower(this string source)
		{
			if (string.IsNullOrWhiteSpace(source))
			{
				return "";
			}
			else
			{
				return source.ToLowerInvariant();
			}
		}
	}

	public static class StringBuilderExtensionMethods
	{
		public static bool Contains(this StringBuilder source, string value)
		{
			if (source == null || string.IsNullOrWhiteSpace(value))
			{
				return false;
			}

			string sourceString = source.ToString();
			if (string.IsNullOrWhiteSpace(sourceString))
			{
				return false;
			}

			return sourceString.Contains(value);
		}
	}

	public static class NextWordFrequencyDictionaryExtensionMethods
	{
		public static IOrderedEnumerable<KeyValuePair<Word, decimal>> OrderByFrequencyDescending(this Dictionary<Word, decimal> source)
		{
			IOrderedEnumerable<KeyValuePair<Word, decimal>> result = (IOrderedEnumerable<KeyValuePair<Word, decimal>>)new List<KeyValuePair<Word, decimal>>();
						
			// If we haven't set FrequencyDictionary yet OR it is out of date (dict has more entries)
			if (source.Any())
			{
				result = source.OrderByDescending(kvp => kvp.Value);
			}

			return result;
		}
	}
}
