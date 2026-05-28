// JsonProtocol — Manual JSON serialization for the debug event stream.
//
// WHY MANUAL: Newtonsoft.Json is unreliable on Crestron's Mono runtime for
// hot-path serialization. StringBuilder is GC-friendly and predictable.
//
// EVENT SCHEMA:
//   { "type":"event", "eventType":"...", "device":"...?",
//     "timestamp":"2026-05-27T22:00:00.000Z",
//     "correlationId":"...?", "data": {...} }
//
// Ported from 1Beyond ISMIv3.Debug.JsonProtocol — kept near-verbatim because
// the pattern is well-tested and AA140 has the same Mono-on-4-Series constraint.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace MCCCD_AA140.Debug
{
    public static class JsonProtocol
    {
        public static string SerializeEvent(
            string eventType,
            string device = null,
            object data = null,
            string correlationId = null)
        {
            var sb = new StringBuilder(256);
            sb.Append("{\"type\":\"event\",\"eventType\":");
            AppendString(sb, eventType ?? "");
            if (device != null) {
                sb.Append(",\"device\":");
                AppendString(sb, device);
            }
            sb.Append(",\"timestamp\":");
            AppendString(sb, DateTime.UtcNow.ToString(
                "yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture));
            if (correlationId != null) {
                sb.Append(",\"correlationId\":");
                AppendString(sb, correlationId);
            }
            sb.Append(",\"data\":");
            AppendValue(sb, data);
            sb.Append('}');
            return sb.ToString();
        }

        public static void AppendValue(StringBuilder sb, object v)
        {
            if (v == null)               { sb.Append("null"); return; }
            if (v is bool b)             { sb.Append(b ? "true" : "false"); return; }
            if (v is string s)           { AppendString(sb, s); return; }
            if (v is int i)              { sb.Append(i.ToString(CultureInfo.InvariantCulture)); return; }
            if (v is long l)             { sb.Append(l.ToString(CultureInfo.InvariantCulture)); return; }
            if (v is ushort us)          { sb.Append(us.ToString(CultureInfo.InvariantCulture)); return; }
            if (v is uint ui)            { sb.Append(ui.ToString(CultureInfo.InvariantCulture)); return; }
            if (v is double d)           { sb.Append(d.ToString("R", CultureInfo.InvariantCulture)); return; }
            if (v is float f)            { sb.Append(f.ToString("R", CultureInfo.InvariantCulture)); return; }
            if (v is DateTime dt)        { AppendString(sb, dt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture)); return; }
            if (v is IDictionary<string, object> dict) {
                sb.Append('{');
                bool first = true;
                foreach (var kv in dict) {
                    if (!first) sb.Append(',');
                    AppendString(sb, kv.Key);
                    sb.Append(':');
                    AppendValue(sb, kv.Value);
                    first = false;
                }
                sb.Append('}');
                return;
            }
            if (v is System.Collections.IEnumerable enumerable && !(v is string)) {
                sb.Append('[');
                bool first = true;
                foreach (var item in enumerable) {
                    if (!first) sb.Append(',');
                    AppendValue(sb, item);
                    first = false;
                }
                sb.Append(']');
                return;
            }
            AppendString(sb, v.ToString() ?? "");
        }

        public static void AppendString(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (char c in s) {
                switch (c) {
                    case '"':  sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n");  break;
                    case '\r': sb.Append("\\r");  break;
                    case '\t': sb.Append("\\t");  break;
                    default:
                        if (c < 0x20)
                            sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else
                            sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }
    }
}
