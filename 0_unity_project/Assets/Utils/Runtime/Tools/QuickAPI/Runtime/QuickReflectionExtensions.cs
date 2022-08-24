namespace QuickEditor.Core
{
    using System;
    using System.Reflection;

    public static class QuickReflectionExtensions
    {
        public static T InvokeConstructor<T>(this Type type, Type[] paramTypes = null, object[] paramValues = null)
        {
            return (T)type.InvokeConstructor(paramTypes, paramValues);
        }

        public static object InvokeConstructor(this Type type, Type[] paramTypes = null, object[] paramValues = null)
        {
            if (paramTypes == null || paramValues == null)
            {
                paramTypes = new Type[] { };
                paramValues = new object[] { };
            }

            var constructor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, paramTypes, null);

            return constructor.Invoke(paramValues);
        }

        public static T Invoke<T>(this object o, string methodName, params object[] args)
        {
            var value = o.Invoke(methodName, args);
            if (value != null)
            {
                return (T)value;
            }

            return default(T);
        }

        public static T Invoke<T>(this object o, string methodName, Type[] types, params object[] args)
        {
            var value = o.Invoke(methodName, types, args);
            if (value != null)
            {
                return (T)value;
            }

            return default(T);
        }

        public static object Invoke(this object o, string methodName, params object[] args)
        {
            Type[] types = new Type[args.Length];
            for (int i = 0; i < args.Length; i++)
                types[i] = args[i] == null ? null : args[i].GetType();

            return o.Invoke(methodName, types, args);
        }

        public static object Invoke(this object o, string methodName, Type[] types, params object[] args)
        {
            var invaild = Array.FindIndex(types, item => item == null) != -1;
            var type = o is Type ? (Type)o : o.GetType();

            var method = invaild ? type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) : type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, CallingConventions.Any, types, null);
            if (method != null)
                return method.Invoke(o, args);

            return null;
        }

        public static T GetFieldValue<T>(this object o, string name)
        {
            var value = o.GetFieldValue(name);
            if (value != null)
            {
                return (T)value;
            }

            return default(T);
        }

        public static object GetFieldValue(this object o, string name)
        {
            var field = o.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
            {
                return field.GetValue(o);
            }

            return null;
        }

        public static void SetFieldValue(this object o, string name, object value)
        {
            var field = o.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
            {
                field.SetValue(o, value);
            }
        }

        public static T GetPropertyValue<T>(this object o, string name)
        {
            var value = o.GetPropertyValue(name);
            if (value != null)
            {
                return (T)value;
            }

            return default(T);
        }

        public static object GetPropertyValue(this object o, string name)
        {
            var property = o.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null)
            {
                return property.GetValue(o, null);
            }

            return null;
        }

        public static void SetPropertyValue(this object o, string name, object value)
        {
            var property = o.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null)
            {
                property.SetValue(o, value, null);
            }
        }
    }
}
