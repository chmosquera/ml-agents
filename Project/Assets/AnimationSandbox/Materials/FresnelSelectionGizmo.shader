Shader "Teleportal/Unlit/FresnelSelectionGizmo"
{
    Properties
    {
        _FresnelColor("Fresnel Color", Color) = (1,1,1,1)
        _FresnelBias("Fresnel Bias", Float) = 0
        _FresnelScale("Fresnel Scale", Float) = 1
        _FresnelPower("Fresnel Power", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True"}
		Blend SrcAlpha OneMinusSrcAlpha
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#pragma target 2.0

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float fresnel : TEXCOORD1;
            };

            fixed4 _FresnelColor;
            fixed _FresnelBias;
            fixed _FresnelScale;
            fixed _FresnelPower;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                float3 i = normalize(ObjSpaceViewDir(v.vertex));
                o.fresnel = _FresnelBias + _FresnelScale * pow(1 + dot(i, v.normal), _FresnelPower);

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col;
				col.rgb = _FresnelColor;
                fixed time = (sin(_Time.z) + 1.0) / 2.0;
                col.a = pow(1.0 - i.fresnel, 2.5 - time);
                return col;
            }
            ENDCG
        }
    }
}
