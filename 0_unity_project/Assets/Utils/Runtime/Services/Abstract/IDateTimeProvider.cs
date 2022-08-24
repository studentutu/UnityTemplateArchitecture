using System;

namespace App.Core.Services
{
    public interface IDateTimeService : IService
    {
        DateTime GetCurrentDateTime();
        DateTimeOffset GetCurrentDateTimeWithOffset();
    }
}