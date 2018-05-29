using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BbGit.Framework
{
    public class DisposableColleciton<T> : ICollection<T>, IDisposable where T : IDisposable
    {
        private ICollection<T> innerCollection;
        private bool isDisposing;

        public static DisposableColleciton<T> Empty => new DisposableColleciton<T>(new Collection<T>());

        public DisposableColleciton(ICollection<T> innerCollection)
        {
            this.innerCollection = innerCollection;
        }

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator()
        {
            return innerCollection.GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) innerCollection).GetEnumerator();
        }

        /// <inheritdoc />
        public void Add(T item)
        {
            innerCollection.Add(item);
        }

        /// <inheritdoc />
        public void Clear()
        {
            innerCollection.Clear();
        }

        /// <inheritdoc />
        public bool Contains(T item)
        {
            return innerCollection.Contains(item);
        }

        /// <inheritdoc />
        public void CopyTo(T[] array, int arrayIndex)
        {
            innerCollection.CopyTo(array, arrayIndex);
        }

        /// <inheritdoc />
        public bool Remove(T item)
        {
            return innerCollection.Remove(item);
        }

        /// <inheritdoc />
        public int Count => innerCollection.Count;

        /// <inheritdoc />
        public bool IsReadOnly => innerCollection.IsReadOnly;

        /// <inheritdoc />
        public void Dispose()
        {
            if (isDisposing)
            {
                return;
            }

            lock (this)
            {
                if (isDisposing || this.innerCollection == null)
                {
                    return;
                }
                isDisposing = true;

                foreach (var item in this.innerCollection)
                {
                    item.Dispose();
                }
                this.innerCollection = null;
            }
        }
    }
}