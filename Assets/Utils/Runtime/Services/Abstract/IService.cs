namespace App.Core.Services
{
    
    /// <summary>
    /// Always invoke ConfigureCustomService and put all of the DI in there!
    /// </summary>
    public interface IService
    {
        [UnityEngine.Scripting.Preserve]
        void ConfigureCustomService();
    }
}