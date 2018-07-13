Shader "SPIN Play SDK/UVMap-YUVNV12"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "black" {}
        _Y("Y", 2D) = "black" {}
        _UV("UV", 2D) = "gray" {}
        _ToFormat("ToFormat", 2D) = "black" {}
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
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
			};

            sampler2D _MainTex;
            sampler2D _Y;
            sampler2D _UV;
            sampler2D _ToFormat;
            float4 _MainTex_ST;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
                fixed4 orig_uv = tex2D(_ToFormat, i.uv);

                float y = (tex2D(_Y, orig_uv.rg).r - 0.0625)  *  1.1643;
                float2 uv = tex2D(_UV, orig_uv.rg).rg - 0.5;
                float u = uv.r;
                float v = uv.g;

                float r = clamp(y + 1.5958 * v, 0.0, 1.0);
                float g = clamp(y - 0.39173 * u - 0.81290 * v, 0.0, 1.0);
                float b = clamp(y + 2.017 * u, 0.0, 1.0);

                fixed4 col = fixed4(r, g, b, 1.0);

                // apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}
}
