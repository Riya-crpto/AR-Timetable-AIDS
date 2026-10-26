#!/usr/bin/env python3
"""
Dual HTTP/HTTPS local server for WebAR demonstration.
Supports both HTTP (port 8080) and secure HTTPS (port 8443) with auto-generated SSL certificate.
"""
import http.server
import socketserver
import ssl
import threading
import os
import socket

PORT_HTTP = 8080
PORT_HTTPS = 8443

def get_local_ip():
    s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    try:
        s.connect(('8.8.8.8', 1))
        ip = s.getsockname()[0]
    except Exception:
        ip = '10.112.111.148'
    finally:
        s.close()
    return ip

class CORSRequestHandler(http.server.SimpleHTTPRequestHandler):
    def end_headers(self):
        self.send_header('Access-Control-Allow-Origin', '*')
        self.send_header('Access-Control-Allow-Methods', 'GET, OPTIONS')
        self.send_header('Cache-Control', 'no-store, no-cache, must-revalidate')
        super().end_headers()

def run_http(web_dir):
    os.chdir(web_dir)
    with socketserver.TCPServer(("", PORT_HTTP), CORSRequestHandler) as httpd:
        httpd.serve_forever()

def run_https(web_dir):
    os.chdir(web_dir)
    cert_path = os.path.join(web_dir, 'cert.pem')
    key_path = os.path.join(web_dir, 'key.pem')

    context = ssl.SSLContext(ssl.PROTOCOL_TLS_SERVER)
    context.load_cert_chain(certfile=cert_path, keyfile=key_path)

    with socketserver.TCPServer(("", PORT_HTTPS), CORSRequestHandler) as httpsd:
        httpsd.socket = context.wrap_socket(httpsd.socket, server_side=True)
        httpsd.serve_forever()

if __name__ == '__main__':
    web_dir = os.path.dirname(os.path.abspath(__file__))
    ip = get_local_ip()

    print("=" * 60)
    print("   COLLEGE AR TIMETABLE // LIVE DUAL HTTP/HTTPS SERVER")
    print("=" * 60)
    print(f"\n[+] HTTP  Address:  http://{ip}:{PORT_HTTP}")
    print(f"[+] HTTPS Address:  https://{ip}:{PORT_HTTPS} (Enables Phone Camera!)")
    print("\nPress Ctrl+C to stop.\n")

    t_http = threading.Thread(target=run_http, args=(web_dir,), daemon=True)
    t_http.start()

    run_https(web_dir)
