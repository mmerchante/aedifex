Shader "Pitch/ScreenOverlay"
{
	Properties
	{
		_OverlayColor ("Overlay Color", Color) = (1.0, 1.0, 1.0, 1.0)
	}

	SubShader
	{
		Tags { "RenderType"="Transparent" }
		Cull Off ZWrite Off ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				return o;
			}

            float4 _OverlayColor;

			fixed4 frag (v2f i) : SV_Target
			{
				return _OverlayColor;
			}
			ENDCG
		}
	}
}
