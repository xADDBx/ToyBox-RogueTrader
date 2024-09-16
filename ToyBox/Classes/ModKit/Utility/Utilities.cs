// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ModKit {
    public static class Utilities {
        public static TValue? GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default) {
            if (dictionary == null) { throw new ArgumentNullException(nameof(dictionary)); } // using C# 6
            if (key == null) { throw new ArgumentNullException(nameof(key)); } //  using C# 6

            return dictionary.TryGetValue(key, out var value) ? value : defaultValue;
        }
        public static Dictionary<TKey, TElement> ToDictionaryIgnoringDuplicates<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey>? comparer = null) {
            if (source == null)
                throw new ArgumentException("source");
            if (keySelector == null)
                throw new ArgumentException("keySelector");
            if (elementSelector == null)
                throw new ArgumentException("elementSelector");
            Dictionary<TKey, TElement> d = new Dictionary<TKey, TElement>(comparer);
            foreach (TSource element in source) {
                if (!d.ContainsKey(keySelector(element)))
                    d.Add(keySelector(element), elementSelector(element));
            }
            return d;
        }
        public static string StripHTML(this string s) => Regex.Replace(s, "<.*?>", string.Empty);
        public static string MergeSpaces(this string str, bool trim = false) {
            if (str == null)
                return null;
            else {
                StringBuilder stringBuilder = new StringBuilder(str.Length);

                int i = 0;
                foreach (char c in str) {
                    if (c != ' ' || i == 0 || str[i - 1] != ' ')
                        stringBuilder.Append(c);
                    i++;
                }
                if (trim)
                    return stringBuilder.ToString().Trim();
                else
                    return stringBuilder.ToString();
            }
        }
        public static string SubstringBetweenCharacters(this string input, char charFrom, char charTo) {
            var posFrom = input.IndexOf(charFrom);
            if (posFrom != -1) //if found char
            {
                var posTo = input.IndexOf(charTo, posFrom + 1);
                if (posTo != -1) //if found char
                {
                    return input.Substring(posFrom + 1, posTo - posFrom - 1);
                }
            }
            return string.Empty;
        }
        public static string[] TrimCommonPrefix(this string[] values) {
            var prefix = string.Empty;
            int? resultLength = null;

            if (values != null) {
                if (values.Length > 1) {
                    var min = values.Min(value => value.Length);
                    for (var charIndex = 0; charIndex < min; charIndex++) {
                        for (var valueIndex = 1; valueIndex < values.Length; valueIndex++) {
                            if (values[0][charIndex] != values[valueIndex][charIndex]) {
                                resultLength = charIndex;
                                break;
                            }
                        }
                        if (resultLength.HasValue) {
                            break;
                        }
                    }
                    if (resultLength.HasValue &&
                        resultLength.Value > 0) {
                        prefix = values[0].Substring(0, resultLength.Value);
                    }
                } else if (values.Length > 0) {
                    prefix = values[0];
                }
            }
            return prefix.Length > 0 ? values.Select(s => s.Replace(prefix, "")).ToArray() : values;
        }
        // Credits to https://github.com/microsoftenator2022
        /// <summary>
        /// Divides input sequence into chunks of at most <paramref name="chunkSize"/>
        /// </summary>
        /// <returns>Sequence of sequences of <paramref name="chunkSize"/> elements. The last chunk will contain at most <paramref name="chunkSize"/> elements</returns>
        public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> source, int chunkSize) {
            var chunk = new T[chunkSize];
            var i = 0;

            foreach (var element in source) {
                chunk[i] = element;

                i++;
                if (i == chunkSize) {
                    yield return chunk;
                    chunk = new T[chunkSize];
                    i = 0;
                }
            }

            if (i > 0 && i < chunkSize) yield return chunk.Take(i);
        }
    }
    public static class CloneUtil<T> {
        private static readonly Func<T, object> clone;

        static CloneUtil() {
            var cloneMethod = typeof(T).GetMethod("MemberwiseClone", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            clone = (Func<T, object>)cloneMethod.CreateDelegate(typeof(Func<T, object>));
        }

        public static T ShallowClone(T obj) => (T)clone(obj);
    }
    public static class CloneUtil {
        public static T ShallowClone<T>(this T obj) => CloneUtil<T>.ShallowClone(obj);
    }
}
