using App.Core.Services;
using Microsoft.MixedReality.Toolkit.Utilities;

namespace App.Core.CRUD
{

    public enum CacheType : int
    {
        /// <summary>
        /// Corresponds to infinity days
        /// </summary>
        LongTerm  = 0,
        /// <summary>
        /// Corresponds to 3 days
        /// </summary>
        Content = 1,
        
        /// <summary>
        /// 30 days
        /// </summary>
        Days30 = 2,
    }

    public interface IRestCacheService : IService
    {
        void InvalidateOldCache();
        
        void SaveQuery(string query, Response responseToSave, CacheType cacheType, bool usePerUserCache);
        Response GetQuery(string query);
        void DeleteQuery(string query);
    }
}