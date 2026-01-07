using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoidBreaker.Utilities
{
    /// <summary>
    /// Extension methods for common operations.
    /// </summary>
    public static class Extensions
    {
        // ==================== LIST EXTENSIONS ====================

        /// <summary>
        /// Returns a random element from the list.
        /// </summary>
        public static T GetRandom<T>(this IList<T> list)
        {
            if (list == null || list.Count == 0)
                return default;

            return list[UnityEngine.Random.Range(0, list.Count)];
        }

        /// <summary>
        /// Returns a random element from the list using a seeded random.
        /// </summary>
        public static T GetRandom<T>(this IList<T> list, System.Random random)
        {
            if (list == null || list.Count == 0)
                return default;

            return list[random.Next(0, list.Count)];
        }

        /// <summary>
        /// Shuffles the list in place using Fisher-Yates algorithm.
        /// </summary>
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = UnityEngine.Random.Range(0, n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        /// <summary>
        /// Returns true if the list is null or empty.
        /// </summary>
        public static bool IsNullOrEmpty<T>(this IList<T> list)
        {
            return list == null || list.Count == 0;
        }

        /// <summary>
        /// Removes a random element from the list and returns it.
        /// </summary>
        public static T RemoveRandom<T>(this IList<T> list)
        {
            if (list == null || list.Count == 0)
                return default;

            int index = UnityEngine.Random.Range(0, list.Count);
            T item = list[index];
            list.RemoveAt(index);
            return item;
        }

        // ==================== DICTIONARY EXTENSIONS ====================

        /// <summary>
        /// Gets a value or returns default if not found.
        /// </summary>
        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue defaultValue = default)
        {
            return dict.TryGetValue(key, out TValue value) ? value : defaultValue;
        }

        /// <summary>
        /// Adds or updates a value in the dictionary.
        /// </summary>
        public static void AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue value)
        {
            if (dict.ContainsKey(key))
                dict[key] = value;
            else
                dict.Add(key, value);
        }

        // ==================== VECTOR EXTENSIONS ====================

        /// <summary>
        /// Returns the vector with a modified x component.
        /// </summary>
        public static Vector3 WithX(this Vector3 v, float x) => new Vector3(x, v.y, v.z);

        /// <summary>
        /// Returns the vector with a modified y component.
        /// </summary>
        public static Vector3 WithY(this Vector3 v, float y) => new Vector3(v.x, y, v.z);

        /// <summary>
        /// Returns the vector with a modified z component.
        /// </summary>
        public static Vector3 WithZ(this Vector3 v, float z) => new Vector3(v.x, v.y, z);

        /// <summary>
        /// Returns the Vector2 version of a Vector3 (x, y).
        /// </summary>
        public static Vector2 ToVector2(this Vector3 v) => new Vector2(v.x, v.y);

        /// <summary>
        /// Returns a Vector3 from a Vector2 with z = 0.
        /// </summary>
        public static Vector3 ToVector3(this Vector2 v, float z = 0) => new Vector3(v.x, v.y, z);

        /// <summary>
        /// Returns the angle in degrees from origin to this vector.
        /// </summary>
        public static float ToAngle(this Vector2 v)
        {
            return Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// Returns a vector rotated by the given angle in degrees.
        /// </summary>
        public static Vector2 Rotate(this Vector2 v, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }

        // ==================== FLOAT EXTENSIONS ====================

        /// <summary>
        /// Remaps a value from one range to another.
        /// </summary>
        public static float Remap(this float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            return (value - fromMin) / (fromMax - fromMin) * (toMax - toMin) + toMin;
        }

        /// <summary>
        /// Returns true if the float is approximately equal to another.
        /// </summary>
        public static bool Approximately(this float a, float b, float tolerance = 0.0001f)
        {
            return Mathf.Abs(a - b) < tolerance;
        }

        // ==================== COLOR EXTENSIONS ====================

        /// <summary>
        /// Returns the color with a modified alpha.
        /// </summary>
        public static Color WithAlpha(this Color c, float alpha) => new Color(c.r, c.g, c.b, alpha);

        /// <summary>
        /// Converts a hex string to Color.
        /// </summary>
        public static Color HexToColor(this string hex)
        {
            if (hex.StartsWith("#"))
                hex = hex.Substring(1);

            if (hex.Length != 6 && hex.Length != 8)
                return Color.white;

            byte r = Convert.ToByte(hex.Substring(0, 2), 16);
            byte g = Convert.ToByte(hex.Substring(2, 2), 16);
            byte b = Convert.ToByte(hex.Substring(4, 2), 16);
            byte a = hex.Length == 8 ? Convert.ToByte(hex.Substring(6, 2), 16) : (byte)255;

            return new Color32(r, g, b, a);
        }

        // ==================== STRING EXTENSIONS ====================

        /// <summary>
        /// Truncates a string to the specified length with optional suffix.
        /// </summary>
        public static string Truncate(this string str, int maxLength, string suffix = "...")
        {
            if (string.IsNullOrEmpty(str) || str.Length <= maxLength)
                return str;

            return str.Substring(0, maxLength - suffix.Length) + suffix;
        }

        /// <summary>
        /// Converts a string to title case.
        /// </summary>
        public static string ToTitleCase(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            var words = str.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                {
                    words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
                }
            }
            return string.Join(" ", words);
        }

        // ==================== TRANSFORM EXTENSIONS ====================

        /// <summary>
        /// Destroys all children of this transform.
        /// </summary>
        public static void DestroyAllChildren(this Transform transform)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(transform.GetChild(i).gameObject);
            }
        }

        /// <summary>
        /// Resets the transform's local position, rotation, and scale.
        /// </summary>
        public static void ResetLocal(this Transform transform)
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        /// <summary>
        /// Gets the full path of this transform in the hierarchy.
        /// </summary>
        public static string GetPath(this Transform transform)
        {
            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }
            return path;
        }

        // ==================== GAMEOBJECT EXTENSIONS ====================

        /// <summary>
        /// Gets or adds a component to the GameObject.
        /// </summary>
        public static T GetOrAddComponent<T>(this GameObject go) where T : Component
        {
            T component = go.GetComponent<T>();
            if (component == null)
                component = go.AddComponent<T>();
            return component;
        }

        /// <summary>
        /// Sets the layer of this GameObject and all its children.
        /// </summary>
        public static void SetLayerRecursive(this GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
            {
                child.gameObject.SetLayerRecursive(layer);
            }
        }

        // ==================== COMPONENT EXTENSIONS ====================

        /// <summary>
        /// Tries to get a component, returns true if found.
        /// </summary>
        public static bool TryGetComponent<T>(this Component component, out T result) where T : Component
        {
            result = component.GetComponent<T>();
            return result != null;
        }
    }
}
