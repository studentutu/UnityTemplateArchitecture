using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// Taken from https://www.codeproject.com/Articles/1094513/Pipeline-and-Filters-Pattern-using-Csharp
namespace App.Core
{
    /// <summary>
    /// A filter to be registered in the message processing pipeline
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IFilter<T>
    {
        /// <summary>
        /// Filter implementing this method would perform processing on the input type T
        /// </summary>
        /// <param name="input">The input to be executed by the filter</param>
        /// <returns></returns>
        T Execute(T input);

        /// <summary>
        /// Unique hashcode for the same type of class
        /// </summary>
        /// <returns></returns>
        int GetCustomHashCode();
    }

    /// <summary>
    /// An abstract Pipeline with a list of filters and abstract Process method
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class Pipeline<T>
    {
        /// <summary>
        /// List of filters in the pipeline
        /// </summary>
        protected readonly List<IFilter<T>> _filters = new List<IFilter<T>>();

        /// <summary>
        /// Gets a copy of filters
        /// </summary>
        public List<IFilter<T>> GetFilters()
        {
            return new List<IFilter<T>>(_filters);
        }

        /// <summary>
        /// To Register filter in the pipeline
        /// </summary>
        /// <param name="filterToCheck">A filter object implementing IFilter interface</param>
        /// <returns></returns>
        public Pipeline<T> Register(IFilter<T> filterToCheck)
        {
            if (!HasFilter(filterToCheck))
            {
                _filters.Add(filterToCheck);
            }

            return this;
        }

        public bool HasFilter(IFilter<T> filterToCheck)
        {
            int hashcode = filterToCheck.GetCustomHashCode();
            bool exists = false;
            for (int i = 0; i < _filters.Count && !exists; i++)
            {
                exists |= _filters[i].GetCustomHashCode() == hashcode;
            }

            return exists;
        }

        public bool HaveAllFilters(Pipeline<T> fromFilters)
        {
            int allFIlters = fromFilters._filters.Count;
            for (int i = 0; i < fromFilters._filters.Count; i++)
            {
                if (HasFilter(fromFilters._filters[i]))
                {
                    allFIlters--;
                }
            }

            return allFIlters <= 0;
        }

        /// <summary>
        /// To Remove filter in the pipeline
        /// </summary>
        /// <param name="filterToRemove">A filter object implementing IFilter interface</param>
        /// <returns></returns>
        public Pipeline<T> Remove(IFilter<T> filterToRemove)
        {
            int customHashCode = filterToRemove.GetCustomHashCode();
            for (int i = 0; i < _filters.Count; i++)
            {
                if (_filters[i].GetCustomHashCode() == customHashCode)
                {
                    _filters.RemoveAt(i);
                }
            }

            return this;
        }

        /// <summary>
        /// To start processing on the Pipeline
        /// </summary>
        /// <param name="input">
        /// The input object on which filter processing would execute</param>
        /// <returns></returns>
        public abstract T Process(T input);

        public bool IsEmpty()
        {
            return _filters.Count == 0;
        }

        /// <summary>
        /// Same as allow all
        /// </summary>
        public void ResetAllFilter()
        {
            _filters.Clear();
        }
    }

    /// <summary>
    /// Pipeline which selects final list
    /// </summary>
    public class FilterProcessor<T> : Pipeline<IEnumerable<T>>
    {
        /// <summary>
        /// Method which executes the filter on a given Input
        /// </summary>
        /// <param name="input">Input on which filtering
        /// needs to happen as implementing in individual filters</param>
        /// <returns></returns>
        public override IEnumerable<T> Process(IEnumerable<T> input)
        {
            var result = new List<T>();
            result.AddRange(input);
            for (int i = 0; i < _filters.Count; i++)
            {
                result = (List<T>) _filters[i].Execute(result);
            }

            return result;
        }
    }

    /// <summary>
    /// Pipeline which selects final list
    /// </summary>
    public class FilterFactory
    {
        /// <summary>
        /// Creates Simple Filter processor for lists/arrays/custom IEnumerable types
        /// </summary>
        /// <typeparam name="T"> Target Type</typeparam>
        /// <returns></returns>
        public static FilterProcessor<T> CreateProcessorFromFilter<T>(IFilter<IEnumerable<T>> filter)
        {
            var newPipiline = new FilterProcessor<T>();
            newPipiline.Register(filter);
            return newPipiline;
        }
    }
}