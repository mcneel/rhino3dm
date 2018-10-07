import os, sys, BaseHTTPServer, SimpleHTTPServer

dirname = os.path.dirname(os.path.abspath(__file__))
artifacts_dir = os.path.join(dirname, "artifacts")
os.chdir(artifacts_dir)

class ExtHandler(SimpleHTTPServer.SimpleHTTPRequestHandler):
    """content-type extension for tunnelx configuration files"""
    def guess_type(self, path):
        mimetype = SimpleHTTPServer.SimpleHTTPRequestHandler.guess_type(self, path)
        if mimetype == 'application/octet-stream':
            if path.endswith('wasm'):
                mimetype = 'application/wasm'

        return mimetype

HandlerClass = ExtHandler
ServerClass  = BaseHTTPServer.HTTPServer
Protocol     = "HTTP/1.0"

if sys.argv[1:]:
    port = int(sys.argv[1])
else:
    port = 8080

server_address = ('localhost', port)

HandlerClass.protocol_version = Protocol
httpd = ServerClass(server_address, HandlerClass)

socketname = httpd.socket.getsockname()

print "Serving HTTP on", socketname[0], "port", socketname[1], "..."

httpd.serve_forever()
