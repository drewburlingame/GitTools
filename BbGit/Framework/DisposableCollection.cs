using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BbGit.Framework
{
    public class DisposableCollection<T> : ICollection<T>, IDisposable where T : IDisposable
    {
        private readonly ICollection<T> _innerCollection;
        private bool _isDisposing;

        public static DisposableCollection<T> Empty => new(new Collection<T>());

        public DisposableCollection(ICollection<T> innerCollection)
        {
            _innerCollection = innerCollection;
        }
        
        public IEnumerator<T> GetEnumerator()
        {
            AssertNotDisposed();
            return _innerCollection.GetEnumerator();
        }
        
        IEnumerator IEnumerable.GetEnumerator()
        {
            AssertNotDisposed();
            return ((IEnumerable) _innerCollection).GetEnumerator();
        }
        
        public void Add(T item)
        {
            AssertNotDisposed();
            _innerCollection.Add(item);
        }
        
        public void Clear()
        {
            AssertNotDisposed();
            _innerCollection.Clear();
        }
        
        public bool Contains(T item)
        {
            AssertNotDisposed();
            return _innerCollection.Contains(item);
        }
        
        public void CopyTo(T[] array, int arrayIndex)
        {
            AssertNotDisposed();
            _innerCollection.CopyTo(array, arrayIndex);
        }
        
        public bool Remove(T item)
        {
            AssertNotDisposed();
            return _innerCollection.Remove(item);
        }
        
        public int Count
        {
            get { AssertNotDisposed(); return _innerCollection.Count; }
        }

        public bool IsReadOnly
        {
            get { AssertNotDisposed(); return _innerCollection.IsReadOnly; }
        }

        public void Dispose()
        {
            if (_isDisposing)
            {
                return;
            }

            lock (this)
            {
                if (_isDisposing)
                {
                    return;
                }

                _isDisposing = true;

                foreach (var item in _innerCollection)
                {
                    item.Dispose();
                }
            }
        }

        private void AssertNotDisposed()
        {
            if (_isDisposing) throw new ObjectDisposedException(nameof(DisposableCollection<T>));
        }
    }
}