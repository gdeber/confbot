/*
 * Created by SharpDevelop.
 * User: Debe
 * Date: 15/02/2014
 * Time: 12:00
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ConfBot.PlugIns
{
	/// <summary>
	/// Description of BadWordsCleaner
	/// </summary>
	public class BadWordsCleaner
	{
		private List<string> badWords;
		private List<string> goodWords;
		private string badWordsRegexExp;
		private Random rndGen;
		private Dictionary<char, string> literalRegex = new Dictionary<char,string>()
		{{'a',@"[a4à]"},
			{'b',@"[b]"},
			{'c',@"[ck]"},
			{'d',@"[d]"},
			{'e',@"[eéè]"},
			{'f',@"[f]"},
			{'g',@"[g]"},
			{'h',@"[h]"},
			{'i', @"[i1ì]"},
			{'j',@"[j]"},
			{'k',@"[k]"},
			{'l', @"[l1]"},
			{'m', @"[m]"},
			{'n', @"[n]"},
			{'o', @"[o0ò]"},
			{'p', @"[p]"},
			{'q', @"[q]"},
			{'r', @"[r]"},
			{'s', @"[s]"},
			{'t', @"[t7]"},
			{'u', @"[uù]"},
			{'v', @"[v]"},
			{'w', @"[w]"},
			{'x', @"[x]"},
			{'y', @"[y]"},
			{'z', @"[z]"}};
		
		public BadWordsCleaner(List<string> badWords, List<string> goodWords)
		{
			this.badWords = badWords;
			this.goodWords = goodWords;
			this.badWordsRegexExp = createRegexSearchExp(this.badWords);
		}
		
		public string CleanText(string text)
		{
			int badWordCount;
			return this.CleanText(text, out badWordCount);
			
		}
		
		public string CleanText(string text , out int badWordCount)
		{
			this.rndGen = new Random();
			var badWordsRegex = new Regex(this.badWordsRegexExp, RegexOptions.Multiline | RegexOptions.IgnoreCase);
			var matches = badWordsRegex.Matches(text);
			badWordCount = matches.Count;
			var result = badWordsRegex.Replace(text, this.badwordMatchEvaluator);
			return result;
		}
		
		private string createRegexSearchExp(List<string> badWords)
		{
			List<string> wordToken = new List<string>();
			foreach (var word in badWords) {
				List<string> regexToken = new List<string>();
				foreach (char c in word) {
					regexToken.Add(literalRegex[c]);
				}
				wordToken.Add(string.Join(@"\p{P}*", regexToken));
			}
			return @"\b(" + string.Join("|", wordToken) + @")";
		}
		
		private string badwordMatchEvaluator(Match match)
		{
			int randomIdx = rndGen.Next(0, this.goodWords.Count);
			return "_"+this.goodWords[randomIdx]+"_";
		}
	}
}
