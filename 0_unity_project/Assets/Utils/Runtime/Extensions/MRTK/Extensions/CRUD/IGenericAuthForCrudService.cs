using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using App.Core.Services;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;

namespace App.Core
{
    public interface IGenericAuthForCrudService : IService
    {
        Task<Response> RequestToken();
        string GetTokenWithPrefix();
    }
}