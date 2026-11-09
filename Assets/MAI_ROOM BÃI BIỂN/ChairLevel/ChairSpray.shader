Shader "Mai/ChairSpray"
{
    Properties { _Tint ("Water / detergent", Color) = (.65,.88,1,.5) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off ZWrite Off Lighting Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;};
            struct v2f {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;};
            float4 _Tint;
            float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float noise(float2 p){float2 a=floor(p),b=frac(p);b=b*b*(3-2*b);return lerp(lerp(hash(a),hash(a+float2(1,0)),b.x),lerp(hash(a+float2(0,1)),hash(a+1),b.x),b.y);}
            v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o;}
            fixed4 frag(v2f i):SV_Target
            {
                float flow=i.uv.x*23-_Time.y*30;
                float mist=noise(float2(flow,i.uv.y*21))*.6+noise(float2(flow*2,i.uv.y*53))*.4;
                float edge=pow(saturate(1-abs(i.uv.y*2-1)),.7);
                float strands=pow(saturate(sin(i.uv.y*83+noise(float2(flow*.2,i.uv.y*8))*2)),12);
                float alpha=edge*(.18+mist*.48+strands*.2)*_Tint.a;
                alpha*=smoothstep(0,.06,i.uv.x)*(1-smoothstep(.72,1,i.uv.x));
                return float4(_Tint.rgb,alpha);
            }
            ENDCG
        }
    }
}
