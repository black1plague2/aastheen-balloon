// Legacy-style unlit vertex-color shader for GL immediate-mode stamping
// (PaintableSurface.StampAt/DrawPatternGuide). Replaces Shader.Find("Hidden/Internal-Colored"),
// which is DontSave-flagged and gets stripped from device builds. Lives under Resources/ so it's
// never stripped, and uses UnityObjectToClipPos (legacy UNITY_MATRIX_MVP) rather than URP's
// TransformObjectToHClip because GL.LoadPixelMatrix only feeds the legacy transform stack.
Shader "PaintWall/GLStamp"
{
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            ZWrite Off
            ZTest Always
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; fixed4 color : COLOR; };
            struct v2f     { float4 pos : SV_POSITION; fixed4 color : COLOR; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
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
