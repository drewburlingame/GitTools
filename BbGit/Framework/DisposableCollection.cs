using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BbGit.Framework
{
    public class DisposableCollection<T> : ICollection<T>, IDisposable where T : IDisposable
    {
        private ICollection<T> innerCollection;
        private bool isDisposing;

        public static DisposableCollection<T> Empty => new DisposableCollection<T>(new Collection<T>());

        public DisposableCollection(ICollection<T> innerCollection)
        {
            this.innerCollection = innerCollection;
        }

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator()
        {
            return this.innerCollection.GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) this.innerCollection).GetEnumerator();
        }

        /// <inheritdoc />
        public void Add(T item)
        {
            this.innerCollection.Add(item);
        }

        /// <inheritdoc />
        public void Clear()
        {
            this.innerCollection.Clear();
        }

        /// <inheritdoc />
        public bool Contains(T item)
        {
            return this.innerCollection.Contains(item);
        }

        /// <inheritdoc />
        public void CopyTo(T[] array, int arrayIndex)
        {
            this.innerCollection.CopyTo(array, arrayIndex);
        }

        /// <inheritdoc />
        public bool Remove(T item)
        {
            return this.innerCollection.Remove(item);
        }

        /// <inheritdoc />
        public int Count => this.innerCollection.Count;

        /// <inheritdoc />
        public bool IsReadOnly => this.innerCollection.IsReadOnly;

        /// <inheritdoc />
        public void Dispose()
        {
            if (this.isDisposing)
            {
                return;
            }

            lock (this)
            {
                if (this.isDisposing || this.innerCollection == null)
                {
                    return;
                }

                this.isDisposing = true;

                foreach (var item in this.innerCollection)
                {
                    item.Dispose();
                }

                this.innerCollection = null;
            }
        }
    }
}