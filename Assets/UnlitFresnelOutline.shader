Shader "Unlit/FresnelOutline"
{
    Properties
    {
        _EdgeColor("Edge Color", Color) = (1,1,1,1)
        _EdgeWidth("Edge Width", Range(0,1)) = 0.5
    }
    SubShader
    {
        // Transparent Queue + RenderType so we can use alpha
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        // Blending so the inside is transparent
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
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float3 viewDir : TEXCOORD1;
            };

            float4 _EdgeColor;
            float _EdgeWidth;

            // Vertex function
            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);

                // Transform normal to world space
                float3 worldNormal = UnityObjectToWorldNormal(v.normal);

                // World position and camera direction
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 cameraPos = _WorldSpaceCameraPos;
                float3 viewDir = normalize(cameraPos - worldPos);

                o.worldNormal = normalize(worldNormal);
                o.viewDir = viewDir;
                return o;
            }

            // Fragment function
            fixed4 frag (v2f i) : SV_Target
            {
                // Dot of normal and view
                float NdotV = dot(normalize(i.worldNormal), normalize(i.viewDir));

                // Fresnel factor: 1 - |N·V|
                // The closer NdotV is to 0, the stronger the outline
                float fresnel = 1.0 - saturate(abs(NdotV));

                // EdgeWidth controls how thick the outline is
                // e.g. If EdgeWidth=0.2, we keep only the outer rim
                float alpha = smoothstep(_EdgeWidth, 1.0, fresnel);

                // Return the edge color with that alpha
                return float4(_EdgeColor.rgb, alpha * _EdgeColor.a);
            }
            ENDCG
        }
    }
    FallBack "Transparent/Cutout/VertexLit"
}
