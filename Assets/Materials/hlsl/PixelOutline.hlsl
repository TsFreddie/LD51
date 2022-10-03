#ifndef SHADER_PIXELOUTLINE
#define SHADER_PIXELOUTLINE

void PixelOutline_float(UnityTexture2D tex, float2 uv, float4 outlineColor, out float4 color, out float alpha, out float hasOutline)
{
    float2 texelSize = tex.texelSize;

    color = SAMPLE_TEXTURE2D(tex, tex.samplerstate, uv);
    float up = SAMPLE_TEXTURE2D(tex, tex.samplerstate, uv - float2(texelSize.x, 0)).a;
    float right = SAMPLE_TEXTURE2D(tex, tex.samplerstate, uv + float2(texelSize.x, 0)).a;
    float top = SAMPLE_TEXTURE2D(tex, tex.samplerstate, uv - float2(0, texelSize.y)).a;
    float bottom = SAMPLE_TEXTURE2D(tex, tex.samplerstate, uv + float2(0, texelSize.y)).a;

    float outline = saturate((up - color.a) + (right - color.a) + (top - color.a) + (bottom - color.a));

    if (outline > 0.5) {
        hasOutline = 1;
        color = outlineColor;
    } else {
        hasOutline = 0;
    }
    alpha = color.a;
}

#endif 