Shader "SPIN Play SDK/UVMap-RGB"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
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
                // note that RGB has image-like y orientation
                // and the C# code has (mostly) adjusted for this
                // by flipping the y direction of the uv coordinates it
                // uses to access textures.  -- BUT our uv maps are
                // built for video y orientation, so we have to flip
                // both before and after reading uv values out of them.
                float2 iuv = float2(i.uv.x, 1 - i.uv.y);
                float2 orig_uv = tex2D(_ToFormat, iuv).xy;
                float2 orig_uv_flip = float2(orig_uv.x, 1 - orig_uv.y);

                // sample the texture
                fixed4 col = tex2D(_MainTex, orig_uv_flip);

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
			}
			ENDCG
		}
	}
}
