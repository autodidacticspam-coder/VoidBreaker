using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoidBreaker.Utilities
{
    /// <summary>
    /// Generic object pooling system for efficient object reuse.
    /// </summary>
    public class ObjectPool<T> where T : class
    {
        private readonly Stack<T> pool;
        private readonly Func<T> createFunc;
        private readonly Action<T> onGet;
        private readonly Action<T> onRelease;
        private readonly int maxSize;

        public int CountInactive => pool.Count;
        public int CountAll { get; private set; }

        public ObjectPool(Func<T> createFunc, Action<T> onGet = null, Action<T> onRelease = null, int initialSize = 10, int maxSize = 1000)
        {
            this.createFunc = createFunc ?? throw new ArgumentNullException(nameof(createFunc));
            this.onGet = onGet;
            this.onRelease = onRelease;
            this.maxSize = maxSize;

            pool = new Stack<T>(initialSize);

            // Pre-populate
            for (int i = 0; i < initialSize; i++)
            {
                T obj = createFunc();
                pool.Push(obj);
                CountAll++;
            }
        }

        public T Get()
        {
            T item;

            if (pool.Count > 0)
            {
                item = pool.Pop();
            }
            else
            {
                item = createFunc();
                CountAll++;
            }

            onGet?.Invoke(item);
            return item;
        }

        public void Release(T item)
        {
            if (item == null)
                return;

            onRelease?.Invoke(item);

            if (pool.Count < maxSize)
            {
                pool.Push(item);
            }
        }

        public void Clear()
        {
            pool.Clear();
            CountAll = 0;
        }
    }

    /// <summary>
    /// Object pool specifically for GameObjects.
    /// </summary>
    public class GameObjectPool
    {
        private readonly GameObject prefab;
        private readonly Transform parent;
        private readonly Stack<GameObject> pool;
        private readonly int maxSize;

        public int CountInactive => pool.Count;

        public GameObjectPool(GameObject prefab, Transform parent = null, int initialSize = 10, int maxSize = 100)
        {
            this.prefab = prefab ?? throw new ArgumentNullException(nameof(prefab));
            this.parent = parent;
            this.maxSize = maxSize;

            pool = new Stack<GameObject>(initialSize);

            // Pre-populate
            for (int i = 0; i < initialSize; i++)
            {
                GameObject obj = CreateNew();
                obj.SetActive(false);
                pool.Push(obj);
            }
        }

        private GameObject CreateNew()
        {
            GameObject obj = UnityEngine.Object.Instantiate(prefab, parent);
            return obj;
        }

        public GameObject Get()
        {
            GameObject obj;

            if (pool.Count > 0)
            {
                obj = pool.Pop();
            }
            else
            {
                obj = CreateNew();
            }

            obj.SetActive(true);
            return obj;
        }

        public GameObject Get(Vector3 position, Quaternion rotation)
        {
            GameObject obj = Get();
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            return obj;
        }

        public void Release(GameObject obj)
        {
            if (obj == null)
                return;

            obj.SetActive(false);

            if (pool.Count < maxSize)
            {
                pool.Push(obj);
            }
            else
            {
                UnityEngine.Object.Destroy(obj);
            }
        }

        public void Clear()
        {
            while (pool.Count > 0)
            {
                var obj = pool.Pop();
                if (obj != null)
                {
                    UnityEngine.Object.Destroy(obj);
                }
            }
        }
    }

    /// <summary>
    /// Component-based poolable interface.
    /// </summary>
    public interface IPoolable
    {
        void OnSpawnFromPool();
        void OnReturnToPool();
    }

    /// <summary>
    /// Generic component pool.
    /// </summary>
    public class ComponentPool<T> where T : Component
    {
        private readonly T prefab;
        private readonly Transform parent;
        private readonly Stack<T> pool;
        private readonly int maxSize;

        public int CountInactive => pool.Count;

        public ComponentPool(T prefab, Transform parent = null, int initialSize = 10, int maxSize = 100)
        {
            this.prefab = prefab ?? throw new ArgumentNullException(nameof(prefab));
            this.parent = parent;
            this.maxSize = maxSize;

            pool = new Stack<T>(initialSize);

            for (int i = 0; i < initialSize; i++)
            {
                T obj = CreateNew();
                obj.gameObject.SetActive(false);
                pool.Push(obj);
            }
        }

        private T CreateNew()
        {
            T obj = UnityEngine.Object.Instantiate(prefab, parent);
            return obj;
        }

        public T Get()
        {
            T obj;

            if (pool.Count > 0)
            {
                obj = pool.Pop();
            }
            else
            {
                obj = CreateNew();
            }

            obj.gameObject.SetActive(true);

            if (obj is IPoolable poolable)
            {
                poolable.OnSpawnFromPool();
            }

            return obj;
        }

        public T Get(Vector3 position)
        {
            T obj = Get();
            obj.transform.position = position;
            return obj;
        }

        public void Release(T obj)
        {
            if (obj == null)
                return;

            if (obj is IPoolable poolable)
            {
                poolable.OnReturnToPool();
            }

            obj.gameObject.SetActive(false);

            if (pool.Count < maxSize)
            {
                pool.Push(obj);
            }
            else
            {
                UnityEngine.Object.Destroy(obj.gameObject);
            }
        }

        public void Clear()
        {
            while (pool.Count > 0)
            {
                var obj = pool.Pop();
                if (obj != null)
                {
                    UnityEngine.Object.Destroy(obj.gameObject);
                }
            }
        }
    }
}
