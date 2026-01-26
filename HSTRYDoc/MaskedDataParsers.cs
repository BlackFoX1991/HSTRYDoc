using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace HSTRYDoc
{
    internal static class MaskedDataParsers
    {
        // Multiple MASK blocks inside ONE .model block
        private static readonly Regex MaskHeaderRegex =
            new Regex(
                @"#MASK\s*:\s*(?<id>[A-Za-z_][A-Za-z0-9_]*)\s*""(?<name>[^""]+)""\s*:\s*",
                RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        private static readonly Regex EndRegex =
            new Regex(@"#END\b", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        // Field token supports: NAME or NAME:REFMODEL
        private static readonly Regex FieldTokenRegex =
            new Regex(
                @"^(?<name>[A-Za-z_][A-Za-z0-9_]*)(?:\s*:\s*(?<ref>[A-Za-z_][A-Za-z0-9_]*))?$",
                RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        public static bool TryParseMaskDefinitions(
            string modelBlockTitle,
            string rawText,
            out List<MaskDefinition> defs,
            out string error)
        {
            defs = new List<MaskDefinition>();
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(modelBlockTitle))
            {
                error = "Model block title is empty.";
                return false;
            }

            string text = StripComments(rawText ?? string.Empty);

            int pos = 0;
            var seenLocal = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            while (true)
            {
                var m = MaskHeaderRegex.Match(text, pos);
                if (!m.Success)
                    break;

                string id = (m.Groups["id"].Value ?? string.Empty).Trim();
                string display = (m.Groups["name"].Value ?? string.Empty).Trim();

                if (string.IsNullOrWhiteSpace(id))
                {
                    error = $"MASK id missing in '{modelBlockTitle}'.";
                    return false;
                }

                if (!seenLocal.Add(id))
                {
                    error = $"Duplicate MASK id '{id}' inside '{modelBlockTitle}'.";
                    return false;
                }

                int bodyStart = m.Index + m.Length;
                var end = EndRegex.Match(text, bodyStart);
                if (!end.Success)
                {
                    error = $"Missing #END for MASK '{id}' in '{modelBlockTitle}'.";
                    return false;
                }

                string body = text.Substring(bodyStart, end.Index - bodyStart);
                var fields = ParseFieldList(body);

                if (fields.Count == 0)
                {
                    error = $"No fields defined for MASK '{id}' in '{modelBlockTitle}'.";
                    return false;
                }

                defs.Add(new MaskDefinition
                {
                    ModelBlockTitle = modelBlockTitle,
                    MaskId = id,
                    DisplayName = display,
                    Fields = fields
                });

                pos = end.Index + end.Length;
            }

            return true;
        }

        private static List<MaskField> ParseFieldList(string body)
        {
            var tokens = body
                .Split(new[] { ',', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .ToList();

            var fields = new List<MaskField>();

            foreach (var t in tokens)
            {
                var m = FieldTokenRegex.Match(t);
                if (!m.Success)
                    continue;

                string name = (m.Groups["name"].Value ?? "").Trim();
                string? rf = m.Groups["ref"].Success ? (m.Groups["ref"].Value ?? "").Trim() : null;

                if (string.IsNullOrWhiteSpace(name))
                    continue;

                fields.Add(new MaskField
                {
                    Name = name,
                    RefMaskId = string.IsNullOrWhiteSpace(rf) ? null : rf
                });
            }

            // unique by field name, preserve order
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var outList = new List<MaskField>();
            foreach (var f in fields)
            {
                if (seen.Add(f.Name))
                    outList.Add(f);
            }

            return outList;
        }

        private static string StripComments(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;

            s = Regex.Replace(s, @"/\*.*?\*/", "", RegexOptions.Singleline);
            s = Regex.Replace(s, @"//.*?$", "", RegexOptions.Multiline);

            return s;
        }

        // ---------------- DATA ----------------

        private static readonly Regex AssignRegex =
            new Regex(@"^(?<key>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*(?<val>.+?)\s*$",
                RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static List<Dictionary<string, string>> ParseDataRecords(string rawText)
        {
            var records = new List<Dictionary<string, string>>();

            string text = rawText ?? string.Empty;
            var lines = text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');

            Dictionary<string, string>? current = null;

            foreach (var rawLine in lines)
            {
                string line = rawLine.Trim();
                if (line.Length == 0) continue;

                if (line == "*")
                {
                    if (current != null && current.Count > 0)
                        records.Add(current);

                    current = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    continue;
                }

                if (current == null)
                    current = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                var m = AssignRegex.Match(line);
                if (!m.Success)
                    continue;

                string key = m.Groups["key"].Value.Trim();
                string valRaw = m.Groups["val"].Value.Trim();

                string value = ParseValue(valRaw);
                current[key] = value;
            }

            if (current != null && current.Count > 0)
                records.Add(current);

            return records;
        }

        private static string ParseValue(string valRaw)
        {
            if (valRaw.Length >= 2)
            {
                if ((valRaw[0] == '"' && valRaw[^1] == '"') || (valRaw[0] == '\'' && valRaw[^1] == '\''))
                {
                    string inner = valRaw.Substring(1, valRaw.Length - 2);
                    return UnescapeBasic(inner);
                }
            }
            return UnescapeBasic(valRaw);
        }

        private static string UnescapeBasic(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;

            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '\\' && i + 1 < s.Length)
                {
                    char n = s[i + 1];
                    switch (n)
                    {
                        case '\\': sb.Append('\\'); i++; continue;
                        case '"': sb.Append('"'); i++; continue;
                        case '\'': sb.Append('\''); i++; continue;
                        case 'n': sb.Append('\n'); i++; continue;
                        case 'r': sb.Append('\r'); i++; continue;
                        case 't': sb.Append('\t'); i++; continue;
                    }
                }
                sb.Append(c);
            }
            return sb.ToString();
        }

        public static string SerializeDataRecords(MaskDefinition def, IEnumerable<Dictionary<string, string>> records)
        {
            var sb = new StringBuilder();

            var modelFields = def.Fields.Select(f => f.Name).ToList();
            var modelFieldSet = modelFields.ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var rec in records)
            {
                sb.AppendLine("*");

                foreach (var field in modelFields)
                {
                    if (rec.TryGetValue(field, out var v) && v != null)
                        AppendAssignLine(sb, field, v);
                }

                var extras = rec.Keys
                    .Where(k => !modelFieldSet.Contains(k))
                    .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (var k in extras)
                {
                    if (rec.TryGetValue(k, out var v) && v != null)
                        AppendAssignLine(sb, k, v);
                }
            }

            return sb.ToString().TrimEnd();
        }

        private static void AppendAssignLine(StringBuilder sb, string key, string value)
        {
            sb.Append(key);
            sb.Append(" = ");
            sb.Append('"');
            sb.Append(EscapeForQuotes(value));
            sb.AppendLine("\"");
        }

        private static string EscapeForQuotes(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
