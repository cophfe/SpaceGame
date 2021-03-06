Shader "Unlit/Ground"
{
	Properties
	{
		_MaterialArray("Materials", 2DArray) = "" {}
		_Scale("Scale", Float) = 1.0
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma require 2darray

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				//material information x: material index 1. y: material index 2. z: material interpolation 
				half3 colour : COLOR; 
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				nointerpolation half3 colour : COLOR;
			};

			float _Scale;
			UNITY_DECLARE_TEX2DARRAY(_MaterialArray);

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = mul(unity_ObjectToWorld, v.vertex * _Scale).xy; // this will look messed up in 3D, but 2d is fine
				o.colour = v.colour.xyz;
				
				return o;
			}


			fixed4 frag(v2f i) : SV_Target
			{
				// linearly interpolate between two material textures
				fixed4 col =  lerp(UNITY_SAMPLE_TEX2DARRAY(_MaterialArray, half3(i.uv.xy, i.colour.x * 255))
								, UNITY_SAMPLE_TEX2DARRAY(_MaterialArray, half3(i.uv.xy, i.colour.y * 255))
								, i.colour.z);
				return col;
			}
			ENDCG
		}
	}
}
