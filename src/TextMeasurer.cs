using SkiaSharp;
using System;

namespace JsonContentTranslator;

public static class TextMeasurer
{

    public static SKSize MeasureString(string text, string fontFamily, float fontSize, SKFontStyleWeight weight = SKFontStyleWeight.Normal)
    {
        using var typeface = SKTypeface.FromFamilyName(fontFamily, weight, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
        using var paint = new SKPaint
        {
            Typeface = typeface,
            TextSize = fontSize
        };

        float width = paint.MeasureText(text);
        var metrics = paint.FontMetrics;
        float height = metrics.Descent - metrics.Ascent;
        return new SKSize(width, height);
    }
}
