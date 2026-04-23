using System;
using System.Collections.Generic;

namespace Liminal.SDK.Collections
{
    /// <summary>
    /// <para>This class provides an interface for acquiring a <see cref="List{T}"/> instance from a pool. If a list is available in the pool, it will
    /// be returned via the <see cref="Get"/> method, otherwise a new <see cref="List{T}"/> instance will be created. When you are finished with
    /// the list, it can be returned to the pool via <see cref="Release(ref List{T})"/>. The <c>ref</c> keyword allows the <see cref="Release(ref List{T})"/>
    /// method to clear the list reference for you automatically.
    /// </para>
    /// </summary>
    /// <remarks>
    /// The <see cref="PooledList{T}"/> struct provides a friendly interface for accessing lists from <see cref="ListPool{T}"/> via the <c>using</c> syntax.
    /// </remarks>
    /// <typeparam name="T">The type of the list pool.</typeparam>
    /// <example>
    /// An example of how to use the <see cref="ListPool{T}"/> class.
    /// <code>
    /// var myIntList = ListPool&lt;int&gt;.Get();
    /// myIntList.Add(100);
    /// // .. Do something with the list contents ..
    /// ListPool&lt;int&gt;.Release(ref myIntList);
    /// System.Diagnostics.Debug.Assert(myIntList == null);
    /// </code>
    /// </example>
    public static class ListPool<T>
    {
        private const int _defaultCapacity = 0;
        private static readonly Queue<List<T>> _pool = new Queue<List<T>>();
        
        #region Properties

        /// <summary>
        /// Gets the number of lists available in the pool.
        /// </summary>
        public static int PoolCount
        {
            get { return _pool.Count; }
        }
        
        #endregion

        /// <summary>
        /// Gets a <see cref="List{T}"/> from the pool, or creates a new one if the pool is exhausted.
        /// </summary>
        /// <returns>A <see cref="List{T}"/> from the pool, or a new list if the pool is exhausted.</returns>
        /// <remarks>Make sure that you return the list to the pool using <see cref="Release(ref List{T})"/>.</remarks>
        public static List<T> Get()
        {
            List<T> list;
            if (_pool.Count > 0)
            {
                list = _pool.Dequeue();
            }
            else
            {
                list = new List<T>(_defaultCapacity);
            }

            return list;
        }

        /// <summary>
        /// Clears and releases a <see cref="List{T}"/> to the pool. The reference is also set to null.
        /// </summary>
        /// <param name="list">The <see cref="List{T}"/> to clear and return to the pool.</param>
        public static void Release(ref List<T> list)
        {
            if (list == null)
                return;

            list.Clear();
            _pool.Enqueue(list);
            list = null;
        }
    }

    /// <summary>
    /// A disposable struct that allows access to <see cref="ListPool{T}"/> instances via the <c>using</c> syntax.
    /// </summary>
    /// <typeparam name="T">The type of the list</typeparam>
    /// <example>
    /// An example of how to use a disposable PooledList struct.
    /// <code>
    /// using (var pList = new PooledList&lt;int&gt;())
    /// {
    ///     var list = pList.List;
    ///     // .. Do something with list ..
    /// }
    /// </code>
    /// </example>
    public struct PooledList<T> : IDisposable
    {
        private volatile bool mDisposed;
        private List<T> mList;
        
        /// <summary>
        /// Gets the pooled list assigned assigned to the struct.
        /// </summary>
        public List<T> List
        {
            get
            {
                if (mList == null)
                    mList = ListPool<T>.Get();

                return mList;
            }
        }

        /// <summary>
        /// Disposes of the pooled list, returning it to the pool.
        /// </summary>
        public void Dispose()
        {
            if (mDisposed)
                return;

            if (mList != null)
            {
                ListPool<T>.Release(ref mList);
            }

            mDisposed = true;
        }
    }
}
