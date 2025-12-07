Shader "Custom/Ghost"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (0.5, 0.8, 1, 0.5)
        _RimPower ("Rim Power", Range(0.5, 8)) = 3
        _RimColor ("Rim Color", Color) = (1, 1, 1, 1)
        _PulseSpeed ("Pulse Speed", Range(0, 5)) = 1
    }
    
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 viewDir : TEXCOORD1;
                float3 normal : TEXCOORD2;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _RimPower;
            float4 _RimColor;
            float _PulseSpeed;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = normalize(WorldSpaceViewDir(v.vertex));
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);
                
                // Rim lighting
                float rim = 1.0 - saturate(dot(i.viewDir, i.normal));
                rim = pow(rim, _RimPower);
                
                // Pulse effect
                float pulse = 0.8 + 0.2 * sin(_Time.y * _PulseSpeed);
                
                fixed4 col = tex * _Color;
                col.rgb += _RimColor.rgb * rim;
                col.a *= pulse;
                
                return col;
            }
            ENDCG
        }
    }
    FallBack "Transparent/Diffuse"
}
