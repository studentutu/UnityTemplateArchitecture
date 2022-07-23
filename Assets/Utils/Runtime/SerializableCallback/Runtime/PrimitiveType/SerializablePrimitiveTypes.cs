using System;
using App.Core.Tools.SerializableFunc;
using UnityEngine;

namespace App.Core
{
    [Serializable]
    public class SerializableBoolFunc : SerializableCallback<bool>
    {
    }
    
    [Serializable]
    public class SerializableStringFunc : SerializableCallback<string>
    {
    }
    
    [Serializable]
    public class SerializableIntFunc : SerializableCallback<int>
    {
    }
    
    [Serializable]
    public class SerializableFloatFunc : SerializableCallback<float>
    {
    }
    
    [Serializable]
    public class SerializableGameObjectFunc : SerializableCallback<GameObject>
    {
    }
}