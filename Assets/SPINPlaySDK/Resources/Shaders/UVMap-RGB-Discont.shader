Shader "SPIN Play SDK/UVMap-RGB-Discont"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
        _ToFormat("ToFormat", 2D) = "black" {}
        _FromFormat("FromFormat", 2D) = "black" {}
        _SrcTexInfo("Source Texture dimensions", Vector) = (1,1,1,1)
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
            sampler2D _FromFormat;
            float4 _MainTex_TexelSize;
            float4 _ToFormat_TexelSize;
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

                // center of pixel that is to the upper left of i.uv in _ToFormat
                float2 ul = float2((floor(iuv.x * _ToFormat_TexelSize.z - 0.5) + 0.5) * _ToFormat_TexelSize.x, (floor(iuv.y * _ToFormat_TexelSize.w - 0.5) + 0.5) * _ToFormat_TexelSize.y);

                // the other three pixel centers surrounding i.uv, note that on the bottom
                // and right edges of _ToFormat we clamp this computation
                float2 ur = ul + float2(_ToFormat_TexelSize.x, 0);
                float2 ll = ul + float2(0, _ToFormat_TexelSize.y);
                float2 lr = ul + float2(_ToFormat_TexelSize.x, _ToFormat_TexelSize.y);

                // look up each each center's uv (where that pixel comes from in the projected (i.e. diamondplane) video frame
                float2 ul_uv = tex2D(_ToFormat, ul).xy;
                float2 ur_uv = tex2D(_ToFormat, ur).xy;
                float2 ll_uv = tex2D(_ToFormat, ll).xy;
                float2 lr_uv = tex2D(_ToFormat, lr).xy;

                // now, to deal with cases where the _toFormat texture has a discontinuity running through this block we are looking
                // at we extrapolate values that seem too far from ul_uv.  If the extrapolated value is also too far then we
                // throw out that value by duplicaing ul_uv's value
                // uncomment the various "returns" to see which pixels need extrapolation logic in order to
                // compute a better uv value to use when sampling from the projected format
                const float tol = .003;
                if ((abs(ur_uv.x - ul_uv.x) > tol) || (abs(ur_uv.y - ul_uv.y) > tol)) {
                    //return fixed4(0, 0, 1, 0);
                    ur_uv = ul_uv * 2 - tex2D(_ToFormat, ul - float2(_ToFormat_TexelSize.x, 0)).xy;
                    if ((abs(ur_uv.x - ul_uv.x) > tol) || (abs(ur_uv.y - ul_uv.y) > tol)) {
                        //return fixed4(1, 0, 0, 0);
                        ur_uv = ul_uv;
                    }
                    else
                    {
                        //return fixed4(0, 1, 0, 1);
                    }
                }
                if ((abs(lr_uv.x - ul_uv.x) > tol) || (abs(lr_uv.y - ul_uv.y) > tol)) {
                    //return fixed4(0, 0, 1, 0);
                    lr_uv = ul_uv * 2 - tex2D(_ToFormat, ul - float2(_ToFormat_TexelSize.x, _ToFormat_TexelSize.y)).xy;
                    if ((abs(lr_uv.x - ul_uv.x) > tol) || (abs(lr_uv.y - ul_uv.y) > tol)) {
                        //return fixed4(1, 0, 0, 0);
                        lr_uv = ul_uv;
                    }
                    else
                    {
                        //return fixed4(0, 1, 0, 1);
                    }
                }
                if ((abs(ll_uv.x - ul_uv.x) > tol) || (abs(ll_uv.y - ul_uv.y) > tol)) {
                    //return fixed4(0, 0, 1, 0);
                    ll_uv = ul_uv * 2 - tex2D(_ToFormat, ul - float2(0, _ToFormat_TexelSize.y)).xy;
                    if ((abs(ll_uv.x - ul_uv.x) > tol) || (abs(ll_uv.y - ul_uv.y) > tol)) {
                        //return fixed4(1, 0, 0, 0);
                        ll_uv = ul_uv;
                    }
                    else
                    {
                        //return fixed4(0, 1, 0, 1);
                    }
                }

                // now linearly interpolate the ul, ur, ll, and lr uv values
                float xwt = (iuv.x - ul.x) * _ToFormat_TexelSize.z;
                float ywt = (iuv.y - ul.y) * _ToFormat_TexelSize.w;
                float2 interp_uv = ul_uv * (1 - ywt) * (1 - xwt) + ur_uv * (1 - ywt) * xwt + ll_uv * ywt * (1 - xwt) + lr_uv * ywt * xwt;

                // if this interpolated uv doesn't move very from from
                // the simpler computation, then use the simpler computation
                float uv_move = length(interp_uv - orig_uv);
                if (uv_move <= .0001) {
                    UNITY_APPLY_FOG(i.fogCoord, col);
                    return col;
                }

                // below here are pixels we are likly to compute a different color
                // than the simple bilinear interpolation produces.  interp_uv is
                // the uv we want to use to look up a yuv color in the projected frame
                // but we also have similar discontinuty problems to deal with there...


                // uncomment to get a sense of how far the uv moves for each pixel
                //return fixed4(uv_move, 0, 0, 0);

                // start by searching for the pixel in the neighborhood of interp_uv whose original uv
                // is closest to i.uv - but remember that the u direction wraps, so .999 is very close to .001

                float2 target_uv;
                float min_dist = 2;
                {
                    ul_uv = float2((floor(interp_uv.x * _MainTex_TexelSize.z - 0.5) + 0.5) * _MainTex_TexelSize.x, (floor(interp_uv.y * _MainTex_TexelSize.w - 0.5) + 0.5) * _MainTex_TexelSize.y);

                    for (int y = 0; y < 2; y++) {
                        for (int x = 0; x < 2; x++) {
                            float2 prop_uv = ul_uv + float2(_MainTex_TexelSize.x*x, _MainTex_TexelSize.y*y);
                            float2 orig_uv = tex2D(_FromFormat, prop_uv);
                            float xdist = abs(iuv.x - orig_uv.x);
                            if (xdist > .5)
                                xdist -= 1;
                            float dist = length(float2(xdist, orig_uv.y - iuv.y));
                            if (dist < min_dist) {
                                target_uv = prop_uv;
                                min_dist = dist;
                            }
                        }
                    }
                }

                ul = target_uv;
                ur = ul + float2(_MainTex_TexelSize.x, 0);
                ll = ul + float2(0, _MainTex_TexelSize.y);
                lr = ul + float2(_MainTex_TexelSize.x, _MainTex_TexelSize.y);

                // read the yuv values of the 4 pixels we are interested in.  But in this case the
                // weighting for each one is based on how close its original uv was to the target
                // uv we are trying to paint.
                float4 ul_col = tex2D(_MainTex, float2(ul.x, 1 - ul.y));
                float4 ur_col = tex2D(_MainTex, float2(ur.x, 1 - ur.y));
                float4 ll_col = tex2D(_MainTex, float2(ll.x, 1 - ll.y));
                float4 lr_col = tex2D(_MainTex, float2(lr.x, 1 - lr.y));

                // where each of the ul_yuv, ur_yuv, etc... came from
                ul_uv = tex2D(_FromFormat, ul).xy;
                ur_uv = tex2D(_FromFormat, ur).xy;
                ll_uv = tex2D(_FromFormat, ll).xy;
                lr_uv = tex2D(_FromFormat, lr).xy;

                float ul_wt = 1.0 / length(ul_uv - iuv);
                float ur_wt = 1.0 / length(ur_uv - iuv);
                float ll_wt = 1.0 / length(ll_uv - iuv);
                float lr_wt = 1.0 / length(lr_uv - iuv);
                float wt = ul_wt + ur_wt + ll_wt + lr_wt;
                ul_wt /= wt;
                ur_wt /= wt;
                ll_wt /= wt;
                lr_wt /= wt;
                float4 interp_col = ul_col * ul_wt + ur_col * ur_wt + ll_col * ll_wt + lr_col * lr_wt;

                if ((interp_uv.x != orig_uv.x) && length(interp_col - col) > .1)
                    col = interp_col;

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
			}
			ENDCG
		}
	}
}
