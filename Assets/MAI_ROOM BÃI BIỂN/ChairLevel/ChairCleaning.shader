Shader "Mai/ChairCleaning"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _DirtyTex ("Dirty chair", 2D) = "white" {}
        _CleanTex ("Restored chair", 2D) = "white" {}
        _SandFreeState ("Dirty chair with all sand removed", 2D) = "white" {}
        _CushionEdge ("Smooth cushion silhouette", 2D) = "white" {}
        _FoamFlow ("Persistent ASMR foam displacement", 2D) = "gray" {}
        _FoamTex ("Soap lather material", 2D) = "white" {}
        _WetState ("Registered soaked chair", 2D) = "white" {}
        _SoapState ("Registered soap chair", 2D) = "white" {}
        _ScrubActive ("ASMR scrub contact strength", Float) = 0
        _ScrubTime ("ASMR scrub time", Float) = 0
        _ScrubPoint ("ASMR brush contact", Vector) = (0,0,0,0)
        _WorkMask ("Clean / foam / water / glue", 2D) = "black" {}
        _BrushMask ("Brush agitation / stroke center", 2D) = "black" {}
        _Damage ("Tear UV", Vector) = (0.49,0.49,0.16,0.13)
        _Repair ("Leather repair", Range(0,1)) = 0
        _Before ("Before comparison", Range(0,1)) = 0
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
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            sampler2D _FoamFlow, _CushionEdge;
            float4 _WorkMask_TexelSize;
            sampler2D _DirtyTex, _CleanTex, _WorkMask, _FoamTex, _BrushMask, _WetState, _SoapState, _SandFreeState;
            float _ScrubActive,_ScrubTime;float4 _ScrubPoint;
            float4 _Damage;
            float _Repair, _Before;
            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color; return o; }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 d=tex2D(_DirtyTex,i.uv), c=tex2D(_CleanTex,i.uv);
                if(_Before>.5) { d*=i.color; d.rgb*=d.a; return d; }
                float4 m=tex2D(_WorkMask,i.uv);
                float4 brush=tex2D(_BrushMask,i.uv);
                // The sand-free state already preserves the hole and yellow filling.
                // Reveal it before damage protection so sand around the tear is not protected as damage.
                fixed4 originalDirty=d;
                fixed4 sandFree=tex2D(_SandFreeState,i.uv);
                d=lerp(d,sandFree,brush.a);
                float distance=length((i.uv-_Damage.xy)/_Damage.zw);
                float tear=1-smoothstep(.76,1.05,distance);
                float amount=lerp(m.r,_Repair,tear);
                fixed4 result=lerp(d,c,amount)*i.color;
                // ASMR workflow: reveal registered painted state with the local stroke mask.
                fixed4 soaked=tex2D(_WetState,i.uv);
                float3 wetRatio=clamp(soaked.rgb/max(originalDirty.rgb,float3(.08,.08,.08)),.22,1.6);
                result.rgb=lerp(result.rgb,result.rgb*wetRatio,smoothstep(.02,.9,m.b));
                fixed4 soap=tex2D(_SoapState,i.uv);
                // Pad coarse work coverage, then trim against the source-resolution upholstery edge.
                float soapCoverage=m.g, foamBuild=brush.r;
                for(int oy=-1;oy<=1;oy++)for(int ox=-1;ox<=1;ox++)
                {
                    float2 tap=i.uv+float2(ox,oy)*_WorkMask_TexelSize.xy;
                    soapCoverage=max(soapCoverage,tex2D(_WorkMask,tap).g);
                    foamBuild=max(foamBuild,tex2D(_BrushMask,tap).r);
                }
                float foam=smoothstep(.02,.9,soapCoverage)*tex2D(_CushionEdge,i.uv).r;
                // ASMR DoghouseWork: move the painted lather, retaining stationary upholstery.
                float4 flow=tex2D(_FoamFlow,i.uv);
                float2 foamUV=i.uv+(flow.rg-.5)*.2;
                fixed4 worked=tex2D(_SoapState,foamUV);
                float coverage=flow.a*worked.a;
                coverage*=smoothstep(.12,.32,min(soap.r,min(soap.g,soap.b)));
                coverage*=smoothstep(.12,.32,min(worked.r,min(worked.g,worked.b)));
                float3 baseColor=tex2D(_WetState,i.uv).rgb;
                float3 shiftedBase=tex2D(_WetState,foamUV).rgb;
                float3 latherColor=max(0,worked.rgb-shiftedBase);
                latherColor*=lerp(.72,1.15,smoothstep(.1,.9,flow.b));
                // Modest accumulation keeps the original art and the ridges of each stroke.
                latherColor*=1+.12*saturate(brush.r);
                float3 lather=lerp(soap.rgb,saturate(baseColor+latherColor),coverage);
                // Build opaque, textured cream over every brushed cushion cell, including dark seams.
                float density=smoothstep(.04,.85,foamBuild);
                float3 creamTexture=tex2D(_FoamTex,foamUV*float2(1.1,1.5)).rgb;
                // Recover stationary cushion lighting from the registered clean upholstery.
                // Green/blue carry its illumination without the dark red albedo of teal leather.
                float2 lightStep=float2(.003,.003);
                float3 shape=c.rgb*.5;
                shape+=tex2D(_CleanTex,i.uv+float2(lightStep.x,0)).rgb*.125;
                shape+=tex2D(_CleanTex,i.uv-float2(lightStep.x,0)).rgb*.125;
                shape+=tex2D(_CleanTex,i.uv+float2(0,lightStep.y)).rgb*.125;
                shape+=tex2D(_CleanTex,i.uv-float2(0,lightStep.y)).rgb*.125;
                float ridge=lerp(.92,1.02,smoothstep(.1,.9,flow.b));
                float cushionLight=clamp(pow(max(shape.g,shape.b)/.68,.95),.36,1.06);
                float3 lightTint=lerp(float3(.84,.93,1),float3(1,.995,.965),smoothstep(.42,.96,cushionLight));
                float3 cream=creamTexture*ridge*cushionLight*lightTint;
                lather=lerp(lather,cream,density);
                result.rgb=lerp(result.rgb,lather,foam);
                result.rgb=lerp(result.rgb,float3(1,.91,.56),m.a*(1-_Repair));
                result.rgb*=result.a;
                return result;
            }
            ENDCG
        }
    }
}
