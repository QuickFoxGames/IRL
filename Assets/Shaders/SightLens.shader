Shader "Custom/SightLens"
{
    Properties
    {
        _Tex1 ("Texture1", 2D) = "white" {}
        [HDR]_Color1 ("Color1", Color) = (1,0,0,1)
        [Space]
        _ToggleTex2 ("ToggleTex2", int) = 1
        _Tex2 ("Texture2", 2D) = "white" {}
        [HDR]_Color2 ("Color2", Color) = (0,1,0,1)
        [Space]
        _ToggleTex3 ("ToggleTex3", int) = 1
        _Tex3 ("Texture3", 2D) = "white" {}
        [HDR]_Color3 ("Color3", Color) = (0,0,1,1)
        [Space]
        _Distance ("Distance", Float) = 100
        _Scale ("Scale", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue" = "Transparent" }
        LOD 100

        Pass
        {
            ZTest off
            Blend SrcAlpha OneMinusSrcAlpha

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

            sampler2D _Tex1;
            sampler2D _Tex2;
            sampler2D _Tex3;
            int _ToggleTex2;
            int _ToggleTex3;
            float _Distance;
            float _Scale;
            float4 _Color1;
            float4 _Color2;
            float4 _Color3;

            v2f vert (appdata v)
            {
                v2f o;

                float3 lensOrigin = UnityObjectToViewPos(float3(0, 0, 0));
                float3 p0 = UnityObjectToViewPos(float3(0, 0, _Distance));
                float3 n = UnityObjectToViewPos(float3(0, 0, 1)) - lensOrigin;
                float3 uDir = UnityObjectToViewPos(float3(1, 0, 0)) - lensOrigin;
                float3 vDir = UnityObjectToViewPos(float3(0, 1, 0)) - lensOrigin;
                float3 vert = UnityObjectToViewPos(v.vertex);

                float a = dot(p0, n) / dot(vert, n);
                float3 vertPrime = a * vert;

                o.vertex = UnityObjectToClipPos(v.vertex);

                o.uv = float2(dot(vertPrime - p0, uDir), dot(vertPrime - p0, vDir));
                o.uv = o.uv / (0.001 * _Scale * a);
                o.uv += float2(0.5, 0.5);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 tex1 = tex2D(_Tex1, i.uv);
                fixed4 tex2 = tex2D(_Tex2, i.uv);
                fixed4 tex3 = tex2D(_Tex3, i.uv);

                fixed4 col = tex1 * _Color1;
                col.a = tex1.a;

                if (_ToggleTex2 == 1)
                {
                    col += tex2 * _Color2;
                    col.a += tex2.a;
                }
                if (_ToggleTex3 == 1) 
                {
                    col += tex3 * _Color3;
                    col.a += tex3.a;
                }

                if (i.uv.x < 0 || i.uv.x > 1 || i.uv.y < 0 || i.uv.y > 1) clip(-1);

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
