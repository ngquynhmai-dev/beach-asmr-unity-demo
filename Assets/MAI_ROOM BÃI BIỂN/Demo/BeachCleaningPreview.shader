Shader "Mai/BeachCleaningPreview"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _DirtyTex ("Dirty", 2D) = "white" {}
        _CleanTex ("Clean", 2D) = "white" {}
        _Clean ("Cleaning", Range(0,1)) = 0
        _Repair ("Repair", Range(0,1)) = 0
        _Damage ("Damage ellipse UV", Vector) = (-2,-2,0.01,0.01)
        _Foam ("Foam", Range(0,1)) = 0
        _Wet ("Wet", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="False" }
        Cull Off Lighting Off ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            sampler2D _DirtyTex, _CleanTex;
            float _Clean, _Repair, _Foam, _Wet;
            float4 _Damage;
            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color; return o; }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 dirty=tex2D(_DirtyTex,i.uv);
                fixed4 clean=tex2D(_CleanTex,i.uv);
                float hole=1-smoothstep(.80,1.04,length((i.uv-_Damage.xy)/max(_Damage.zw,float2(.001,.001))));
                float blend=lerp(_Clean,_Repair,hole);
                fixed4 c=lerp(dirty,clean,blend)*i.color;
                float bubbles=sin(i.uv.x*89)*sin(i.uv.y*103)+sin(i.uv.x*53+i.uv.y*71);
                float foam=saturate((bubbles+.8)*.35)*_Foam*.75;
                c.rgb=lerp(c.rgb,float3(.93,.98,1),foam);
                c.rgb*=1-_Wet*.055;
                c.rgb*=c.a;
                return c;
            }
            ENDCG
        }
    }
}
