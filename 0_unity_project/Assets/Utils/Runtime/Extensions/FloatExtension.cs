namespace App.Core.Extensions
{
    public static class FloatExtension
    {
        /// <summary>
        /// Remap values from input range to target range
        /// </summary>
        /// <param name="value"> current value </param>
        /// <param name="rangemin1"> from range begins</param>
        /// <param name="rangemax1"> from range begins</param>
        /// <param name="rangemin2"> target range ends</param>
        /// <param name="rangemax2">target range ends</param>
        /// <returns></returns>
        public static float Remap(float value, float rangemin1, float rangemax1, float rangemin2, float rangemax2)
        {
            return (value - rangemin1) / (rangemax1 - rangemin1) * (rangemax2 - rangemin2) + rangemin2;
        }
    }
}