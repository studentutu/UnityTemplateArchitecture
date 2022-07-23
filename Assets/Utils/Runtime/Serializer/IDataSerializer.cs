using System;

namespace App.Core.Serializer
{
    public abstract class IDataSerializer<T> : IDataSerializerBase
    {
        public abstract bool isSaveExist( string filenameOrPath);

        public event Action<System.Object> OnLoad;

        protected abstract void SaveT(T data, string to);
        protected abstract T LoadT( string from);

        public void Save(object data, string to)
        {
            SaveT((T)data, to);
        }

        public abstract void Delete( string from);

        public void Load( string from)
        {
            OnLoad.Invoke(LoadT(from));
        }
    }
    public interface IDataSerializerBase
    {
        event System.Action<System.Object> OnLoad;
        bool isSaveExist( string filenameOrPath);
        void Save(System.Object data, string to);
        void Delete( string from);
        void Load( string from);
    }
}