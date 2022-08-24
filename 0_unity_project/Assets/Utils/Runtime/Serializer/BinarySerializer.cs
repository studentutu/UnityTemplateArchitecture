using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using UnityEngine;
using System;

namespace App.Core.Serializer
{
    public class BinarySerializer : IDataSerializer<string>
    {

        public override bool isSaveExist (string filenameOrPath ) { return File.Exists(filenameOrPath); } 

        protected override void SaveT(string data, string filenameorPath)
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Create(filenameorPath);
            bf.Serialize(file, data);
            file.Close();
        }

        protected override string LoadT( string from)
        {
            string data = null;
            if (isSaveExist(from))
            {
                BinaryFormatter bf = new BinaryFormatter();
                FileStream file = File.Open(from, FileMode.Open);
                try
                {
                    data = (string)bf.Deserialize(file);
                }
                catch (SerializationException)
                {
                    file?.Close();
                    return null;
                }
                finally
                {

                    file?.Close();
                }
            }
            return data;
        }

        public override void Delete( string from)
        {
            if (isSaveExist(from))
            {
                File.Delete(from);
            }
        }
    }
    public class BinarySerializer<T> : IDataSerializer<T>
    {
        
        public override bool isSaveExist (string from ) { return File.Exists(from); } 

        protected override void SaveT(T data,string  to)
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Create(to);
            bf.Serialize(file, data);
            file.Close();
        }

        protected override T LoadT( string from)
        {
            T data = default(T);
            if (isSaveExist(from))
            {
                BinaryFormatter bf = new BinaryFormatter();
                FileStream file = File.Open(from, FileMode.Open);
                try
                {
                    data = (T)bf.Deserialize(file);
                }
                catch (SerializationException)
                {
                    file?.Close();
                    return default(T);
                }
                finally
                {

                    file?.Close();
                }
            }
            return data;
        }

        public override void Delete(string from )
        {
            if (isSaveExist(from))
            {
                File.Delete(from);
            }
        }
    }
}