Shader "Disintegration"
{
    Properties
    {
        [MainTexture] _MainTex ("Texture", 2D) = "white" {}
        _MaskTex ("Mask", 2D) = "white" {}
        [MainColor] _Color ("Color", Color) = (1, 1, 1, 1)

        _FlowMap ("Flow (RG)", 2D) = "black" {}
        _DissolveTexture ("Dissolve Texture", 2D) = "white" {}
        _DissolveColor ("Dissolve Color Border", Color) = (1, 1, 1, 1)
        _DissolveBorder ("Dissolve Border", Range(0.001, 0.25)) = 0.05

        _Exapnd ("Expand", Float) = 1
        _Weight ("Weight", Range(0,1)) = 0
        _Direction ("Direction", Vector) = (0, 0, 0, 0)
        [HDR] _DisintegrationColor ("Disintegration Color", Color) = (1, 1, 1, 1)
        _Glow ("Glow", Float) = 1

        _Shape ("Shape Texture", 2D) = "white" {}
        _R ("Radius", Range(0,1)) = 0.5

        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [HideInInspector] _AlphaTex ("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "CanUseSpriteAtlas" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Name "Universal2D"
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex CombinedShapeLightVertex
            #pragma fragment CombinedShapeLightFragment
            #pragma multi_compile_instancing
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_0 __
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_1 __
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_2 __
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_3 __
            #pragma multi_compile_fragment _ DEBUG_DISPLAY

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/LightingUtility.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 flowUV : TEXCOORD1;
                half2 lightingUV : TEXCOORD2;
                half4 color : COLOR;
                #if defined(DEBUG_DISPLAY)
                float3 positionWS : TEXCOORD3;
                #endif
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_MaskTex);
            SAMPLER(sampler_MaskTex);
            TEXTURE2D(_FlowMap);
            SAMPLER(sampler_FlowMap);
            TEXTURE2D(_DissolveTexture);
            SAMPLER(sampler_DissolveTexture);
            TEXTURE2D(_Shape);
            SAMPLER(sampler_Shape);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _FlowMap_ST;
                float4 _Color;
                half4 _RendererColor;
                float4 _DissolveColor;
                float4 _Direction;
                float4 _DisintegrationColor;
                float _DissolveBorder;
                float _Exapnd;
                float _Weight;
                float _Glow;
                float _R;
            CBUFFER_END

            #if USE_SHAPE_LIGHT_TYPE_0
            SHAPE_LIGHT(0)
            #endif

            #if USE_SHAPE_LIGHT_TYPE_1
            SHAPE_LIGHT(1)
            #endif

            #if USE_SHAPE_LIGHT_TYPE_2
            SHAPE_LIGHT(2)
            #endif

            #if USE_SHAPE_LIGHT_TYPE_3
            SHAPE_LIGHT(3)
            #endif

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/CombinedShapeLightShared.hlsl"

            float2 RemapFlowRG(float2 rg)
            {
                return rg * 2.0 - 1.0;
            }

            Varyings CombinedShapeLightVertex(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                #ifdef UNITY_INSTANCING_ENABLED
                v.positionOS = UnityFlipSprite(v.positionOS, unity_SpriteFlip);
                #endif

                o.positionCS = TransformObjectToHClip(v.positionOS);
                #if defined(DEBUG_DISPLAY)
                o.positionWS = TransformObjectToWorld(v.positionOS);
                #endif
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.flowUV = TRANSFORM_TEX(v.uv, _FlowMap);
                o.lightingUV = half2(ComputeScreenPos(o.positionCS / o.positionCS.w).xy);
                o.color = v.color * _Color * _RendererColor;

                #ifdef UNITY_INSTANCING_ENABLED
                o.color *= unity_SpriteColor;
                #endif
                return o;
            }

            half4 ApplyDissolve(half4 baseCol, float2 uv, float2 flowUV)
            {
                float2 flow = RemapFlowRG(SAMPLE_TEXTURE2D(_FlowMap, sampler_FlowMap, flowUV).rg);
                float2 dissolveUV = uv + (_Direction.xy + flow * _Exapnd) * _Weight;

                if (_Weight >= 0.9999)
                {
                    clip(-1.0);
                }

                float dissolve = SAMPLE_TEXTURE2D(_DissolveTexture, sampler_DissolveTexture, dissolveUV).r;
                float threshold = saturate(_Weight * 2.0);
                clip(dissolve - threshold);

                float borderWidth = max(_DissolveBorder, 0.001);
                float edge = 1.0 - smoothstep(0.0, borderWidth, dissolve - threshold);
                float shapeMask = step(saturate(_R), SAMPLE_TEXTURE2D(_Shape, sampler_Shape, uv).r);

                half3 edgeColor = _DissolveColor.rgb * (_Glow * edge);
                half3 disintegration = _DisintegrationColor.rgb * (_Glow * edge * shapeMask);

                baseCol.rgb += edgeColor + disintegration;
                return baseCol;
            }

            half4 CombinedShapeLightFragment(Varyings i) : SV_Target
            {
                half4 baseCol = i.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                baseCol = ApplyDissolve(baseCol, i.uv, i.flowUV);

                const half4 mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, i.uv);
                SurfaceData2D surfaceData;
                InputData2D inputData;

                InitializeSurfaceData(baseCol.rgb, baseCol.a, mask, surfaceData);
                InitializeInputData(i.uv, i.lightingUV, inputData);

                half4 lit = CombinedShapeLightShared(surfaceData, inputData);
                return lit;
            }
            ENDHLSL
        }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" "Queue"="Transparent" "RenderType"="Transparent"}

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex UnlitVertex
            #pragma fragment UnlitFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 flowUV : TEXCOORD1;
                float4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_FlowMap);
            SAMPLER(sampler_FlowMap);
            TEXTURE2D(_DissolveTexture);
            SAMPLER(sampler_DissolveTexture);
            TEXTURE2D(_Shape);
            SAMPLER(sampler_Shape);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _FlowMap_ST;
                float4 _Color;
                half4 _RendererColor;
                float4 _DissolveColor;
                float4 _Direction;
                float4 _DisintegrationColor;
                float _DissolveBorder;
                float _Exapnd;
                float _Weight;
                float _Glow;
                float _R;
            CBUFFER_END

            float2 RemapFlowRG(float2 rg)
            {
                return rg * 2.0 - 1.0;
            }

            Varyings UnlitVertex(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                #ifdef UNITY_INSTANCING_ENABLED
                v.positionOS = UnityFlipSprite(v.positionOS, unity_SpriteFlip);
                #endif

                o.positionCS = TransformObjectToHClip(v.positionOS);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.flowUV = TRANSFORM_TEX(v.uv, _FlowMap);
                o.color = v.color * _Color * _RendererColor;

                #ifdef UNITY_INSTANCING_ENABLED
                o.color *= unity_SpriteColor;
                #endif
                return o;
            }

            half4 UnlitFragment(Varyings i) : SV_Target
            {
                half4 baseCol = i.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);

                float2 flow = RemapFlowRG(SAMPLE_TEXTURE2D(_FlowMap, sampler_FlowMap, i.flowUV).rg);
                float2 dissolveUV = i.uv + (_Direction.xy + flow * _Exapnd) * _Weight;

                // Force full disappearance at the end of the animation.
                if (_Weight >= 0.9999)
                {
                    clip(-1.0);
                }

                float dissolve = SAMPLE_TEXTURE2D(_DissolveTexture, sampler_DissolveTexture, dissolveUV).r;
                float threshold = saturate(_Weight * 2.0);
                clip(dissolve - threshold);

                float borderWidth = max(_DissolveBorder, 0.001);
                float edge = 1.0 - smoothstep(0.0, borderWidth, dissolve - threshold);
                float shapeMask = step(saturate(_R), SAMPLE_TEXTURE2D(_Shape, sampler_Shape, i.uv).r);

                half3 edgeColor = _DissolveColor.rgb * (_Glow * edge);
                half3 disintegration = _DisintegrationColor.rgb * (_Glow * edge * shapeMask);

                baseCol.rgb += edgeColor + disintegration;
                return baseCol;
            }

            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
