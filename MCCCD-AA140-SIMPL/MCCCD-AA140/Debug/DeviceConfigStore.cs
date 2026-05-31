// DeviceConfigStore — persists per-device IP + enabled flag to a single
// JSON file at /user/aa140/devices.json. Loaded at boot; mutated by the
// debug panel's POST /devices/<key> endpoint.
//
// Schema (one entry per device key):
//   {
//     "p300":     { "host":"192.168.2.151", "enabled":true  },
//     "mxa-a":    { "host":"192.168.2.181", "enabled":true  },
//     "mxa-b":    { "host":"192.168.2.182", "enabled":true  },
//     "sony-1":   { "host":"192.168.2.191", "enabled":false },
//     "sony-2":   { "host":"192.168.2.192", "enabled":false },
//     "newline":  { "host":"192.168.2.195", "enabled":false },
//     "airmedia": { "host":"192.168.1.177", "enabled":true  },
//     "cam-1":    { "host":"192.168.2.172", "enabled":true  },
//     "cam-2":    { "host":"192.168.2.173", "enabled":true  }
//   }
//
// "enabled" gates whether the corresponding TCP/REST service even attempts
// to connect — when false the service stays idle (no log spam from stub IPs).
// "host" can be edited at runtime; the service rebinds to the new IP on save.
//
// Persistence is best-effort: a save failure is logged but doesn't crash.
// Missing or corrupt file → in-memory defaults baked in this class.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Crestron.SimplSharp;

namespace MCCCD_AA140.Debug
{
    public class DeviceConfigStore
    {
        private const string DefaultPath = "/user/aa140/devices.json";

        public class Entry
        {
            public string Host;
            public bool Enabled;
        }

        // Bake the as-shipped defaults so a missing file still gives the
        // services sane stub IPs to display in the debug UI.
        private static Dictionary<string, Entry> Defaults() => new Dictionary<string, Entry> {
            { "p300",     new Entry { Host = "192.168.2.151", Enabled = true  } },
            { "mxa-a",    new Entry { Host = "192.168.2.181", Enabled = true  } },
            { "mxa-b",    new Entry { Host = "192.168.2.182", Enabled = true  } },
            { "sony-1",   new Entry { Host = "192.168.2.191", Enabled = false } },
            { "sony-2",   new Entry { Host = "192.168.2.192", Enabled = false } },
            { "newline",  new Entry { Host = "192.168.2.195", Enabled = false } },
            { "airmedia", new Entry { Host = "192.168.1.177", Enabled = true  } },
            { "cam-1",    new Entry { Host = "192.168.2.174", Enabled = true  } },  // IV-CAM-I20 (VISCA 5500)
            { "cam-2",    new Entry { Host = "192.168.2.173", Enabled = true  } },  // IV-CAM-I12 (VISCA 5500)
        };

        private readonly string _path;
        private readonly object _lock = new object();
        private Dictionary<string, Entry> _entries;

        public DeviceConfigStore(string path = DefaultPath)
        {
            _path = path;
        }

        /// <summary>
        /// Loads the JSON from disk, falling back to defaults on any error.
        /// Reports which source the data came from in `source`.
        /// </summary>
        public void Load(out string source)
        {
            lock (_lock) {
                try {
                    if (File.Exists(_path)) {
                        var text = File.ReadAllText(_path);
                        var parsed = ParseSimpleJson(text);
                        if (parsed != null && parsed.Count > 0) {
                            // Merge: defaults provide any missing keys; file
                            // values override. This way adding a new device
                            // to Defaults() doesn't require deleting the file.
                            _entries = Defaults();
                            foreach (var kv in parsed) _entries[kv.Key] = kv.Value;
                            source = "file";
                            return;
                        }
                    }
                } catch (Exception ex) {
                    ErrorLog.Warn("DeviceConfigStore: load failed, using defaults: {0}", ex.Message);
                }
                _entries = Defaults();
                source = "defaults";
            }
        }

        public IDictionary<string, Entry> Snapshot()
        {
            lock (_lock) {
                var copy = new Dictionary<string, Entry>(_entries.Count);
                foreach (var kv in _entries)
                    copy[kv.Key] = new Entry { Host = kv.Value.Host, Enabled = kv.Value.Enabled };
                return copy;
            }
        }

        public Entry Get(string key)
        {
            lock (_lock) {
                if (_entries.TryGetValue(key, out Entry e))
                    return new Entry { Host = e.Host, Enabled = e.Enabled };
                return null;
            }
        }

        /// <summary>
        /// Update one device's config and persist. Returns the merged entry.
        /// Pass <paramref name="host"/> or <paramref name="enabled"/> as null
        /// to leave that field unchanged.
        /// </summary>
        public Entry Set(string key, string host, bool? enabled)
        {
            Entry merged;
            lock (_lock) {
                if (!_entries.TryGetValue(key, out Entry cur))
                    cur = new Entry { Host = "", Enabled = false };
                if (host != null)    cur.Host = host.Trim();
                if (enabled.HasValue) cur.Enabled = enabled.Value;
                _entries[key] = cur;
                merged = new Entry { Host = cur.Host, Enabled = cur.Enabled };
            }
            try {
                SaveNow();
            } catch (Exception ex) {
                ErrorLog.Warn("DeviceConfigStore: save failed: {0}", ex.Message);
            }
            return merged;
        }

        private void SaveNow()
        {
            // Ensure the /user/aa140/ directory exists.
            try {
                var dir = Path.GetDirectoryName(_path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
            } catch { /* fall through and let WriteAllText error if it must */ }

            var sb = new StringBuilder(256);
            sb.Append('{');
            bool first = true;
            lock (_lock) {
                foreach (var kv in _entries) {
                    if (!first) sb.Append(',');
                    sb.Append('"').Append(kv.Key).Append("\":{\"host\":\"").Append(kv.Value.Host ?? "")
                      .Append("\",\"enabled\":").Append(kv.Value.Enabled ? "true" : "false").Append('}');
                    first = false;
                }
            }
            sb.Append('}');
            File.WriteAllText(_path, sb.ToString());
        }

        // Minimal JSON parser for the flat schema above. Avoids pulling in
        // Newtonsoft.Json. Tolerates whitespace and trailing newlines; rejects
        // anything beyond the expected shape (object of objects with host+enabled).
        private static Dictionary<string, Entry> ParseSimpleJson(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            int i = 0;
            SkipWs(text, ref i);
            if (i >= text.Length || text[i] != '{') return null;
            i++;
            var result = new Dictionary<string, Entry>();
            SkipWs(text, ref i);
            if (i < text.Length && text[i] == '}') return result;
            while (i < text.Length) {
                SkipWs(text, ref i);
                var key = ReadString(text, ref i);
                if (key == null) return null;
                SkipWs(text, ref i);
                if (i >= text.Length || text[i] != ':') return null;
                i++;
                SkipWs(text, ref i);
                var entry = ReadEntry(text, ref i);
                if (entry == null) return null;
                result[key] = entry;
                SkipWs(text, ref i);
                if (i < text.Length && text[i] == ',') { i++; continue; }
                if (i < text.Length && text[i] == '}') { i++; break; }
                return null;
            }
            return result;
        }

        private static Entry ReadEntry(string s, ref int i)
        {
            if (i >= s.Length || s[i] != '{') return null;
            i++;
            string host = "";
            bool enabled = false;
            while (i < s.Length) {
                SkipWs(s, ref i);
                if (s[i] == '}') { i++; return new Entry { Host = host, Enabled = enabled }; }
                var key = ReadString(s, ref i);
                if (key == null) return null;
                SkipWs(s, ref i);
                if (i >= s.Length || s[i] != ':') return null;
                i++;
                SkipWs(s, ref i);
                if (key == "host") {
                    host = ReadString(s, ref i) ?? "";
                } else if (key == "enabled") {
                    if      (i + 3 < s.Length && s.Substring(i, 4) == "true")  { enabled = true;  i += 4; }
                    else if (i + 4 < s.Length && s.Substring(i, 5) == "false") { enabled = false; i += 5; }
                    else return null;
                } else {
                    // Unknown key — skip value (string only for our schema).
                    if (s[i] == '"') ReadString(s, ref i);
                    else return null;
                }
                SkipWs(s, ref i);
                if (i < s.Length && s[i] == ',') { i++; continue; }
                if (i < s.Length && s[i] == '}') { i++; return new Entry { Host = host, Enabled = enabled }; }
                return null;
            }
            return null;
        }

        private static string ReadString(string s, ref int i)
        {
            if (i >= s.Length || s[i] != '"') return null;
            i++;
            var sb = new StringBuilder();
            while (i < s.Length) {
                char c = s[i++];
                if (c == '"') return sb.ToString();
                if (c == '\\' && i < s.Length) {
                    char esc = s[i++];
                    switch (esc) {
                        case '"':  sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/':  sb.Append('/'); break;
                        case 'n':  sb.Append('\n'); break;
                        case 'r':  sb.Append('\r'); break;
                        case 't':  sb.Append('\t'); break;
                        default:   sb.Append(esc); break;
                    }
                } else {
                    sb.Append(c);
                }
            }
            return null;
        }

        private static void SkipWs(string s, ref int i)
        {
            while (i < s.Length && (s[i] == ' ' || s[i] == '\t' || s[i] == '\r' || s[i] == '\n')) i++;
        }
    }
}
