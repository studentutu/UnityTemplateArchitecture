using System.Collections.Generic;

namespace App.Core.MVC.Abstractions
{
    public interface IUser
    {
        string GetName();
        string GetLogin();

        /// <summary>
        /// Token without Prefix
        /// </summary>
        /// <returns></returns>
        string GetToken();

        Dictionary<string, System.Object> GetAdditionalData();
    }
}