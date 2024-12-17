Shader "Custom/SightLens"
{
    Properties
    {
        _Tex1 ("Texture1", 2D) = "white" {}
        [HDR]_Color1 ("Color1", Color) = (1,0,0,1)
        _Scale1 ("Scale1", Float) = 1
        [Space]
        _ToggleTex2 ("ToggleTex2", int) = 1
        _Tex2 ("Texture2", 2D) = "white" {}
        [HDR]_Color2 ("Color2", Color) = (0,1,0,1)
        _Scale2 ("Scale2", Float) = 1
        [Space]
        _ToggleTex3 ("ToggleTex3", int) = 1
        _Tex3 ("Texture3", 2D) = "white" {}
        [HDR]_Color3 ("Color3", Color) = (0,0,1,1)
        _Scale3 ("Scale3", Float) = 1
        [Space]
        _Distance ("Distance", Float) = 100
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
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv1 : TEXCOORD0;
                float2 uv2 : TEXCOORD1;
                float2 uv3 : TEXCOORD2;
                UNITY_FOG_COORDS(3)
                float4 vertex : SV_POSITION;
            };

            sampler2D _Tex1;
            sampler2D _Tex2;
            sampler2D _Tex3;
            int _ToggleTex2;
            int _ToggleTex3;
            float _Distance;
            float _Scale1;
            float _Scale2;
            float _Scale3;
            float4 _Color1;
            float4 _Color2;
            float4 _Color3;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);

                // Calculate base UV coordinates
                float3 lensOrigin = UnityObjectToViewPos(float3(0, 0, 0));
                float3 p0 = UnityObjectToViewPos(float3(0, 0, _Distance));
                float3 n = UnityObjectToViewPos(float3(0, 0, 1)) - lensOrigin;
                float3 uDir = UnityObjectToViewPos(float3(1, 0, 0)) - lensOrigin;
                float3 vDir = UnityObjectToViewPos(float3(0, 1, 0)) - lensOrigin;
                float3 vert = UnityObjectToViewPos(v.vertex);

                float a = dot(p0, n) / dot(vert, n);
                float3 vertPrime = a * vert;

                float2 uv = float2(dot(vertPrime - p0, uDir), dot(vertPrime - p0, vDir));
                uv = uv / (0.001 * a) + float2(0.5, 0.5);

                // Apply individual scaling from the center
                o.uv1 = (uv - 0.5) / _Scale1 + 0.5; // Scale relative to center
                o.uv2 = (uv - 0.5) / _Scale2 + 0.5; // Scale relative to center
                o.uv3 = (uv - 0.5) / _Scale3 + 0.5; // Scale relative to center

                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Sample textures with scaled UVs
                fixed4 tex1 = tex2D(_Tex1, i.uv1) * _Color1;
                fixed4 tex2 = tex2D(_Tex2, i.uv2) * _Color2;
                fixed4 tex3 = tex2D(_Tex3, i.uv3) * _Color3;

                // Start with Texture 1
                fixed4 col = tex1;

                // Add Texture 2 if enabled
                if (_ToggleTex2 == 1)
                {
                    col.rgb = lerp(col.rgb, tex2.rgb, tex2.a); // Blend tex2 on top
                    col.a = max(col.a, tex2.a);               // Update alpha
                }

                // Add Texture 3 if enabled
                if (_ToggleTex3 == 1)
                {
                    col.rgb = lerp(col.rgb, tex3.rgb, tex3.a); // Blend tex3 on top
                    col.a = max(col.a, tex3.a);               // Update alpha
                }

                // Clip out-of-bounds UVs
                if (i.uv1.x < 0 || i.uv1.x > 1 || i.uv1.y < 0 || i.uv1.y > 1) clip(-1);

                // Apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
