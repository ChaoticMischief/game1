# Static server for the web load-time and runtime comparison in FINDINGS.md: serves X.gz with Content-Encoding gzip (Unity's
# pre-compressed build), gzips other text/wasm on the fly (like itch.io's CDN would), and injects
# a ready-time probe into index.html. Usage: serve.py <root> <port>
import gzip, http.server, os, sys, mimetypes
root, port = sys.argv[1], int(sys.argv[2])
mimetypes.add_type("application/wasm", ".wasm")
HOOK = b'<script>(function(){window.__wasmMems=[];var W=WebAssembly;var M=W.Memory;W.Memory=function(d){var m=new M(d);window.__wasmMems.push(m);return m;};W.Memory.prototype=M.prototype;\nfunction grab(r){try{var i=r.instance||r;for(var k in i.exports){if(i.exports[k] instanceof M&&window.__wasmMems.indexOf(i.exports[k])<0)window.__wasmMems.push(i.exports[k]);}}catch(e){}return r;}\nvar ins=W.instantiate,iss=W.instantiateStreaming;W.instantiate=function(){return ins.apply(W,arguments).then(grab);};if(iss)W.instantiateStreaming=function(){return iss.apply(W,arguments).then(grab);};})();</script>'
PROBE = b"""<script>(function(){var t0=performance.now();new MutationObserver(function(){
var u=document.querySelector('#overlay.done'), g=window.__sawStatus&&!document.getElementById('status');if(document.getElementById('status'))window.__sawStatus=1;
if((u||g)&&!window.__readyMs){window.__readyMs=performance.now();}}).observe(document.documentElement,{subtree:true,childList:true,attributes:true});})();</script>"""
cache = {}
class H(http.server.BaseHTTPRequestHandler):
    def do_GET(self):
        p = os.path.join(root, self.path.split("?")[0].lstrip("/") or "index.html")
        if os.path.isdir(p): p = os.path.join(p, "index.html")
        if not os.path.isfile(p): self.send_error(404); return
        data = open(p, "rb").read(); enc = None; name = p
        if p.endswith(".gz"): enc = "gzip"; name = p[:-3]
        if name.endswith("index.html"): data = data.replace(b"<head>", b"<head>" + HOOK + PROBE, 1)
        ctype = mimetypes.guess_type(name)[0] or "application/octet-stream"
        if enc is None and "gzip" in self.headers.get("Accept-Encoding", "") and not name.endswith(".png"):
            key = (p, os.path.getmtime(p))
            if key not in cache: cache[key] = gzip.compress(data, 6)
            data = cache[key]; enc = "gzip"
        self.send_response(200); self.send_header("Content-Type", ctype)
        if enc: self.send_header("Content-Encoding", enc)
        self.send_header("Content-Length", str(len(data))); self.send_header("Cache-Control", "no-store")
        self.end_headers(); self.wfile.write(data)
    def log_message(self, *a): pass
http.server.ThreadingHTTPServer(("127.0.0.1", port), H).serve_forever()
