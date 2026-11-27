# Measures frame time and memory (FINDINGS.md, "Runtime performance") of the Unity and Godot web builds in a real Chrome via the
# DevTools Protocol (stdlib only). Usage: perf.py <cdp_port> <runs> -> JSON lines on stdout.
import base64, json, os, socket, struct, sys, time, urllib.request

PORT, RUNS = int(sys.argv[1]), int(sys.argv[2])
BUILDS = {"unity": "http://localhost:8401/", "godot": "http://localhost:8402/"}
THROTTLES = [1, 4, 10, 20]

class WS:
    def __init__(self, url):
        host, path = url[len("ws://"):].split("/", 1)
        h, p = host.split(":")
        self.s = socket.create_connection((h, int(p))); self.s.settimeout(200)
        key = base64.b64encode(os.urandom(16)).decode()
        self.s.sendall((f"GET /{path} HTTP/1.1\r\nHost: {host}\r\nUpgrade: websocket\r\nConnection: Upgrade\r\n"
                        f"Sec-WebSocket-Key: {key}\r\nSec-WebSocket-Version: 13\r\n\r\n").encode())
        buf = b""
        while b"\r\n\r\n" not in buf:
            buf += self.s.recv(4096)
        self.rest = buf.split(b"\r\n\r\n", 1)[1]
        self.id = 0

    def _read(self, n):
        while len(self.rest) < n:
            chunk = self.s.recv(65536)
            if not chunk:
                raise EOFError
            self.rest += chunk
        out, self.rest = self.rest[:n], self.rest[n:]
        return out

    def send(self, obj):
        data = json.dumps(obj).encode()
        hdr = bytearray([0x81])
        n = len(data)
        if n < 126: hdr.append(0x80 | n)
        elif n < 65536: hdr += bytes([0x80 | 126]) + struct.pack(">H", n)
        else: hdr += bytes([0x80 | 127]) + struct.pack(">Q", n)
        mask = os.urandom(4)
        self.s.sendall(bytes(hdr) + mask + bytes(b ^ mask[i % 4] for i, b in enumerate(data)))

    def recv(self):
        msg = b""
        while True:
            b0, b1 = self._read(2)
            n = b1 & 0x7F
            if n == 126: n = struct.unpack(">H", self._read(2))[0]
            elif n == 127: n = struct.unpack(">Q", self._read(8))[0]
            payload = self._read(n)
            if (b0 & 0x0F) in (0x8, 0x9, 0xA):
                continue
            msg += payload
            if b0 & 0x80:
                return json.loads(msg)

    def call(self, method, **params):
        self.id += 1
        self.send({"id": self.id, "method": method, "params": params})
        while True:
            m = self.recv()
            if m.get("id") == self.id:
                if "error" in m:
                    raise RuntimeError(f"{method}: {m['error']}")
                return m.get("result", {})

    def eval(self, expr, timeout=120):
        r = self.call("Runtime.evaluate", expression=expr, awaitPromise=True, returnByValue=True, timeout=timeout * 1000)
        if "exceptionDetails" in r:
            raise RuntimeError(r["exceptionDetails"])
        return r["result"].get("value")

# In-page sampler: rAF deltas for `ms`, optionally holding D (walk right) for the middle part.
SAMPLE = """(async (ms, walk) => {
  const c = document.querySelector('canvas'); c.focus();
  const key = (t) => c.dispatchEvent(new KeyboardEvent(t, {key:'d', code:'KeyD', keyCode:68, which:68, bubbles:true}));
  const d = []; let last = performance.now(); const end = last + ms;
  if (walk) key('keydown');
  await new Promise(res => { const f = (t) => { d.push(t - last); last = t; if (t < end) requestAnimationFrame(f); else res(); }; requestAnimationFrame(f); });
  if (walk) key('keyup');
  d.shift(); d.sort((a,b)=>a-b);
  const q = p => d[Math.min(d.length-1, Math.floor(p*d.length))];
  const wasm = (window.__wasmMems||[]).reduce((a,m)=>a+m.buffer.byteLength,0);
  return {frames:d.length, avg:d.reduce((a,b)=>a+b,0)/d.length, p50:q(.5), p95:q(.95), p99:q(.99), max:d[d.length-1],
          over20:d.filter(x=>x>20).length, over33:d.filter(x=>x>33.4).length,
          jsHeap: performance.memory ? performance.memory.usedJSHeapSize : null,
          jsHeapTotal: performance.memory ? performance.memory.totalJSHeapSize : null, wasm,
          ready: window.__readyMs};
})(%d, %s)"""

def target():
    req = urllib.request.Request(f"http://127.0.0.1:{PORT}/json/new?about:blank", method="PUT")
    t = json.load(urllib.request.urlopen(req))
    return t["id"], WS(t["webSocketDebuggerUrl"])

def metrics(ws):
    return {m["name"]: m["value"] for m in ws.call("Performance.getMetrics")["metrics"]}

def sample(ws, ms, walk):
    before = metrics(ws)
    r = ws.eval(SAMPLE % (ms, walk), 120)
    after = metrics(ws)
    for k in ("TaskDuration", "ScriptDuration", "LayoutDuration", "RecalcStyleDuration"):
        r["cpu_" + k] = (after[k] - before[k]) * 1000 / max(r["frames"], 1)  # ms of main-thread work per frame
    return r

for run in range(RUNS):
    for name, url in BUILDS.items():
        for rate in THROTTLES:
            tid, ws = target()
            try:
                ws.call("Page.enable"); ws.call("Runtime.enable"); ws.call("Performance.enable", timeDomain="threadTicks")
                ws.call("Network.enable"); ws.call("Network.setCacheDisabled", cacheDisabled=True)
                ws.call("Emulation.setCPUThrottlingRate", rate=rate)
                ws.call("Page.navigate", url=f"{url}?perf={run}-{rate}")
                ready = ws.eval("(async()=>{for(let i=0;i<1500&&!window.__readyMs;i++)await new Promise(r=>setTimeout(r,100));return window.__readyMs||null})()", 190)
                ws.eval("new Promise(r=>setTimeout(r,6000))", 60)
                idle = sample(ws, 8000, "false")
                walk = sample(ws, 3000, "true")
                if run == 0 and rate == 1:
                    shot = ws.call("Page.captureScreenshot", format="png")["data"]
                    open(os.path.join(os.path.dirname(__file__), f"shot-{name}-after-walk.png"), "wb").write(base64.b64decode(shot))
                ws.call("HeapProfiler.enable"); ws.call("HeapProfiler.collectGarbage")
                mem = ws.eval("({js: performance.memory.usedJSHeapSize, wasm: (window.__wasmMems||[]).reduce((a,m)=>a+m.buffer.byteLength,0)})", 30)
                print(json.dumps({"run": run, "build": name, "cpu_throttle": rate, "ready_ms": ready, "idle": idle, "walk": walk, "mem": mem}), flush=True)
            except Exception as e:
                print(json.dumps({"run": run, "build": name, "cpu_throttle": rate, "error": repr(e)}), flush=True)
            try:
                urllib.request.urlopen(f"http://127.0.0.1:{PORT}/json/close/{tid}")
            except Exception:
                pass
