Shader "Unlit/CheckerBoxShader"
{
    Properties
    {
        _Color1 ("Color1", Color) = (0.5,0.5,0.5,1 )
        _Color2 ("Color2", Color) = (1,1,1,1 )
        _Number ("Split into ", Float) = 24
    	
//    	[IntRange]_StencilRef("Stencil Reference", Range(0,255)) = 1
		
		// 0 disabled, 1 - Never, 2 - Less, 3 - equal,
		// 4- LessEqual
		// 5 - Greater , 6 - NotEqual, 7- GreaterEqual
		// 8 - always
		// [IntRange]
//		[Enum(UnityEngine.Rendering.CompareFunction)]_StencilComp(" Stencil Comparison", Int) = 8
//		[Header(Stencil Stuff)]
		// 0 keep, 1 - Zero, 2 - Replace, 
		// 3 - IncrementSaturate ,4- DecrementSaturate
		// 5 - Invert , 6 - IncrementWrap,
		// 7- DecrementWrap
//		[Enum(UnityEngine.Rendering.StencilOp)]_StencilOp(" Stencil Operation", Int) = 0
//		[Enum(UnityEngine.Rendering.StencilOp)]_StencilOpFail(" Stencil Operation On Fail", Int) = 0
//		[Enum(UnityEngine.Rendering.StencilOp)]_StencilOpZFail(" Stencil Operation On ZTestFail", Int) = 0

		// [IntRange]_StencilWriteMask(" Stencil Write Mask", Range(0,255)) = 255
		// [IntRange]_StencilReadMask(" Stencil Read Mask", Range(0,255)) = 255

		// [Enum(UnityEngine.Rendering.ColorWriteMask)] _ColorWriteMask("Color Write Mask", Float) = 15 // "All"
        // [Enum(UnityEngine.Rendering.CullMode)] _CullMode("Cull Mode", Float) = 2  
        // [Enum(DepthWrite)] _ZWrite("Depth Write", Float) = 1                                         // "On"
        //
        // 
        // [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Source Blend", Float) = 1                 // "One"
        // [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Destination Blend", Float) = 0  
    }
    SubShader
    {
        Tags 
		{ 
			"LightMode" = "ForwardBase"
			"RenderType" = "Opaque"  // Opaque  transparent tranparentcutout
			"Queue" = "Geometry+100"  // Geometry  
			"PreviewType" = "Plane"
			"DisableBatching" = "false"
			"ForceNoShadowCasting" = "true"
			"IgnoreProjector" = "true"
			"CanUseSpriteAtlas" = "false"
		}
//		Lighting Off
//        Offset -1 ,-1
//      ZTest Always 
//    	Cull Off 
//    	ZWrite Off
    	Fog { Mode off }
    	
//    	Blend SrcAlpha OneMinusSrcAlpha
//		Stencil
//		{
//			Ref[_StencilRef]
//			Comp[_StencilComp]
//			Pass[_StencilOp]
			// ReadMask[_StencilReadMask]
			// WriteMask[_StencilWriteMask]
			// Fail[_StencilOpFail] // do not change stencil value if stencil test fails
			// ZFail[_StencilOpZFail] // do not change stencil value if stencil test passes but depth test fails
//		}
    	
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #pragma multi_compile_instancing  // for it you should use inside Vert -> 
			#pragma fragmentoption ARB_precision_hint_fastest
            #include "UnityCG.cginc"
            #include "UnityStandardConfig.cginc"
            #include "UnityStandardUtils.cginc"
			// #include "UnityShadowLibrary.cginc"
			// #include "AutoLight.cginc"

//              struct appdata_t
//             {
//                 float4 vertex : POSITION;
//                 // The default UV channel used for texturing.
//                 float2 uv : TEXCOORD0;
// #if defined(LIGHTMAP_ON)
//                 // Reserved for Unity's light map UVs.
//                 float2 uv1 : TEXCOORD1;
// #endif
//                 // Used for smooth normal data (or UGUI scaling data).
//                 float4 uv2 : TEXCOORD2;
//                 // Used for UGUI scaling data.
//                 float2 uv3 : TEXCOORD3;
// #if defined(_VERTEX_COLORS)
//                 fixed4 color : COLOR0;
// #endif
//                 fixed3 normal : NORMAL;
// #if defined(_NORMAL_MAP)
//                 fixed4 tangent : TANGENT;
// #endif
//                 UNITY_VERTEX_INPUT_INSTANCE_ID  // for instancing
//             };

//             struct v2f 
//             {
//                 float4 position : SV_POSITION;
// #if defined(_BORDER_LIGHT)
//                 float4 uv : TEXCOORD0;
// #elif defined(_UV)
//                 float2 uv : TEXCOORD0;
// #endif
// #if defined(LIGHTMAP_ON)
//                 float2 lightMapUV : TEXCOORD1;
// #endif
// #if defined(_VERTEX_COLORS)
//                 fixed4 color : COLOR0;
// #endif
// #if defined(_SPHERICAL_HARMONICS)
//                 fixed3 ambient : COLOR1;
// #endif
// #if defined(_WORLD_POSITION)
// #if defined(_NEAR_PLANE_FADE)
//                 float4 worldPosition : TEXCOORD2;
// #else
//                 float3 worldPosition : TEXCOORD2;
// #endif
// #endif
// #if defined(_SCALE)
//                 float3 scale : TEXCOORD3;
// #endif
// #if defined(_NORMAL)
// #if defined(_TRIPLANAR_MAPPING)
//                 fixed3 worldNormal : COLOR3;
//                 fixed3 triplanarNormal : COLOR4;
//                 float3 triplanarPosition : TEXCOORD6;
// #elif defined(_NORMAL_MAP)
//                 fixed3 tangentX : COLOR3;
//                 fixed3 tangentY : COLOR4;
//                 fixed3 tangentZ : COLOR5;
// #else
//                 fixed3 worldNormal : COLOR3;
// #endif
// #endif
//                 UNITY_VERTEX_INPUT_INSTANCE_ID
//                 UNITY_VERTEX_OUTPUT_STEREO
//             };

            //  UNITY_INSTANCING_BUFFER_START(Props)
            // UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
            // UNITY_INSTANCING_BUFFER_END(Props)

//              v2f vert(appdata_t v)
//             {
//                 v2f o;
//                 UNITY_SETUP_INSTANCE_ID(v);
//                 UNITY_INITIALIZE_OUTPUT(v2f, o);
//                 UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
//                 UNITY_TRANSFER_INSTANCE_ID(v, o);
//
//                 float4 vertexPosition = v.vertex;
//
// #if defined(_WORLD_POSITION) || defined(_VERTEX_EXTRUSION)
//                 float3 worldVertexPosition = mul(unity_ObjectToWorld, vertexPosition).xyz;
// #endif
//
//                 fixed3 localNormal = v.normal;
//
// #if defined(_NORMAL) || defined(_VERTEX_EXTRUSION)
//                 fixed3 worldNormal = UnityObjectToWorldNormal(localNormal);
// #endif
//
// #if defined(_VERTEX_EXTRUSION)
// #if defined(_VERTEX_EXTRUSION_SMOOTH_NORMALS)
//                 worldVertexPosition += UnityObjectToWorldNormal(v.uv2 * o.scale) * _VertexExtrusionValue;
// #else
//                 worldVertexPosition += worldNormal * _VertexExtrusionValue;
// #endif
//                 vertexPosition = mul(unity_WorldToObject, float4(worldVertexPosition, 1.0));
// #endif
//
//                 o.position = UnityObjectToClipPos(vertexPosition);
//
// #if defined(_WORLD_POSITION)
//                 o.worldPosition.xyz = worldVertexPosition;
// #endif
//
// #if defined(_UV)
//                 o.uv = TRANSFORM_TEX(v.uv, _MainTex);
// #endif
//
// #if defined(LIGHTMAP_ON)
//                 o.lightMapUV.xy = v.uv1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
// #endif
//
// #if defined(_VERTEX_COLORS)
//                 o.color = v.color;
// #endif
//
// #if defined(_SPHERICAL_HARMONICS)
//                 o.ambient = ShadeSH9(float4(worldNormal, 1.0));
// #endif
//
// #if defined(_NORMAL)
// #if defined(_TRIPLANAR_MAPPING)
//                 o.worldNormal = worldNormal;
// #if defined(_LOCAL_SPACE_TRIPLANAR_MAPPING)
//                 o.triplanarNormal = localNormal;
//                 o.triplanarPosition = vertexPosition;
// #else
//                 o.triplanarNormal = worldNormal;
//                 o.triplanarPosition = o.worldPosition;
// #endif
// #elif defined(_NORMAL_MAP)
//                 fixed3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
//                 fixed tangentSign = v.tangent.w * unity_WorldTransformParams.w;
//                 fixed3 worldBitangent = cross(worldNormal, worldTangent) * tangentSign;
//                 o.tangentX = fixed3(worldTangent.x, worldBitangent.x, worldNormal.x);
//                 o.tangentY = fixed3(worldTangent.y, worldBitangent.y, worldNormal.y);
//                 o.tangentZ = fixed3(worldTangent.z, worldBitangent.z, worldNormal.z);
// #else
//                 o.worldNormal = worldNormal;
// #endif
// #endif
//
//                 return o;
//             }
            
            uniform float4 _Color1;
            uniform float4 _Color2;
            uniform float _Number;

            float4 frag(v2f_img i) : SV_Target
            {
            	UNITY_SETUP_INSTANCE_ID(i);
                float total = floor(i.uv.x * _Number) + floor(i.uv.y * _Number);
                return float4(lerp(_Color1.rgb, _Color2.rgb, step(fmod(total, 2.0), 0.5)), 1.0);
            }
            ENDCG
        }
    }
}