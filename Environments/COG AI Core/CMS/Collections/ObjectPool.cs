using System;
using System.Collections.Concurrent;

namespace CMS.Collections
{
    /// <summary>
    /// A pool of reusable objects.
    /// </summary>
    /// <typeparam name="T">Type of objects in this pool.</typeparam>
    public class ObjectPool<T> 
        where T : IPoolable, new()
    {
        private readonly ConcurrentBag<T> _pool;
        private int _count;

        /// <summary>
        /// Creates an object pool with <paramref name="initialSize"/> objects.
        /// </summary>
        public ObjectPool(int initialSize = 16)
        {
            _pool = new ConcurrentBag<T>();
            _count = initialSize;

            AddObjects();
        }

        /// <summary>
        /// Increases the object pool size.
        /// </summary>
        private void AddObjects()
        {
            var toAdd = _count - _pool.Count;
#if DEBUG
            Console.WriteLine($"Adding new objects {toAdd} to count: {_count}");
#endif
            for (int i = 0; i < toAdd; i++)
            {
                _pool.Add(new T());
            }
            _count *= 2;
        }

        /// <summary>
        /// Takes and returns resetted object from the pool. 
        /// If there are no more objects the pool size is increased.
        /// </summary>
        public T Get()
        {
            T item;
            if (_pool.TryTake(out item))
            {
                return item;
            }
            else
            {
                AddObjects();
                _pool.TryTake(out item);
                return item;
            }
        }

        /// <summary>
        /// Return <paramref name="item"/> to the pool of objects.
        /// </summary>
        public void Put(T item)
        {
            item.Reset();
            _pool.Add(item);
        }

        /// <summary>
        /// Number of objects remaining in the pool.
        /// </summary>
        public int Count => _pool.Count;
    }
}
