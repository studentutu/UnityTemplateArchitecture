using System;
using System.Collections;
using System.Collections.Generic;

namespace App.Core.CommonPatterns
{
    public abstract class LazyItem<T> : Lazy<T>, IEnumerable<T>
    {
        public LazyItem(Func<T> function):base(function)
        {
        }

        protected abstract LazyItem<T> GenerateNextValue();

        public IEnumerator<T> GetEnumerator()
        {
            var currentRoot = this;
            while (currentRoot != null)
            {
                yield return currentRoot.Value;
                currentRoot = currentRoot.GenerateNextValue();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}