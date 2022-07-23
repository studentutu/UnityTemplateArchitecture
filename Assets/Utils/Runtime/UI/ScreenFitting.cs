using System;
using UnityEngine;

namespace App.Core.Tools
{
    public static class ScreenFitting
    {
        public enum CanvasScale
        {
            Heigth,
            Width
        }
        
        public static Vector2 CurrentAspectRatio = Vector2.zero;
        private static readonly Vector2 HD = new Vector2(9, 16);

        /// <summary>
        /// Reverse of HD
        /// </summary>
        private static readonly Vector2 RHD = new Vector2(16, 9);

        public static readonly Vector2 QXGA = new Vector2(3, 4);

        /// <summary>
        /// Reverse of QXGA
        /// </summary>
        public static readonly Vector2 RQXGA = new Vector2(4, 3);


        public static CanvasScale CurrentGameScale = CanvasScale.Heigth;

        public static void CalculateCanvasScale(Canvas canvasToCalculateFrom)
        {
            CurrentAspectRatio = GetAspectRatio(Screen.width, Screen.height);
            if (CurrentAspectRatio == HD || CurrentAspectRatio == RHD || CurrentAspectRatio == QXGA ||
                CurrentAspectRatio == RQXGA)
            {
                // Height
                // 3/4
                CurrentGameScale = CanvasScale.Heigth;
            }
            else
            {
                // Width
                // OneTo2 ROneTo2
                // PhoneX
                CurrentGameScale = CanvasScale.Width;
            }
        }

        private static Vector2 GetAspectRatio(int width, int height)
        {
            float divRes = (float) width / (float) height;
            int index = 0;
            while (true)
            {
                index++;
                if (Math.Round(divRes * index, 2) == Mathf.RoundToInt(divRes * index))
                {
                    break;
                }
            }

            return new Vector2((float) Math.Round(divRes * index, 2), index);
        }
    }
}