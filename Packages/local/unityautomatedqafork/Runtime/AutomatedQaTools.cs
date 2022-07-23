using System;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine;
using System.Text;
using System.IO;

namespace Unity.AutomatedQA
{
    public static class AutomatedQaTools
    {
        public static bool SequenceEqual<T>(this List<T> list, List<T> otherList)
        {
            if (list.Count != otherList.Count) return false;
            for (int i = 0; i < list.Count; i++)
            {
                if (!list[i].Equals(otherList[i])) return false;
            }
            return true;
        }

        public static List<X> Select<X, T>(this T[] list, Func<T, X> predicate)
        {
            List<X> values = new List<X>();
            foreach (T item in list)
            {
                values.Add(predicate.Invoke(item));
            }
            return values;
        }

        public static List<T> Prepend<T>(this List<T> list, T item)
        {
            List<T> result = new List<T>() { item };
            result.AddRange(list);
            return result;
        }

        public static List<T> PrependRange<T>(this List<T> list, List<T> items)
        {
            List<T> result = items;
            result.AddRange(list);
            return result;
        }

        public static List<T> AddAtAndReturnNewList<T>(this List<T> list, int index, T item)
        {
            List<T> results = new List<T>();
            if (index < 0)
            {
                results.Add(item);
            }
            for (int i = 0; i < list.Count; i++)
            {
                if (i == index)
                    results.Add(item);
                results.Add(list[i]);
            }
            if (index >= list.Count)
            {
                results.Add(item);
            }

            return results;
        }

        public static bool AnyMatch<T>(this T[] array, Func<T, bool> predicate)
        {
            foreach (T item in array)
            {
                if (predicate.Invoke(item))
                    return true;
            }
            return false;
        }

        public static bool AnyMatch<T>(this List<T> list, Func<T, bool> predicate)
        {
            foreach (T item in list)
            {
                if (predicate.Invoke(item))
                    return true;
            }
            return false;
        }

        public static bool Any<T>(this List<T> list)
        {
            return list.Count > 0;
        }

        public static bool Any<T>(this T[] array)
        {
            return array.Length > 0;
        }

        public static bool Contains<T>(this T[] array, T item)
        {
            return array.ToList().Contains(item);
        }

        public static List<X> Select<X, T>(this List<T> list, Func<T, X> predicate)
        {
            return Select(list.ToArray(), predicate);
        }

        public static List<T> ToList<T>(this T[] array)
        {
            return new List<T>(array);
        }

        public static List<T> ToList<T>(this IEnumerable<T> enumerable)
        {
            return new List<T>(enumerable);
        }

        public static T First<T>(this List<T> list)
        {
            if (!list.Any())
            {
                throw new UnityException("List provided to AutomatedQATools.First() was empty. Cannot invoke First() on an empty list. Check for list being empty before invoking First().");
            }
            return list[0];
        }

        public static T First<T>(this T[] array)
        {
            if (!array.Any())
            {
                throw new UnityException("Array provided to AutomatedQATools.First() was empty. Cannot invoke First() on an empty array. Check for array being empty before invoking First().");
            }
            return array[0];
        }

        public static T Last<T>(this List<T> list)
        {
            if (!list.Any())
            {
                throw new UnityException("List provided to AutomatedQATools.Last() was empty. Cannot invoke Last() on an empty list. Check for list being empty before invoking Last().");
            }
            else
            {
                return list[list.Count - 1];
            }
        }

        public static T Last<T>(this T[] array)
        {
            if (!array.Any())
            {
                throw new UnityException("Array provided to AutomatedQATools.Last() was empty. Cannot invoke Last() on an empty array. Check for array being empty before invoking Last().");
            }
            else
            {
                return array[array.Length - 1];
            }
        }

        public static List<T> GetUniqueObjectsBetween<T>(this List<T> thisList, List<T> otherList)
        {
            List<T> unique = new List<T>();
            for (int i = 0; i < thisList.Count; i++)
            {
                if (!otherList.Contains(thisList[i]))
                {
                    unique.Add(thisList[i]);
                }
            }
            for (int x = 0; x < otherList.Count; x++)
            {
                if (!thisList.Contains(otherList[x]))
                {
                    unique.Add(otherList[x]);
                }
            }
            return unique;
        }

        public static T Random<T>(this List<T> list)
        {
            System.Random r = new System.Random((int)Time.time);
            if (!list.Any())
            {
                throw new UnityException("List provided to AutomatedQATools.Last() was empty. Cannot invoke Last() on an empty list. Check for list being empty before invoking Last().");
            }
            return list[r.Next(0, list.Count - 1)];
        }

        public static List<string> GetHierarchy(GameObject go)
        {
            var hierarchy = new List<string>();
            var parent = go.transform.parent;
            while (parent != null)
            {
                hierarchy.Add(parent.name);
                parent = parent.parent;
            }

            hierarchy.Reverse();
            return hierarchy;
        }

        public static bool IsError(this UnityWebRequest uwr)
        {
#if UNITY_2020_1_OR_NEWER
            return uwr.result != UnityWebRequest.Result.Success;
#else
            return uwr.isNetworkError || uwr.isHttpError;
#endif
        }

        public static string SanitizeStringForUseInGeneratingCode(this string val)
        {
            StringBuilder result = new StringBuilder();
            char[] chars = val.ToCharArray();
            foreach (char character in chars)
            {
                if (!alphaNumerics.Contains(character) && character != '_')
                {
                    if (result.ToString().EndsWith("_"))
                    {
                        continue;
                    }
                    else
                    {
                        result.Append("_");
                    }
                }
                else
                {
                    if (character == '_' && result.ToString().EndsWith("_"))
                    {
                        continue;
                    }
                    else
                    {
                        result.Append(character);
                    }
                }
            }
            return result.ToString();
        }

        public static string SanitizeStringForUseInFilePath(this string val)
        {
            return string.Join("_", val.Split(Path.GetInvalidFileNameChars())).Replace(" ", "_");
        }

        static char[] alphaNumerics = { 'a', 'A', 'b', 'B', 'c', 'C', 'd', 'D', 'e', 'E', 'f', 'F', 'g', 'G', 'h', 'H', 'i', 'I', 'j', 'J', 'k', 'K', 'l', 'L', 'm', 'M', 'n', 'N', 'o', 'O', 'p', 'P', 'q', 'Q', 'r', 'R', 's', 'S', 't', 'T', 'u', 'U', 'v', 'V', 'w', 'W', 'x', 'X', 'y', 'Y', 'z', 'Z', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
        static char[] nonAlphaNumerics = { '@', '#', '^', '&', '(', ')', '+' };
        public static string RandomString(int length, bool alphaNumericOnly = false, bool isPassword = false)
        {
            System.Random random = new System.Random();
            List<char> useChars = new List<char>();
            if (!alphaNumericOnly)
            {
                useChars.AddRange(nonAlphaNumerics);
            }
            useChars.AddRange(alphaNumerics);
            StringBuilder randomString = new StringBuilder();
            if (isPassword)
            {
                if (length < 8)
                {
                    return string.Empty;
                }
                //If this is a password, these must be satisfied.
                bool hasCapitalLetter = false;
                bool hasLowercaseLetter = false;
                bool hasNumber = false;
                bool hasSpecialChar = false;

                for (int c = 0; c < length; c++)
                {
                    List<char> LimitedSet = new List<char>();
                    if (!hasCapitalLetter)
                    {
                        LimitedSet = useChars.FindAll(x => char.IsUpper(x));
                        randomString.Append(LimitedSet[random.Next(0, LimitedSet.Count - 1)]);
                        hasCapitalLetter = true;
                    }
                    else if (!hasLowercaseLetter)
                    {
                        LimitedSet = useChars.FindAll(x => !char.IsUpper(x));
                        randomString.Append(LimitedSet[random.Next(0, LimitedSet.Count - 1)]);
                        hasLowercaseLetter = true;
                    }
                    else if (!hasNumber)
                    {
                        LimitedSet = useChars.FindAll(x => char.IsDigit(x));
                        randomString.Append(LimitedSet[random.Next(0, LimitedSet.Count - 1)]);
                        hasNumber = true;
                    }
                    else if (!hasSpecialChar && !alphaNumericOnly)
                    {
                        randomString.Append(nonAlphaNumerics[random.Next(0, nonAlphaNumerics.Length - 1)]);
                        hasSpecialChar = true;
                    }
                    else
                    {
                        randomString.Append(useChars[random.Next(0, useChars.Count - 1)]);
                    }
                }
            }
            else
            {
                for (int c = 0; c < length; c++)
                {
                    randomString.Append(useChars[random.Next(0, useChars.Count - 1)]);
                }
            }
            return randomString.ToString();
        }
        
        public static void HandleError(string msg)
        {
            AQALogger logger = new AQALogger();
            if (Application.isBatchMode)
            {
                throw new Exception(msg);
            }
            logger.LogError(msg);
        }
    }
}