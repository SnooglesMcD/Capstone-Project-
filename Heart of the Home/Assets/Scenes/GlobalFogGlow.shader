Shader "Custom/GlobalFogGlow"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _GlowColor ("Glow Color", Color) = (0.8, 0.6, 1.0, 0.3)
        _GlowIntensity ("Glow Intensity", Range(0, 3)) = 1.0
        _GlowFalloff ("Glow Falloff", Range(0.1, 10)) = 2.0
        _HeightFog ("Height Fog", Range(0, 2)) = 0.5
        _NoiseSpeed ("Noise Speed", Range(0, 1)) = 0.1
        _NoiseAmount ("Noise Amount", Range(0, 0.5)) = 0.1
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent+100" 
            "IgnoreProjector"="True"
            "PreviewType"="Plane"
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        
        Pass
        {
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
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
                float3 viewDir : TEXCOORD2;
                UNITY_FOG_COORDS(3)
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _GlowColor;
            float _GlowIntensity;
            float _GlowFalloff;
            float _HeightFog;
            float _NoiseSpeed;
            float _NoiseAmount;
            
            // Simple noise function
            float noise(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.viewDir = normalize(UnityWorldSpaceViewDir(o.worldPos));
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Base texture
                fixed4 col = tex2D(_MainTex, i.uv);
                
                // Center-based glow
                float2 center = float2(0.5, 0.5);
                float dist = distance(i.uv, center);
                float glow = 1.0 - smoothstep(0.0, _GlowFalloff, dist * 2.0);
                glow = pow(glow, 2.0) * _GlowIntensity;
                
                // Height-based fog (thicker near floor)
                float heightFog = 1.0 - saturate(i.worldPos.y * _HeightFog);
                
                // Animated noise for subtle movement
                float noiseValue = noise(i.uv + _Time.y * _NoiseSpeed) * _NoiseAmount;
                
                // Combine effects
                float finalGlow = glow * heightFog * (1.0 + noiseValue);
                
                // Create glow color
                fixed4 glowColor = _GlowColor * finalGlow;
                
                // Blend with original
                col.rgb = col.rgb + glowColor.rgb;
                col.a = max(col.a, glowColor.a);
                
                // Apply Unity fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                
                return col;
            }
            ENDCG
        }
    }
    
    Fallback "Unlit/Transparent"
}