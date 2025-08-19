// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Hidden/Minimap/LayerColor"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Pass
        {
            ZWrite On
            Cull Off
            Lighting Off
            Fog { Mode Off }
            ColorMask RGBA

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // —ерый дл€ сло€ дороги (layer 8), белый дл€ остальных
                int id = int(unity_ObjectToWorld._m03) % 32; // временный костыль, заменим на реальный ID через PropertyBlock
                if (unity_ObjectToWorld._m03 == 8) // сюда реально передаем слой через PropertyBlock
                    return fixed4(0.5, 0.5, 0.5, 1);
                return fixed4(1, 1, 1, 1);
            }
            ENDCG
        }
    }
}
