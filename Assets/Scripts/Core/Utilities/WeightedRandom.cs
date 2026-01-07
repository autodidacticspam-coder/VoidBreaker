using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoidBreaker.Utilities
{
    /// <summary>
    /// Weighted random selection with support for various weight-based algorithms.
    /// </summary>
    public class WeightedRandom<T>
    {
        private readonly List<WeightedItem> items = new List<WeightedItem>();
        private float totalWeight = 0f;
        private bool isDirty = true;

        private struct WeightedItem
        {
            public T Value;
            public float Weight;
            public float CumulativeWeight;
        }

        public int Count => items.Count;
        public float TotalWeight => totalWeight;

        public WeightedRandom() { }

        public WeightedRandom(IEnumerable<(T item, float weight)> initial)
        {
            foreach (var (item, weight) in initial)
            {
                Add(item, weight);
            }
        }

        /// <summary>
        /// Adds an item with the specified weight.
        /// </summary>
        public void Add(T item, float weight)
        {
            if (weight <= 0)
            {
                Debug.LogWarning("Attempting to add item with non-positive weight");
                return;
            }

            items.Add(new WeightedItem
            {
                Value = item,
                Weight = weight,
                CumulativeWeight = 0
            });

            isDirty = true;
        }

        /// <summary>
        /// Removes an item from the weighted collection.
        /// </summary>
        public bool Remove(T item)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (EqualityComparer<T>.Default.Equals(items[i].Value, item))
                {
                    items.RemoveAt(i);
                    isDirty = true;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Clears all items.
        /// </summary>
        public void Clear()
        {
            items.Clear();
            totalWeight = 0;
            isDirty = true;
        }

        /// <summary>
        /// Selects a random item based on weights.
        /// </summary>
        public T Select()
        {
            if (items.Count == 0)
                return default;

            if (isDirty)
                Rebuild();

            float roll = UnityEngine.Random.Range(0f, totalWeight);
            return SelectByCumulativeWeight(roll);
        }

        /// <summary>
        /// Selects a random item using a seeded random.
        /// </summary>
        public T Select(System.Random random)
        {
            if (items.Count == 0)
                return default;

            if (isDirty)
                Rebuild();

            float roll = (float)(random.NextDouble() * totalWeight);
            return SelectByCumulativeWeight(roll);
        }

        /// <summary>
        /// Selects multiple unique items without replacement.
        /// </summary>
        public List<T> SelectMultiple(int count)
        {
            var result = new List<T>();

            if (count >= items.Count)
            {
                foreach (var item in items)
                    result.Add(item.Value);
                return result;
            }

            // Create a temporary copy
            var tempItems = new List<WeightedItem>(items);
            float tempTotal = totalWeight;

            for (int i = 0; i < count && tempItems.Count > 0; i++)
            {
                float roll = UnityEngine.Random.Range(0f, tempTotal);
                float cumulative = 0f;

                for (int j = 0; j < tempItems.Count; j++)
                {
                    cumulative += tempItems[j].Weight;
                    if (roll <= cumulative)
                    {
                        result.Add(tempItems[j].Value);
                        tempTotal -= tempItems[j].Weight;
                        tempItems.RemoveAt(j);
                        break;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the weight of a specific item.
        /// </summary>
        public float GetWeight(T item)
        {
            foreach (var i in items)
            {
                if (EqualityComparer<T>.Default.Equals(i.Value, item))
                    return i.Weight;
            }
            return 0f;
        }

        /// <summary>
        /// Updates the weight of an existing item.
        /// </summary>
        public bool SetWeight(T item, float newWeight)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (EqualityComparer<T>.Default.Equals(items[i].Value, item))
                {
                    var existing = items[i];
                    existing.Weight = newWeight;
                    items[i] = existing;
                    isDirty = true;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Gets the probability of selecting a specific item.
        /// </summary>
        public float GetProbability(T item)
        {
            if (isDirty)
                Rebuild();

            if (totalWeight <= 0)
                return 0f;

            return GetWeight(item) / totalWeight;
        }

        private void Rebuild()
        {
            totalWeight = 0f;

            for (int i = 0; i < items.Count; i++)
            {
                totalWeight += items[i].Weight;
                var item = items[i];
                item.CumulativeWeight = totalWeight;
                items[i] = item;
            }

            isDirty = false;
        }

        private T SelectByCumulativeWeight(float roll)
        {
            // Binary search for efficiency
            int low = 0;
            int high = items.Count - 1;

            while (low < high)
            {
                int mid = (low + high) / 2;
                if (items[mid].CumulativeWeight < roll)
                    low = mid + 1;
                else
                    high = mid;
            }

            return items[low].Value;
        }
    }

    /// <summary>
    /// Static utility methods for weighted random selection.
    /// </summary>
    public static class WeightedRandomUtils
    {
        /// <summary>
        /// Selects an item from a list using a weight selector function.
        /// </summary>
        public static T Select<T>(IList<T> items, Func<T, float> weightSelector)
        {
            if (items == null || items.Count == 0)
                return default;

            float totalWeight = 0f;
            foreach (var item in items)
            {
                totalWeight += weightSelector(item);
            }

            float roll = UnityEngine.Random.Range(0f, totalWeight);
            float cumulative = 0f;

            foreach (var item in items)
            {
                cumulative += weightSelector(item);
                if (roll <= cumulative)
                    return item;
            }

            return items[items.Count - 1];
        }

        /// <summary>
        /// Selects from a dictionary where values are weights.
        /// </summary>
        public static TKey SelectFromWeights<TKey>(IDictionary<TKey, float> weights)
        {
            if (weights == null || weights.Count == 0)
                return default;

            float totalWeight = 0f;
            foreach (var weight in weights.Values)
            {
                totalWeight += weight;
            }

            float roll = UnityEngine.Random.Range(0f, totalWeight);
            float cumulative = 0f;

            foreach (var kvp in weights)
            {
                cumulative += kvp.Value;
                if (roll <= cumulative)
                    return kvp.Key;
            }

            // Fallback to first item
            foreach (var key in weights.Keys)
                return key;

            return default;
        }
    }
}
