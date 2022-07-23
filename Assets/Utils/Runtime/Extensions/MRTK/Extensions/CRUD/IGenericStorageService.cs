using System.Threading.Tasks;
using App.Core.Services;
using Microsoft.MixedReality.Toolkit.Utilities;

namespace App.Core
{
    public interface IGenericStorageService : IService
    {
        string GetBaseURL();

        Task<Response> GetAll();
        Task<Response> GetById(string id);
        Task<Response> GetAllWithQuery(string queryWithArguments);
    }
}