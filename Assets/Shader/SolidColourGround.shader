Shader "Unlit/SolidColourGround"
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
										//this is probably interpolated, which will break everything.
				half3 colour : COLOR; //material information x: material index 1. y: material index 2. z: material interpolation 
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				half3 colour : COLOR;
			};

			float _Scale;
			UNITY_DECLARE_TEX2DARRAY(_MaterialArray);

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = mul(unity_ObjectToWorld, v.vertex * _Scale).xy; // this will look messed up in 3D, but 2d is fine
				o.colour = lerp(UNITY_SAMPLE_TEX2DARRAY_LOD(_MaterialArray, half3(o.uv.xy, v.colour.x * 255), 0)
					, UNITY_SAMPLE_TEX2DARRAY_LOD(_MaterialArray, half3(o.uv.xy, v.colour.y * 255), 0)
					, v.colour.z);
				
				return o;
			}


			fixed4 frag(v2f i) : SV_Target
			{
				return fixed4(i.colour.xyz, 1);
			}
			ENDCG
		}
	}
}
