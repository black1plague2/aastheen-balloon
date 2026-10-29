// Unlit vertex-color particle shader for the paint-splatter FX (PaintableSurface.CreateSplatterFx).
// Replaces Shader.Find("Particles/Standard Unlit"), a built-in shader that gets stripped from
// device builds — same class of bug as PaintStamp.shader. Lives under Resources/ so it's never
// stripped. No LightMode tag, so URP renders it via its SRPDefaultUnlit fallback pass.
Shader "PaintWall/ParticleUnlit"
{
    Properties
    {
        _Color ("Tint Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;

            struct appdata { float4 vertex : POSITION; fixed4 color : COLOR; };
            struct v2f     { float4 pos : SV_POSITION; fixed4 color : COLOR; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return i.color;
            }
            ENDCG
        }
    }
}
