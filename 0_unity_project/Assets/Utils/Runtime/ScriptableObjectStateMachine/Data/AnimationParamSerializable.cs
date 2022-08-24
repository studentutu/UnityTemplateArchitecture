using System;
using System.Collections.Generic;
using App.Core.Attributes;
using UnityEngine;

namespace App.Core.Tools.StateMachine.Data
{
    [Serializable]
    public class AnimationParamSerialiazable
    {
	    public enum ConditionalComparisonType
	    {
		    Equals,
		    NotEquals,
		    GreaterThan,
		    SmallerThan,
		    SmallerOrEquals,
		    GreaterOrEquals
	    }
	    
	    [Serializable]
	    public class TransitionCondition
	    {
		    
		    public AnimationParamSerialiazable parameter;
		    [DrawIf(nameof(parameter) + "/" + nameof(AnimationParamSerialiazable.type),AnimatorControllerParameterType.Bool, ComparisonType.NotEquals)]
		    [DrawIf(nameof(parameter) + "/" + nameof(AnimationParamSerialiazable.type),AnimatorControllerParameterType.Trigger, ComparisonType.NotEquals)]
		    public ConditionalComparisonType comparisonType = ConditionalComparisonType.Equals;
	    }
	    
	    private static Dictionary<AnimatorControllerParameterType, Func<AnimationParamSerialiazable,TransitionCondition, bool>> strategy = null;

	    protected static Dictionary<AnimatorControllerParameterType, Func<AnimationParamSerialiazable,TransitionCondition, bool>> Strategy
	    {
		    get
		    {
			    if (strategy == null)
			    {
				    strategy =
					    new Dictionary<AnimatorControllerParameterType, Func<AnimationParamSerialiazable,TransitionCondition, bool>>()
					    {
						    {AnimatorControllerParameterType.Trigger,AreEqualTriggers },
						    {AnimatorControllerParameterType.Bool,AreEqualBool },
						    {AnimatorControllerParameterType.Int,AreEqualInt },
						    {AnimatorControllerParameterType.Float,AreEqualFloat },
					    };

			    }

			    return strategy;
		    }
	    }

	    private static bool AreEqualTriggers(AnimationParamSerialiazable original , TransitionCondition checkWith)
	    {
		    return original.triggerValue == checkWith.parameter.triggerValue;
	    }
		
	    private static bool AreEqualBool(AnimationParamSerialiazable original , TransitionCondition checkWith)
	    {
		    return original.boolValue == checkWith.parameter.boolValue;
	    }
		
	    private static bool AreEqualInt(AnimationParamSerialiazable original , TransitionCondition checkWith)
	    {
		    return DictOfComparisonTypes[checkWith.comparisonType].Invoke(original.intValue,checkWith.parameter.intValue );
	    }
	    
	    private static bool AreEqualFloat(AnimationParamSerialiazable original , TransitionCondition checkWith)
	    {
		    return DictOfComparisonTypes[checkWith.comparisonType].Invoke(original.floatValue,checkWith.parameter.floatValue );
	    }

	    private static Dictionary<ConditionalComparisonType, Func<float, float, bool>> dictOfComparisonTypes = null;

	    protected static Dictionary<ConditionalComparisonType, Func<float, float, bool>> DictOfComparisonTypes
	    {
		    get
		    {
			    if (dictOfComparisonTypes == null)
			    {
				    dictOfComparisonTypes = new Dictionary<ConditionalComparisonType, Func<float, float, bool>>()
				    {
					    {ConditionalComparisonType.Equals, AreEqualActualFloat},
					    {ConditionalComparisonType.NotEquals, AreNotEqualActualFloat},
					    {ConditionalComparisonType.SmallerThan, AreLessActualFloat},
					    {ConditionalComparisonType.SmallerOrEquals, AreLessEqualActualFloat},
					    {ConditionalComparisonType.GreaterThan, AreGreaterActualFloat},
					    {ConditionalComparisonType.GreaterOrEquals, AreGreaterEqualActualFloat},

				    };
			    }

			    return dictOfComparisonTypes;
		    }
	    }
	    
	    private static bool AreEqualActualFloat(float original , float checkWith)
	    {
		    return Mathf.Abs(original - checkWith) < 0.0001f;
	    }
	    
	    private static bool AreNotEqualActualFloat(float original , float checkWith)
	    {
		    return original != checkWith;
	    }
	    private static bool AreLessEqualActualFloat(float original , float checkWith)
	    {
		    return checkWith <= original;
	    }
	    private static bool AreLessActualFloat(float original , float checkWith)
	    {
		    return  checkWith < original;
	    }
	    private static bool AreGreaterActualFloat(float original , float checkWith)
	    {
		    return original < checkWith;
	    }
	    private static bool AreGreaterEqualActualFloat(float original , float checkWith)
	    {
		    return original <= checkWith;
	    }
	    
	    
        [SerializeField] public string keyName = string.Empty;
        [HideInInspector] [SerializeField] public int nameHash = 0; // better to not disclose them
        [SerializeField] public AnimatorControllerParameterType type = AnimatorControllerParameterType.Bool;
        [SerializeField] public bool boolValue = false;
        [SerializeField] public int intValue = 0;
        [SerializeField] public float floatValue = 0;
        [SerializeField] public bool triggerValue = false;

        public void CopyTo(AnimationParamSerialiazable copyTo)
        {
	        copyTo.keyName = keyName;
	        copyTo.nameHash = nameHash;
	        copyTo.type = type;
	        copyTo.boolValue = boolValue;
	        copyTo.intValue = intValue;
	        copyTo.floatValue = floatValue;
	        copyTo.triggerValue = triggerValue;
        }

        public bool AreEqual(TransitionCondition another)
        {
	        return nameHash == another.parameter.nameHash && Strategy[type].Invoke(this, another);
        }
    }
}