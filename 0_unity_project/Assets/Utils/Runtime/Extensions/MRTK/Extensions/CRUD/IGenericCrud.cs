using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using App.Core.Services;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;

namespace App.Core
{
    public interface IGenericCrud<T, W> : IService
        where T : class
        where W : IGenericStorageService
    {
        W GetStorage();

        Task<Response> Create(T item);
        Task<Response> Update(T item);
        Task<Response> Delete(T item);
        Task<T> Get(string id);

        Task<List<T>> GetByQuery(string query);
    }
}