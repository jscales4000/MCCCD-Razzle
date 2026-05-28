// DebugTrace — singleton trace API with 1000-event ring buffer + monotonic
// IDs. The polling endpoint at GET /events?since=N reads from this buffer.
//
// Public API (mirror of 1Beyond's):
//   DebugTrace.Lifecycle(msg, data?)            — boot/shutdown/IP change/...
//   DebugTrace.Command(device, method, args?)   — outbound command, returns correlation id
//   DebugTrace.Response(device, raw, corrId?)   — device's response
//   DebugTrace.Error(device, msg, corrId?)
//   DebugTrace.StateChange(device, prop, val, corrId?)
//   DebugTrace.SigChange(device, signal, type, val, corrId?)
//
// Every method wraps in try/catch — instrumentation must never crash. JSON
// allocation always happens (event volume <10/s typ) — keeps the polling
// endpoint usable without needing client-presence tracking.

using System;
using System.Collections.Generic;
using Crestron.SimplSharp;

namespace MCCCD_AA140.Debug
{
    public static class DebugTrace
    {
        public sealed class EventEntry
        {
            public long Id;
            public string Json;
        }

        private const int RingCapacity = 1000;
        private static readonly EventEntry[] _ring = new EventEntry[RingCapacity];
        private static int _ringWriteIndex = 0;
        private static long _nextEventId = 1;
        private static readonly CCriticalSection _ringLock = new CCriticalSection();
        private static int _correlationCounter = 0;

        // ─── Public API ──────────────────────────────────────────────────

        public static void Lifecycle(string message, IDictionary<string, object> data = null)
        {
            var payload = data ?? new Dictionary<string, object>();
            payload["message"] = message;
            Send("system", null, payload, null);
        }

        public static string Command(string device, string method, object args = null, string correlationId = null)
        {
            if (correlationId == null) correlationId = NewCorrelationId();
            Send("command", device, new Dictionary<string, object> {
                { "method", method },
                { "args", args },
            }, correlationId);
            return correlationId;
        }

        public static void Response(string device, string rawResponse, string correlationId = null)
        {
            Send("response", device, new Dictionary<string, object> {
                { "raw", rawResponse },
            }, correlationId);
        }

        public static void Error(string device, string message, string correlationId = null)
        {
            Send("error", device, new Dictionary<string, object> {
                { "message", message },
            }, correlationId);
        }

        public static void StateChange(string device, string property, object newValue, string correlationId = null)
        {
            Send("state_change", device, new Dictionary<string, object> {
                { "property", property },
                { "value", newValue },
            }, correlationId);
        }

        public static void SigChange(string device, string signal, string signalType, object value, string correlationId = null)
        {
            Send("sig_change", device, new Dictionary<string, object> {
                { "signal", signal },
                { "signalType", signalType },
                { "value", value },
            }, correlationId);
        }

        // ─── Ring buffer access (used by DebugServer's /events poll) ─────

        public static EventEntry[] DrainSince(long since, int maxCount, out long nextSince)
        {
            var hits = new List<EventEntry>(maxCount);
            long high = since;
            try { _ringLock.Enter();
                for (int i = 0; i < RingCapacity; i++) {
                    var e = _ring[i];
                    if (e == null) continue;
                    if (e.Id > since) hits.Add(e);
                    if (e.Id > high) high = e.Id;
                }
            }
            finally { _ringLock.Leave(); }

            hits.Sort((a, b) => a.Id.CompareTo(b.Id));
            if (hits.Count > maxCount) hits = hits.GetRange(0, maxCount);
            nextSince = hits.Count > 0 ? hits[hits.Count - 1].Id : high;
            return hits.ToArray();
        }

        public static long CurrentEventId()
        {
            try { _ringLock.Enter(); return _nextEventId - 1; }
            finally { _ringLock.Leave(); }
        }

        // ─── Internal ────────────────────────────────────────────────────

        private static void Send(string eventType, string device, object data, string correlationId)
        {
            try {
                var json = JsonProtocol.SerializeEvent(eventType, device, data, correlationId);
                AppendToRing(json);
            } catch (Exception ex) {
                ErrorLog.Error("DebugTrace.Send eventType={0}: {1}", eventType, ex.Message);
            }
        }

        private static void AppendToRing(string json)
        {
            try { _ringLock.Enter();
                long id = _nextEventId++;
                _ring[_ringWriteIndex] = new EventEntry { Id = id, Json = json };
                _ringWriteIndex = (_ringWriteIndex + 1) % RingCapacity;
            }
            finally { _ringLock.Leave(); }
        }

        private static string NewCorrelationId()
        {
            int n = System.Threading.Interlocked.Increment(ref _correlationCounter);
            return "c" + n.ToString();
        }
    }
}
