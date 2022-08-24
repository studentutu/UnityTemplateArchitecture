using UnityEngine;
using UnityEditor;
using System.Collections;
using System;

namespace CustomDefineManagement {
    public partial class CustomDefineManager {
        [Flags]

        public enum cdmBuildTargetGroup {
            Standalone = 1,
            iOS = 2,
            Android = 4,
            WebGL = 8,
        }
    }

    public static class BuildTargetGroupExtensions {
        public static BuildTargetGroup ToBuildTargetGroup(this CustomDefineManager.cdmBuildTargetGroup tg) {
            //Debug.Log(tg.ToString());
            return (BuildTargetGroup)Enum.Parse(typeof(BuildTargetGroup), tg.ToString());
        }

        public static string ToIconName(this CustomDefineManager.cdmBuildTargetGroup tg) {
            switch (tg) {
                case CustomDefineManager.cdmBuildTargetGroup.iOS: return "iPhone";
#if UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3 || UNITY_5_4
                case CustomDefineManager.cdmBuildTargetGroup.Nintendo3DS: return "N3DS";
#endif
            }

            return tg.ToString();
        }

        /// <summary>
        /// A FX 3.5 way to mimic the FX4 "HasFlag" method.
        /// Frm: http://www.sambeauvois.be/blog/2011/08/enum-hasflag-method-extension-for-4-0-framework/ thanks Sam!
        /// </summary>
        /// <param name="variable">The tested enum.</param>
        /// <param name="value">The value to test.</param>
        /// <returns>True if the flag is set. Otherwise false.</returns>
        public static bool HasFlag(this Enum variable, Enum value) {
            // check if from the same type.
            if (variable.GetType() != value.GetType()) {
                throw new ArgumentException("The checked flag is not from the same type as the checked variable.");
            }

            ulong num = Convert.ToUInt64(value);
            ulong num2 = Convert.ToUInt64(variable);

            return (num2 & num) == num;
        }
    }
}