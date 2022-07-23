using System;
using UnityEngine;

namespace App.Core.Attributes
{
    public enum ComparisonType
    {
        Equals,
        NotEquals,
        GreaterThan,
        SmallerThan,
        SmallerOrEquals,
        GreaterOrEquals
    }

    public enum DisablingType
    {
        ReadOnly,
        Hide
    }

    /// <summary>
    /// Draws the field/property ONLY if the compared property, compared by the comparison type with the value of comparedValue, returns true.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
    public class DrawIfAttribute : PropertyAttribute
    {
        public string ComparedPropertyName { get; private set; }
        public object ComparedValue { get; private set; }
        public ComparisonType ComparisonType { get; private set; }
        public DisablingType DisablingType { get; private set; }

        /// <summary>
        /// Draw the field only if a condition is met.
        /// </summary>
        /// <param name="comparedPropertyName">The name of the property that is being compared (case sensitive).</param>
        /// <param name="comparedValue">The value that property is being compared to.</param>
        /// <param name="comparisonType">The type of comparison the values will be compared by.</param>
        /// <param name="disablingType">The type of disabling that should happen if the condition is NOT met. Defaulted to DisablingType.Hide.</param>
        public DrawIfAttribute(string comparedPropertyName, object comparedValue, ComparisonType comparisonType, DisablingType disablingType = DisablingType.Hide)
        {
            ComparedPropertyName = comparedPropertyName;
            ComparedValue = comparedValue;
            ComparisonType = comparisonType;
            DisablingType = disablingType;
        }
    }
}