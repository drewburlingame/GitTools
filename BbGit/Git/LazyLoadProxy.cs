using System;

namespace BbGit.Git
{
    public class LazyLoadProxy<T>
    {
        private T? _value;
        private Func<T>? _valueProvider;

        public T? Value
        {
            get
            {
                if (_valueProvider != null)
                {
                    _value = _valueProvider();
                    _valueProvider = null;
                }

                return _value;
            }
            set
            {
                _value = value;
                _valueProvider = null;
            }
        }
        
        private LazyLoadProxy()
        {
        }
        
        public LazyLoadProxy(Func<T> valueProvider)
        {
            _valueProvider = valueProvider;
        }

        public static implicit operator T?(LazyLoadProxy<T> lazy)
        {
            return lazy.Value;
        }

        public static implicit operator LazyLoadProxy<T>(T value)
        {
            return new LazyLoadProxy<T> {Value = value};
        }
    }
}