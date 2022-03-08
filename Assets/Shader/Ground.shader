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
										//this is probably interpolated, which will break everything.
				half4 colour : COLOR; //material information x: material index 1. y: material index 2. z: material interpolation 
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				half4 colour : TEXCOORD1;
			};

			float _Scale;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = mul(unity_ObjectToWorld, v.vertex * _Scale).xy;
				o.colour = v.colour;
				//o.uv.xy = (v.vertex.xy + 0.5) * _Scale;
				return o;
			}

			UNITY_DECLARE_TEX2DARRAY(_MaterialArray);

			fixed4 frag(v2f i) : SV_Target
			{
				// linearly interpolate between two material textures
				fixed4 col =  lerp(UNITY_SAMPLE_TEX2DARRAY(_MaterialArray, half3(i.uv.xy, i.colour.x * 256))
								, UNITY_SAMPLE_TEX2DARRAY(_MaterialArray, half3(i.uv.xy, i.colour.y * 256))
								, i.colour.z);

				//fixed4 col = lerp(i.colour.x * 50
				//				, half4(1,0,0,1)
				//				, i.colour.z);
				//fixed4 col = i.colour;
				return col;
			}
			ENDCG
		}
	}
}
