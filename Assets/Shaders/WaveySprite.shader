Shader "Custom/WaveySprite"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Amplitude ("Amplitude", Float) = 0.05
        _Frequency ("Frequency", Float) = 10
        _Speed ("Speed", Float) = 1
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        Lighting Off
        ZWrite Off

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
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _Amplitude;
            float _Frequency;
            float _Speed;

            v2f vert(appdata v)
            {
                v2f o;
                float2 uv = TRANSFORM_TEX(v.uv, _MainTex);
                // wave along X based on V coordinate and time
                float wave = sin((uv.y * _Frequency) + (_Time.y * _Speed)) * _Amplitude;
                float3 displaced = v.vertex.xyz + float3(wave, 0, 0);
                o.pos = UnityObjectToClipPos(float4(displaced, 1));
                o.uv = uv;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);
                return tex * i.color;
            }
            ENDCG
        }
    }
    FallBack "Sprites/Default"
}
