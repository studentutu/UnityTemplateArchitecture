using System.Collections.Generic;
using UnityEngine;

namespace App.Core.Services
{
    public abstract class ICacheMock : ScriptableObject
    {
        /// <summary>
        /// Fills in cache but does not overrides list listOfUnmodifiedCachedKeys
        /// </summary>
        /// <param name="listOfUnmodifiedCachedKeys"></param>
        public abstract void FillIn(List<string> listOfUnmodifiedCachedKeys);
    }
}