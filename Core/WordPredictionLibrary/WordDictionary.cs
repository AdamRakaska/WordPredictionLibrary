﻿using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace WordPredictionLibrary.Core
{
	public class WordDictionary
	{
		public decimal UniqueWordCount { get { return _internalDictionary == null ? 0 : _internalDictionary.Count; } }
		public decimal TotalSampleSize { get { return _internalDictionary.Select(kvp => kvp.Value.AbsoluteFrequency).Sum(); } }

		#region Internal Properties & Method

		internal List<Word> Words { get { return _internalDictionary.Values.ToList(); } }
		internal Dictionary<string, Word> _internalDictionary = null;
		internal bool isOrdered = false;
		public static string StartPlaceholder = "{{start}}";
		public static string EndPlaceholder = "{{end}}";
		public static decimal noMatchValue = 0;

		internal void OrderInternalDictionary()
		{
			if (_internalDictionary != null)
			{
				if (!isOrdered)
				{
					isOrdered = true;

					Dictionary<string, Word> newDict = _internalDictionary.OrderByDescending(kvp => kvp.Value.AbsoluteFrequency).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
					_internalDictionary = newDict;

					// Sort every Word's internal dictionary
					foreach (Word word in _internalDictionary.Values)
					{
						word.OrderInternalDictionary();
					}
				}
			}
		}

		#endregion

		#region Constructors & ToString

		public WordDictionary()
		{
			_internalDictionary = new Dictionary<string, Word>();
			isOrdered = false;
		}

		public WordDictionary(Dictionary<string, Word> dictionary)
			: this()
		{
			_internalDictionary = dictionary;
		}

		public override string ToString()
		{
			if (!isOrdered)
			{
				OrderInternalDictionary();
			}

			StringBuilder result = new StringBuilder();
			foreach (Word word in Words)
			{
				result.AppendLine(word.ToString());
			}
			return result.ToString();
		}

		#endregion

		#region Train

		public void Train(List<List<string>> paragraph)
		{
			foreach (List<string> sentence in paragraph)
			{
				this.Train(sentence);
			}
		}

		public void Train(List<string> sentence)
		{
			if (sentence == null || sentence.Count < 1) { return; }
			sentence = sentence.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
			sentence = sentence.Select(s => s.TryToLower()).ToList();

			List<string> previousWords = new List<string>();
			string lastWord = StartPlaceholder;
			foreach (string word in sentence)
			{
				previousWords.Add(lastWord);
				if (string.IsNullOrWhiteSpace(word))
				{
					continue;
				}
				if (!string.IsNullOrEmpty(lastWord))
				{
					Add(previousWords, lastWord, word);
				}
				lastWord = word;
			}
			Add(previousWords, lastWord, EndPlaceholder);
		}

		internal void Add(List<string> previous, string current, string next)
		{
			if (string.IsNullOrWhiteSpace(current))
			{
				return;
			}
			List<string> previousLower = previous.Select(s => s.TryToLower()).ToList();
			string currentLower = current.TryToLower();
			string nextLower = next.TryToLower();

			CreateIfRequired(currentLower);
			CreateIfRequired(nextLower);
			Word currentWord = _internalDictionary[currentLower];
			currentWord.AddPreviousWords(previousLower);
			currentWord.AddNextWord(_internalDictionary[nextLower]);
		}

		private void CreateIfRequired(string word)
		{
			string lowerWord = word.TryToLower();
			if (lowerWord == "")
			{
				lowerWord = EndPlaceholder;
			}
			if (!_internalDictionary.ContainsKey(lowerWord))
			{
				_internalDictionary.Add(lowerWord, new Word(lowerWord));
			}
		}

		#endregion

		#region Suggest

		public string SuggestNextWord(string fromWord)
		{
			fromWord = fromWord.TryToLower();
			if (_internalDictionary.ContainsKey(fromWord))
			{
				return _internalDictionary[fromWord].SuggestNextWord();
			}
			return string.Empty;
		}

		public string SuggestNextWord(string fromWord, string previousWord)
		{
			fromWord = fromWord.TryToLower();
			if (_internalDictionary.ContainsKey(fromWord))
			{
				return _internalDictionary[fromWord].SuggestNextWord(previousWord);
			}
			return string.Empty;
		}

		public IEnumerable<string> Suggest(string fromWord, int quantity)
		{
			fromWord = fromWord.TryToLower();
			if (_internalDictionary.ContainsKey(fromWord))
			{
				return _internalDictionary[fromWord].SuggestNextWords(quantity);
			}
			return new List<string>() { };
		}

		public IEnumerable<string> Suggest(string fromWord, string previousWord, int quantity)
		{
			fromWord = fromWord.TryToLower();
			if (_internalDictionary.ContainsKey(fromWord))
			{
				return _internalDictionary[fromWord].SuggestNextWords(previousWord, quantity);
			}
			return new List<string>() { };
		}

		#endregion

		#region NextWord

		public decimal GetNextWordProbability(string current, string next)
		{
			if (!_internalDictionary.ContainsKey(current) || !_internalDictionary.ContainsKey(next)) { return noMatchValue; }
			return _internalDictionary[current].GetNextWordProbability(_internalDictionary[next]);
		}

		public decimal GetNextWordPopularity(string current, string next)
		{
			if (!_internalDictionary.ContainsKey(current) || !_internalDictionary.ContainsKey(next)) { return noMatchValue; }

			decimal baseProbability = (1 / TotalSampleSize);

			return (_internalDictionary[next].AbsoluteFrequency * baseProbability);
		}

		public decimal GetNextWordFrequency(string current, string next)
		{
			if (!_internalDictionary.ContainsKey(current) || !_internalDictionary.ContainsKey(next)) { return noMatchValue; }
			return _internalDictionary[current].GetNextWordFrequency(_internalDictionary[next]);
		}

		#endregion

		#region PreviousWord
		#endregion

		#region Sort

		public IEnumerable<Word> GetDistinctSortedWordsList()
		{
			return _internalDictionary
						.OrderByDescending(kvp => kvp.Value.AbsoluteFrequency)
						.Select(kvp => kvp.Value)
						.Distinct();
		}

		public string GetDistinctSortedWordFrequencyString()
		{
			Dictionary<Word, decimal> freqDict = GetFrequencyDictionary();

			// Should sum to 1, since every probability is a value between 0 and 1 that is the proportional fraction
			decimal sumOfProbabilities = freqDict.Select(kvp => kvp.Value).Sum();
			sumOfProbabilities = decimal.Round(sumOfProbabilities);
			if (sumOfProbabilities != 1) { throw new ArithmeticException("The sum off all the probabilities should be 1."); }

			StringBuilder result = new StringBuilder();

			result.AppendLine("WORD,FREQUENCY");
			result.Append(
				string.Join(
					Environment.NewLine,
					freqDict.Select(kvp => string.Format("{0:#.000000000},{1,8}", (kvp.Value * 100), kvp.Key.Value))
				)
			);
			result.AppendLine();

			return result.ToString();
		}

		#endregion

		#region Statistics

		public decimal GetVariance(string word)
		{
			if (_internalDictionary == null) { return noMatchValue; }
			return _internalDictionary[word].GetVariance();
		}

		public decimal GetVariance()
		{
			if (_internalDictionary == null) { return noMatchValue; }

			decimal mean = this.TotalSampleSize / this.UniqueWordCount;
			decimal squaredDeviations = _internalDictionary.Sum(kvp => (decimal)Math.Pow((double)(kvp.Value.AbsoluteFrequency - mean), 2));
			return squaredDeviations / mean;
		}

		public decimal GetStandardDeviation(string word)
		{
			if (!_internalDictionary.ContainsKey(word)) { return noMatchValue; }
			return _internalDictionary[word].GetStandardDeviation();
		}

		public decimal GetStandardDeviation()
		{
			if (_internalDictionary == null) { return noMatchValue; }
			return (decimal)Math.Sqrt((double)this.GetVariance());
		}

		#endregion

		#region Find

		public bool Contains(string word)
		{
			return Words.Any(wrd => string.Compare(wrd.Value, word, true) == 0);
		}

		public Word Find(string word)
		{
			return Words.Where(wrd => string.Compare(wrd.Value, word, true) == 0).Single();
		}

		#endregion

		#region Get Dictionary

		public Dictionary<Word, decimal> GetFrequencyDictionary()
		{
			decimal baseProbability = 1 / TotalSampleSize;
			return GetDistinctSortedWordsList().ToDictionary(k => k, v => baseProbability * v.AbsoluteFrequency);
		}

		public Dictionary<string, Word> GetInternalDictionary()
		{
			return _internalDictionary;
		}

		#endregion

	}
}
