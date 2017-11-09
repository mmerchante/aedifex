Shader "Pitch/BaseSurface" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
        _DissolveTex ("Dissolve tex", 2D) = "white" {}
    	_Dissolve ("Dissolve", Range(0,1)) = 0.0
        _DissolveGlow ("Glow", Range(0,15)) = 0.0
    	_DissolveGlowSize ("GlowSize", Range(0,1)) = 0.0
        _DissolveColor ("Dissolve Color", Color) = (1,1,1,1)
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
        _Cutout ("Cutout", Range(0,1)) = 0.0
        _DissolveScale ("Dissolve Scale", Float) = 1.0
	}
	SubShader {
		Tags { "RenderType"="Transparent" "Queue"="Transparent" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Standard alphatest:_Cutout
		#pragma target 5.0

		sampler2D _MainTex;
        sampler2D _DissolveTex;

		struct Input {
			float2 uv_MainTex;
            float2 uv_DissolveTex;
            float3 worldPos;
		};

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;
        float _Dissolve;
        float _DissolveGlow;
        float4 _DissolveColor;
		float _DissolveGlowSize;
		float _DissolveScale;

		UNITY_INSTANCING_CBUFFER_START(Props)
		UNITY_INSTANCING_CBUFFER_END

		void surf (Input IN, inout SurfaceOutputStandard o) {
			float4 c = tex2D (_MainTex, IN.worldPos.xz + IN.worldPos.yx) * _Color;
			o.Albedo = c.rgb;
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;

            float2 dissolveUV = (IN.worldPos.xz + IN.worldPos.yx) * .1 * _DissolveScale;

            float a = tex2D (_DissolveTex, dissolveUV * 4.0 + _Time.xx).r;
            a = tex2D (_DissolveTex, dissolveUV * 2.0 + a * .5 + _Time.xx * 4.0).r;
			o.Alpha = smoothstep(_Dissolve - .15, _Dissolve, a);

			float glow = 1.0 - smoothstep(_Dissolve - .15, _Dissolve+ _DissolveGlowSize, a);
            o.Emission = _DissolveColor * _DissolveGlow * glow * 2.0;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
