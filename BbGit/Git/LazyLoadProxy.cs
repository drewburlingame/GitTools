using System;

namespace BbGit.Git
{
    public class LazyLoadProxy<T>
    {
        private Func<T> valueProvider;

        private T value;

        public static implicit operator T(LazyLoadProxy<T> lazy)
        {
            return lazy.Value;
        }
        public static implicit operator LazyLoadProxy<T>(T value)
        {
            return new LazyLoadProxy<T> {Value = value};
        }

        public T Value
        {
            get
            {
                if (this.valueProvider != null)
                {
                    this.value = this.valueProvider();
                    this.valueProvider = null;
                }

                return this.value;
            }
            set
            {
                this.value = value;
                this.valueProvider = null;
            }
        }

        /// <inheritdoc />
        private LazyLoadProxy()
        {
        }

        /// <inheritdoc />
        public LazyLoadProxy(Func<T> valueProvider)
        {
            this.valueProvider = valueProvider;
        }
    }
}