using UnityEngine;

namespace App.Core.Tools
{
    public static class Constants
    {
        public static class Generation
        {
            public const float OFFSET_TEXT = 0.01f;
            public const float OFFSET_STANDARD_OBJECTS = 0;
        }

        public static class PhysicalLayers
        {
            private static int defaultLayer = -1;
            private static int transparentFX = -1;
            private static int ignoreRaycast = -1;
            private static int layer3 = -1;
            private static int water = -1;
            private static int ui = -1;
            private static int layer6 = -1;
            private static int layer7 = -1;
            private static int spatial_Awareness = -1;

            /// <summary>
            /// Actual integer  - number
            /// </summary>
            /// <value></value>
            public static int Default
            {
                get
                {
                    if (defaultLayer == -1)
                    {
                        defaultLayer = LayerMask.NameToLayer("Default");
                    }
                    return defaultLayer;
                }
            }

            /// <summary>
            /// Actual integer  - number
            /// </summary>
            /// <value></value>
            public static int TransparentFX
            {
                get
                {
                    if (transparentFX == -1)
                    {
                        transparentFX = LayerMask.NameToLayer("TransparentFX");
                    }
                    return transparentFX;
                }
            }

            /// <summary>
            /// Actual integer  - number
            /// </summary>
            /// <value></value>
            public static int IgnoreRaycast
            {
                get
                {
                    if (ignoreRaycast == -1)
                    {
                        ignoreRaycast = 2;
                    }
                    return ignoreRaycast;
                }
            }

            /// <summary>
            /// Actual integer  - number
            /// </summary>
            /// <value></value>
            public static int Layer3
            {
                get
                {
                    if (layer3 == -1)
                    {
                        layer3 = 3;
                    }
                    return layer3;
                }
            }

            /// <summary>
            /// Actual integer  - number
            /// </summary>
            /// <value></value>
            public static int Water
            {
                get
                {
                    if (water == -1)
                    {
                        water = LayerMask.NameToLayer("Water");
                    }
                    return water;
                }
            }

            /// <summary>
            /// Actual integer  - number
            /// </summary>
            /// <value></value>
            public static int UI
            {
                get
                {
                    if (ui == -1)
                    {
                        ui = LayerMask.NameToLayer("UI");
                    }
                    return ui;
                }
            }

            /// <summary>
            /// Actual integer  - number
            /// </summary>
            /// <value></value>
            public static int Layer6
            {
                get
                {
                    if (layer6 == -1)
                    {
                        layer6 = 6;
                    }
                    return layer6;
                }
            }

            /// <summary>
            /// Actual integer  - number
            /// </summary>
            /// <value></value>
            public static int Layer7
            {
                get
                {
                    if (layer7 == -1)
                    {
                        layer7 = 7;
                    }
                    return layer7;
                }
            }

            public static int SpatialAwareness
            {
                get
                {
                    if (spatial_Awareness == -1)
                    {
                        spatial_Awareness = 31;
                    }

                    return spatial_Awareness;
                }
            }
        }

        public static class UISortingLayers
        {
            private static int defaultUI = -1;
            public static int Default
            {
                get
                {
                    if (defaultUI == -1)
                    {
                        defaultUI = SortingLayer.GetLayerValueFromName("Default");
                    }
                    return defaultUI;
                }
            }
        }

        public static class Tags
        {
            public const string UNTAGGED = "Untagged";
            public const string FINISH = "Finish";
            public const string CAMERA_MAIN = "MainCamera";
            public const string RESPAWN = "Respawn";
            public const string EDITOR_ONLY = "EditorOnly";
            public const string PLAYER = "Player";
            public const string GAME_CONTROLLER = "GameController";
        }

        public static class Animations
        {
            // public const int PhysicalLayerForInteraction = 10;
        }

        public static class Strings
        {
            // public const string PhysicalLayerForInteraction = 10;
        }

        public static class ShaderProperties
        {
            public const string Main_Texture = "_MainTex";
        }

        public static class ResourcesPath
        {
            
        }
    }
}