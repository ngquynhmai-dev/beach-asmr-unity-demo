Shader "Mai/RescueBasket"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Mouth ("Opening UV ellipse", Vector) = (.5,.62,.36,.14)
        _FrontOnly ("Front wall only", Float) = 0
        _ClipInside ("Clip occupant inside opening", Float) = 0
        _BasketRect ("Basket world rectangle", Vector) = (0,0,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="False" }
        Cull Off Lighting Off ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
            struct v2f {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;float2 world:TEXCOORD1;fixed4 color:COLOR;};
            sampler2D _MainTex;
            float4 _Mouth,_BasketRect;
            float _FrontOnly,_ClipInside;
            v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.world=mul(unity_ObjectToWorld,v.vertex).xy;o.color=v.color;return o;}
            fixed4 frag(v2f i):SV_Target
            {
                if(_FrontOnly>.5)
                {
                    float dx=(i.uv.x-_Mouth.x)/_Mouth.z;
                    float front=_Mouth.y-_Mouth.w*sqrt(saturate(1-dx*dx));
                    clip(front-i.uv.y);
                }
                if(_ClipInside>.5)
                {
                    float2 basketUV=(i.world-_BasketRect.xy)/_BasketRect.zw;
                    float2 p=(basketUV-_Mouth.xy)/_Mouth.zw;
                    clip(1-dot(p,p));
                }
                fixed4 c=tex2D(_MainTex,i.uv)*i.color;c.rgb*=c.a;return c;
            }
            ENDCG
        }
    }
}
