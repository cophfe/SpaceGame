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
                float3 materialInformation : COLOR; //material information x: material index 1. y: material index 2. z: material interpolation
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
				float3 materialInformation : COLOR;
            };

            float4 _Scale;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv.xy = (v.vertex.xy + 0.5) * _Scale;
				o.materialInformation = v.materialInformation;
                return o;
            }

			UNITY_DECLARE_TEX2DARRAY(_MaterialArray);

			fixed4 frag(v2f i) : SV_Target
			{
				// linearly interpolate between two material textures
				fixed4 col = lerp(UNITY_SAMPLE_TEX2DARRAY(_MaterialArray, float3(i.uv.xy, i.materialInformation.x))
								, UNITY_SAMPLE_TEX2DARRAY(_MaterialArray, float3(i.uv.xy, i.materialInformation.y))
								, i.materialInformation.z);
                return col;
            }
            ENDCG
        }
    }
}
