Shader "SPIN Play SDK/UVMap-YUVNV12-Discont"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "black" {}
        _Y("Y", 2D) = "black" {}
        _UV("UV", 2D) = "gray" {}
        _ToFormat("ToFormat", 2D) = "black" {}
        _FromFormat("FromFormat", 2D) = "black" {}
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
			
            fixed4 frag(v2f i) : SV_Target
            {
                // first, perform the computation that the main shader did
                fixed4 orig_uv = tex2D(_ToFormat, i.uv);
                float y = (tex2D(_Y, orig_uv.rg).r - 0.0625)  *  1.1643;
                float2 uv = tex2D(_UV, orig_uv.rg).rg - 0.5;
                float u = uv.r;
                float v = uv.g;

                float r = clamp(y + 1.5958 * v, 0.0, 1.0);
                float g = clamp(y - 0.39173 * u - 0.81290 * v, 0.0, 1.0);
                float b = clamp(y + 2.017 * u, 0.0, 1.0);

                // now, more carefully emulate that computation,
                // being highly aware of the discontinuities in _ToFormat
                // note that we expect (require) that the images are set
                // so that sampling is done using bilinear interpolation.
                // much of the algorithm below makes use of what one would
                // normally do with point sampling, but we get nearly the same
                // thing by always sampling from the exact center of each pixel

                // center of pixel that is to the upper left of i.uv in _ToFormat
                float2 ul = float2((floor(i.uv.x * _ToFormat_TexelSize.z - 0.5) + 0.5) * _ToFormat_TexelSize.x, (floor(i.uv.y * _ToFormat_TexelSize.w - 0.5) + 0.5) * _ToFormat_TexelSize.y);

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
                    ur_uv = ul_uv*2 - tex2D(_ToFormat, ul - float2(_ToFormat_TexelSize.x, 0)).xy;
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
                    lr_uv = ul_uv*2 - tex2D(_ToFormat, ul - float2(_ToFormat_TexelSize.x, _ToFormat_TexelSize.y)).xy;
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
                    ll_uv = ul_uv*2 - tex2D(_ToFormat, ul - float2(0, _ToFormat_TexelSize.y)).xy;
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
                float xwt = (i.uv.x - ul.x) * _ToFormat_TexelSize.z;
                float ywt = (i.uv.y - ul.y) * _ToFormat_TexelSize.w;
                float2 interp_uv = ul_uv * (1 - ywt) * (1 - xwt) + ur_uv * (1 - ywt) * xwt + ll_uv * ywt * (1 - xwt) + lr_uv * ywt * xwt;

                // if this interpolated uv doesn't move very from from
                // the simpler computation, then use the simpler computation
                float uv_move = length(interp_uv - orig_uv);
                if (uv_move <= .0001)
                    return fixed4(r, g, b, 1);

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
                            float xdist = abs(i.uv.x - orig_uv.x);
                            if (xdist > .5)
                                xdist -= 1;
                            float dist = length(float2(xdist, orig_uv.y - i.uv.y));
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
                float4 ul_yuv = float4(tex2D(_Y, ul).r, tex2D(_UV, ul).rg, 0);
                float4 ur_yuv = float4(tex2D(_Y, ur).r, tex2D(_UV, ur).rg, 0);
                float4 ll_yuv = float4(tex2D(_Y, ll).r, tex2D(_UV, ll).rg, 0);
                float4 lr_yuv = float4(tex2D(_Y, lr).r, tex2D(_UV, lr).rg, 0);

                // where each of the ul_yuv, ur_yuv, etc... came from
                ul_uv = tex2D(_FromFormat, ul).xy;
                ur_uv = tex2D(_FromFormat, ur).xy;
                ll_uv = tex2D(_FromFormat, ll).xy;
                lr_uv = tex2D(_FromFormat, lr).xy;

                float ul_wt = 1.0 / length(ul_uv - i.uv);
                float ur_wt = 1.0 / length(ur_uv - i.uv);
                float ll_wt = 1.0 / length(ll_uv - i.uv);
                float lr_wt = 1.0 / length(lr_uv - i.uv);
                float wt = ul_wt + ur_wt + ll_wt + lr_wt;
                ul_wt /= wt;
                ur_wt /= wt;
                ll_wt /= wt;
                lr_wt /= wt;
                float4 interp_yuv = ul_yuv * ul_wt + ur_yuv * ur_wt + ll_yuv * ll_wt + lr_yuv * lr_wt;


                // convert the interpolated yuv to rgb
                y = (interp_yuv.r - 0.0625)  *  1.1643;
                uv = interp_yuv.gb - 0.5;
                u = uv.r;
                v = uv.g;
                
                float intr = clamp(y + 1.5958 * v, 0.0, 1.0);
                float intg = clamp(y - 0.39173 * u - 0.81290 * v, 0.0, 1.0);
                float intb = clamp(y + 2.017 * u, 0.0, 1.0);

                // finally, if the carefully computed color is fairly close to the hardware linearly interpolated one
                // then we use the hardware interpolated one.  The "careful" one won't make big errors in the color
                // it computes, but won't necessarily get exactly what the hardware one gets, and since this shader
                // runs on more pixels than just the ones that need correcting, we don't want to add another fake
                // boundary to the edge of where this shader is running.  We only want to change pixels that are
                // way off.
                if ((interp_uv.x != orig_uv.x) && (abs(intr - r) > .1 || abs(intg - g) > .1 || abs(intb - b) > .1)) {
                    //return fixed4(1, 0, 0, 1);
                    r = intr;
                    g = intg;
                    b = intb;
                }

                fixed4 col = fixed4(r, g, b, 1.0);

                // apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}
}
