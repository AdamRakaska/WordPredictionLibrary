﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

namespace WordPredictionLibrary.Core
{
	public class Word : IEquatable<string>
	{
		public string Value { get; internal set; }
		public decimal NextWordDistinctCount { get { return _nextWordDictionary.DistinctWordCount; } }
		public decimal AbsoluteFrequency { get { return _nextWordDictionary.TotalWordCount; } }

		public static decimal noMatchValue = 0;

		internal NextWordFrequencyDictionary _nextWordDictionary;

		internal Dictionary<List<string>, int> _previousWordsDictionary;

		private WordDictionary _parentDictionary;


		#region Constructors

		public Word()
		{
			_previousWordsDictionary = new Dictionary<List<string>, int>(new WordListEqualityComparer());
			_nextWordDictionary = new NextWordFrequencyDictionary();
			_parentDictionary = new WordDictionary();
		}

		public Word(WordDictionary parentDictionary, string value)
			: this()
		{
			_parentDictionary = parentDictionary;
			Value = value.TryToLower();
		}

		#endregion

		#region Equals & IEquatable Overrides

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			else if (ReferenceEquals(this, obj))
			{
				return true;
			}
			else if (obj is string)
			{
				string str = (string)obj;
				return this.Value.Equals(str, StringComparison.InvariantCultureIgnoreCase);
			}
			else if (obj is Word)
			{
				Word wrd = (Word)obj;
				return this.Value.Equals(wrd.Value, StringComparison.InvariantCultureIgnoreCase);
			}
			else
			{
				return base.Equals(obj);
			}
		}

		public bool Equals(Word wrd)
		{
			return this.Value.Equals(wrd.Value, StringComparison.InvariantCultureIgnoreCase);
		}

		public bool Equals(string str)
		{
			return this.Value.Equals(str, StringComparison.InvariantCultureIgnoreCase);
		}

		public override int GetHashCode()
		{
			return Value.GetHashCode();
		}

		public IEnumerable<string> FormatAsString()
		{
			decimal wordOccurrence = AbsoluteFrequency;
			decimal totalWords = _parentDictionary.UniqueWordCount;

			decimal prevalence = Math.Round(wordOccurrence / totalWords, 5);

			yield return string.Format
				(
					"[\"{0}\" \t - \t {1}/{2}]",
					Value.ToUpperInvariant(),
					wordOccurrence,
					totalWords
				);

			yield break;
		}

		#endregion

		#region Dictionary Altering Methods

		public void AddPreviousWords(List<string> previousWords)
		{
			if (_previousWordsDictionary.ContainsKey(previousWords))
			{
				_previousWordsDictionary[previousWords] += 1;
			}
			else
			{
				_previousWordsDictionary.Add(previousWords, 1);
			}
		}

		public void AddNextWord(Word word)
		{
			_nextWordDictionary.Add(word);
		}

		public string SuggestNextWord()
		{
			return _nextWordDictionary.GetNextWord();
		}

		public string SuggestNextWord(string previousWord)
		{
			return _nextWordDictionary.GetNextWord(previousWord);
		}

		public IEnumerable<string> SuggestNextWords(int count)
		{
			return _nextWordDictionary.TakeTop(count);
		}

		public IEnumerable<string> SuggestNextWords(string previousWord, int count)
		{
			return _nextWordDictionary.TakeTop(previousWord, count);
		}

		#endregion

		#region Get Dictionary

		Dictionary<Word, decimal> _freqDict = null;
		public Dictionary<Word, decimal> GetFrequencyDictionary()
		{
			if (_freqDict == null && _nextWordDictionary.DistinctWordCount > 0)
			{
				_freqDict = _nextWordDictionary.GetFrequencyDictionary();
				return _freqDict;
			}
			return new Dictionary<Word, decimal>();
		}

		public NextWordFrequencyDictionary GetNextWordDictionary()
		{
			return _nextWordDictionary;
		}

		#endregion

		#region Suggest

		public decimal GetNextWordProbability(Word nextWord)
		{
			if (!_nextWordDictionary.Contains(nextWord)) { return noMatchValue; }

			decimal nextWordOccurrences = _nextWordDictionary[nextWord];
			decimal absoluteFrequency = AbsoluteFrequency;

			return nextWordOccurrences / absoluteFrequency;
		}

		public decimal GetNextWordFrequency(Word nextWord)
		{
			if (!_nextWordDictionary.Contains(nextWord)) { return noMatchValue; }

			return this.GetFrequencyDictionary()[nextWord];
		}

		#endregion

		#region Statistics

		public decimal GetVariance()
		{
			if (_nextWordDictionary == null || !_nextWordDictionary._internalDictionary.Any()) { return noMatchValue; }

			decimal sum = _nextWordDictionary._internalDictionary.Sum(kvp => (long)kvp.Value);
			decimal mean = sum / AbsoluteFrequency;

			decimal squaredDeviations = _nextWordDictionary._internalDictionary.Sum(kvp => (decimal)Math.Pow((double)(kvp.Value - mean), 2));
			decimal variance = squaredDeviations / mean;
			return variance;
		}

		public decimal GetStandardDeviation()
		{
			if (_nextWordDictionary == null) { return noMatchValue; }

			return (decimal)Math.Sqrt((double)GetVariance());
		}

		#endregion

		#region Order

		public void OrderInternalDictionary(SortCriteria sortCriteria, SortDirection sortDirection)
		{
			if (_nextWordDictionary != null && _nextWordDictionary._internalDictionary.Any())
			{
				_nextWordDictionary.OrderInternalDictionary(sortCriteria, sortDirection);
			}

			if (_previousWordsDictionary.Any())
			{
				Dictionary<List<string>, int> previousDict = _previousWordsDictionary.OrderDictionaryBy(kvp => kvp.Value, sortDirection).ToDictionary(kvp => kvp.Key, kvp => kvp.Value, new WordListEqualityComparer());
				_previousWordsDictionary = previousDict;
			}
		}

		#endregion

	}
}
