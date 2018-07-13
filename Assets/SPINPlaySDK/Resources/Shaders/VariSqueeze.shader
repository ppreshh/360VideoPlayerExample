Shader "SPIN Play SDK/VariSqueeze" {
	 // Revised VariSqueeze shader to be based on default Unlit Texture Unity Shader rather than AVPro special shader
	 // Handle UV offsets, try to counter 16-bit floats on S8
	 // Leaving test code in (commented out) for now   since it's proved useful diagnosing issues
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	 	_UVXTex("UVXTex", 2D) = "black" {}
		_UVYTex("UVYTex", 2D) = "black" {}

		// all of following are just for testing

			_Left("Left", Float) = 0 //
			_Right("Right", Float) = 0 //
			_Top("Top", Float) = 0 //
			_Bottom("Bottom", Float) = 0 //

            //_TestColor("Test Color",  COLOR) = (1,0,1,1)
			// uncomment these for testing
			// _LineColor("Line Color",  COLOR) = (1,0,1,1)
			// [Toggle] _ShowLines("Show Lines", Float) = 1.0

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

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			// sws experiments with different sampling
			// sampler2D_float _UVXTex;
			// sampler2D_float _UVYTex;
			// sampler2D _UVXTex;
			 // sampler2D  _UVYTex;
              sampler2D _UVXTex;
             sampler2D  _UVYTex;
             float4 _UVXTex_ST;
             float4 _UVYTex_ST;

			// all of following for testing
			float _Left;
			float _Right;
			float _Top;
			float _Bottom;


		// uncomment for testing
		//	float _ShowLines;
		//	fixed4 _LineColor;
          //   fixed4  _TestColor;




			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
                     o.uv = v.uv;
                     // as per Mike o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                     //UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
                 fixed4 col;
                 float4 xValue;
                 float4 yValue;
               // test  col = tex2D(_MainTex, TRANSFORM_TEX(i.uv, _MainTex));
                 if (i.uv.x < .5) // symmetrical so use finer resolution section
                 { // do regular processing
                     xValue = tex2D(_UVXTex, float2(i.uv.x, 0.0));
                 }
                 else
                 {
                       xValue = tex2D(_UVXTex, float2(1.0 - i.uv.x, 0.0));
                       xValue.r = 1.0 - xValue.r;
                 }

                 if (i.uv.y < .5)
                 { // do regular processing
                     yValue = tex2D(_UVXTex, float2(i.uv.y, 0.0));
                 }
                 else
                 {
                     yValue = tex2D(_UVXTex, float2(1.0 - i.uv.y, 0.0));
                     yValue.r = 1.0 - yValue.r;
                 }

                       col = tex2D(_MainTex, TRANSFORM_TEX(float2(xValue.r, yValue.r), _MainTex));

                     return col;
			}
/* standard
                 fixed4 frag(v2f i) : SV_Target
                 {


                  //   float4 xValue = tex2D(_UVXTex, float2(i.uv.x, 0.0));
                  //   float4 yValue = tex2D(_UVYTex, float2(i.uv.y, 0.0));
                     float4 xValue = tex2D(_UVXTex,TRANSFORM_TEX(float2(i.uv.x,0.0), _UVXTex));
                     float4 yValue = tex2D(_UVYTex, TRANSFORM_TEX(float2(i.uv.y, 0.0), _UVYTex));
                     fixed4 col = tex2D(_MainTex, TRANSFORM_TEX(float2(xValue.r, yValue.r), _MainTex));
                     return col;
                 }
 */
			ENDCG
		}
	}
}

		//fixed4 frag(v2f i) : SV_Target
		//{
		//    fixed4 col;
		//// test  col = tex2D(_MainTex, TRANSFORM_TEX(i.uv, _MainTex));
		//if (i.uv.x < .5)
		//{ // do regular processing
		//    float4 xValue = tex2D(_UVXTex, float2(i.uv.x, 0.0));
		//    float4 yValue = tex2D(_UVYTex, float2(i.uv.y, 0.0));
		//
		//
		//    col = tex2D(_MainTex, TRANSFORM_TEX(float2(xValue.r, yValue.r), _MainTex));
		//    /*
		//    if (i.uv.x == 0.0)
		//    {
		//    col = _TestColor;
		//    }
		//    */
		//}
		//else
		//{
		//    float4 xValue = tex2D(_UVXTex, float2(1.0 - i.uv.x, 0.0));
		//    float4 yValue = tex2D(_UVYTex, float2(i.uv.y, 0.0));
		//
		//    float modValue = 1.0 - xValue.r;
		//
		//    //col = tex2D(_MainTex, TRANSFORM_TEX(i.uv, _MainTex));
		//    col = tex2D(_MainTex, TRANSFORM_TEX(float2(modValue, yValue.r), _MainTex));
		//    //if (i.uv.x > .999)
		//    //{
		//    //    modValue = 0.0;
		//    //    col = tex2D(_MainTex, TRANSFORM_TEX(float2(modValue, yValue.r), _MainTex));
		//    // //   col = _TestColor;
		//    //}
		//}
		//return col;
		//}
