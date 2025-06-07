using System.Collections.Generic;
using System.Globalization;

namespace JsonContentTranslator.AutoTranslate
{
    public static class StringExtensions
    {
        public static List<string> SplitToLines(this string s) => s.SplitToLines(s.Length);

        public static string CapitalizeFirstLetter(this string s, CultureInfo ci = null)
        {
            var si = new StringInfo(s);
            if (ci == null)
            {
                ci = CultureInfo.CurrentCulture;
            }

            if (si.LengthInTextElements > 0)
            {
                s = si.SubstringByTextElements(0, 1).ToUpper(ci);
            }

            if (si.LengthInTextElements > 1)
            {
                s += si.SubstringByTextElements(1);
            }

            return s;
        }

        public static List<string> SplitToLines(this string s, int max)
        {
            //original non-optimized version: return source.Replace("\r\r\n", "\n").Replace("\r\n", "\n").Replace('\r', '\n').Replace('\u2028', '\n').Split('\n');

            var lines = new List<string>();
            var start = 0;
            var i = 0;

            if (s.Length < max)
            {
                max = s.Length;
            }

            while (i < max)
            {
                var ch = s[i];
                if (ch == '\r')
                {
                    // See https://github.com/SubtitleEdit/subtitleedit/issues/8854
                    // SE now tries to follow how VS code opens text file
                    //if (i < max - 2 && s[i + 1] == '\r' && s[i + 2] == '\n') // \r\r\n
                    //{
                    //    lines.Add(s.Substring(start, i - start));
                    //    i += 3;
                    //    start = i;
                    //    continue;
                    //}

                    if (i < max - 1 && s[i + 1] == '\n') // \r\n
                    {
                        lines.Add(s.Substring(start, i - start));
                        i += 2;
                        start = i;
                        continue;
                    }

                    lines.Add(s.Substring(start, i - start));
                    i++;
                    start = i;
                    continue;
                }

                if (ch == '\n' || ch == '\u2028')
                {
                    lines.Add(s.Substring(start, i - start));
                    i++;
                    start = i;
                    continue;
                }

                i++;
            }

            lines.Add(s.Substring(start, i - start));
            return lines;
        }

      
    }
}
