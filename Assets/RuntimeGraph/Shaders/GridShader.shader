Shader "RuntimeGraph/GridShader"
{
    Properties
    {
        _GridColor ("Grid Color", Color) = (1,1,1,0.12)
        _DotColor ("Dot Color", Color) = (1,1,1,0.5)
        _GridSpacing ("Grid Spacing", Float) = 5.0
        _DotSpacing ("Dot Spacing", Float) = 0.2
        _LineWidth ("Line Width", Float) = 1
        _DotSize ("Dot Size", Float) = 1.0
        _ZoomLevel ("Zoom Level", Float) = 1.0
        _CameraPosition ("Camera Position", Vector) = (0,0,0,0)
        _CameraSize ("Camera Size", Float) = 10.0
    }
    
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest Always
        
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
                float3 worldPos : TEXCOORD1;
            };
            
            fixed4 _GridColor;
            fixed4 _DotColor;
            float _GridSpacing;
            float _DotSpacing;
            float _LineWidth;
            float _DotSize;
            float _ZoomLevel;
            float4 _CameraPosition;
            float _CameraSize;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.uv = v.uv;
                return o;
            }
            
            // Function to calculate grid line intensity
            float gridLine(float coord, float spacing, float lineWidth)
            {
                float derivative = fwidth(coord / spacing);
                // Add epsilon to prevent division by zero and reduce precision issues
                derivative = max(derivative, 0.0001);
                
                float grid = abs(frac(coord / spacing - 0.5) - 0.5) / derivative;
                
                // Use smoothstep for better anti-aliasing and reduce flickering
                return 1.0 - smoothstep(0.0, lineWidth, grid);
            }
            
            // Function to calculate dot intensity
            float gridDot(float2 coord, float spacing, float dotSize)
            {
                float2 grid = abs(frac(coord / spacing - 0.5) - 0.5);
                float2 gridDerivs = fwidth(coord / spacing);
                
                // Distance to nearest grid intersection
                float2 gridDist = grid / gridDerivs;
                float dist = length(gridDist);
                
                // Create circular dots with constant world-space size
                float dotRadius = dotSize; // Constant world-space size
                return 1.0 - smoothstep(dotRadius * 0.5, dotRadius, dist);
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float2 worldPos = i.worldPos.xy;
                
                // Calculate grid lines
                float gridX = gridLine(worldPos.x, _GridSpacing, _LineWidth);
                float gridY = gridLine(worldPos.y, _GridSpacing, _LineWidth);
                float gridIntensity = max(gridX, gridY);
                
                // Calculate dynamic dot spacing to align with grid lines
                // Use grid spacing divided by zoom-based subdivision to ensure dots align with grid intersections
                float subdivisionFactor = max(1.0, _ZoomLevel / 2.0); // More subdivisions at higher zoom
                float dynamicDotSpacing = _GridSpacing / subdivisionFactor;
                
                // Calculate dots with zoom-independent size
                float dotIntensity = gridDot(worldPos, dynamicDotSpacing, _DotSize);
                
                // Combine grid lines and dots
                fixed4 gridResult = _GridColor * gridIntensity;
                fixed4 dotResult = _DotColor * dotIntensity;
                
                // Blend dots over grid lines
                fixed4 finalColor = gridResult + dotResult * (1.0 - gridResult.a);
                
                return finalColor;
            }
            ENDCG
        }
    }
    FallBack "Sprites/Default"
}