using System;
using UnityEngine;

namespace App.Core.Serializer
{
    /// <summary> Each Primitive should be used in Here </summary>
    public class PlayerPrefsSerializerPrimitiveString : IDataSerializer<string>
    {

        public override bool isSaveExist( string from) { return PlayerPrefs.HasKey(from); }

        protected override void SaveT(string data, string from)
        {
            PlayerPrefs.SetString(from, data);
            PlayerPrefs.Save();
        }

        protected override string LoadT(string from)
        {
            return PlayerPrefs.GetString(from, null);
        }

        public override void Delete( string from)
        {
            if (isSaveExist(from))
            {
                PlayerPrefs.DeleteKey(from);
            }
        }

    }
    /// <summary> Each Primitive should be used in Here </summary>
    public class PlayerPrefsSerializerPrimitiveInt : IDataSerializer<int>
    {

        public override bool isSaveExist( string from) { return PlayerPrefs.HasKey(from); }

        protected override void SaveT(int data, string from)
        {
            PlayerPrefs.SetString(from, data.ToString());
            PlayerPrefs.Save();
        }

        protected override int LoadT( string from)
        {
            return int.Parse( PlayerPrefs.GetString(from, "0"));
        }

        public override void Delete( string from)
        {
            if (isSaveExist(from))
            {
                PlayerPrefs.DeleteKey(from);
            }
        }

    }

    /// <summary> Sadly does't work with primitive types! </summary>
    public class PlayerPrefsSerializer<W> : IDataSerializer<W>
        where W : new()
    {

        public override bool isSaveExist( string from ) { return PlayerPrefs.HasKey(from); }

        protected override void SaveT(W data, string from )
        {
            string dataJson = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(from, dataJson);
            PlayerPrefs.Save();
        }

        protected override W LoadT(string from)
        {
            W data = default(W);
            if (isSaveExist(from))
            {
                string dataJson = PlayerPrefs.GetString(from);
                if (!string.IsNullOrEmpty(dataJson) )
                {
                    data = JsonUtility.FromJson<W>(dataJson);
                }
                
            }
            return data;
        }

        public override void Delete( string from)
        {
            if (isSaveExist(from))
            {
                PlayerPrefs.DeleteKey(from);
            }
        }
    }
}