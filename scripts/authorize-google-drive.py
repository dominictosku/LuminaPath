#!/usr/bin/env python3
"""Authorize private Drive storage locally; never print tokens or client secrets."""
import argparse
import base64
import hashlib
from http.server import BaseHTTPRequestHandler, HTTPServer
import json
import os
from pathlib import Path
import secrets
import time
import urllib.error
import urllib.parse
import urllib.request
import webbrowser

SCOPE = 'https://www.googleapis.com/auth/drive.file'


def write_secret(path, value):
    # Exclusive creation avoids overwriting a working authorization accidentally.
    descriptor = os.open(path, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o600)
    with os.fdopen(descriptor, 'w', encoding='utf-8') as output:
        output.write(value)


def authorize(client_json, output_dir, port):
    credentials = json.loads(client_json.read_text(encoding='utf-8'))
    client = credentials.get('installed') or credentials.get('web')
    if not client or not client.get('client_id') or not client.get('client_secret'):
        raise ValueError('Use a downloaded OAuth Desktop or Web client JSON, not an API key or service account.')
    redirect = f'http://127.0.0.1:{port}/callback'
    if 'web' in credentials and redirect not in client.get('redirect_uris', []):
        raise ValueError(f'Register {redirect} as an authorized redirect URI on this Web client and download its JSON again.')
    names = ['client_id.txt', 'client_secret.txt', 'refresh_token.txt']
    if any((output_dir / name).exists() for name in names):
        raise ValueError('Output already contains credentials. Use a new directory for reauthorization.')
    state = secrets.token_urlsafe(32)
    verifier = secrets.token_urlsafe(64)
    challenge = base64.urlsafe_b64encode(hashlib.sha256(verifier.encode()).digest()).rstrip(b'=').decode()
    result = {}

    class Callback(BaseHTTPRequestHandler):
        def log_message(self, *_):
            pass  # Callback URLs contain authorization codes.

        def do_GET(self):
            parsed = urllib.parse.urlsplit(self.path)
            query = urllib.parse.parse_qs(parsed.query)
            if parsed.path != '/callback' or not secrets.compare_digest(query.get('state', [''])[0], state):
                self.send_error(400, 'Invalid OAuth callback')
                return
            result['code'] = query.get('code', [None])[0]
            result['error'] = 'error' in query or not result['code']
            self.send_response(200)
            self.send_header('Content-Type', 'text/plain; charset=utf-8')
            self.end_headers()
            self.wfile.write(b'Authorization response received. Return to the terminal; you can close this tab.')

    with HTTPServer(('127.0.0.1', port), Callback) as server:
        server.timeout = 1
        url = 'https://accounts.google.com/o/oauth2/v2/auth?' + urllib.parse.urlencode({
            'client_id': client['client_id'], 'redirect_uri': redirect, 'response_type': 'code',
            'scope': SCOPE, 'access_type': 'offline', 'prompt': 'consent', 'state': state,
            'code_challenge': challenge, 'code_challenge_method': 'S256',
        })
        print('Open this authorization URL on this computer:\n' + url)
        webbrowser.open(url)
        deadline = time.monotonic() + 300
        while not result and time.monotonic() < deadline:
            server.handle_request()
    if not result or result.get('error'):
        raise ValueError('Authorization was denied or timed out. No credentials were saved.')
    data = urllib.parse.urlencode({
        'client_id': client['client_id'], 'client_secret': client['client_secret'],
        'code': result['code'], 'code_verifier': verifier, 'redirect_uri': redirect,
        'grant_type': 'authorization_code',
    }).encode()
    try:
        with urllib.request.urlopen('https://oauth2.googleapis.com/token', data=data, timeout=30) as response:
            tokens = json.load(response)
    except urllib.error.HTTPError as error:
        raise ValueError(f'Token exchange failed (HTTP {error.code}); check the OAuth client and redirect URI.') from None
    if not tokens.get('refresh_token'):
        raise ValueError('Google did not return a refresh token. Reauthorize with offline consent.')
    if SCOPE not in tokens.get('scope', '').split():
        raise ValueError('Drive file access was not granted. No credentials were saved.')
    output_dir.mkdir(parents=True, exist_ok=True, mode=0o700)
    for name, value in zip(names, [client['client_id'], client['client_secret'], tokens['refresh_token']]):
        write_secret(output_dir / name, value)
    print(f'Credentials saved to {output_dir}. Transfer them securely to your VPS; do not commit them.')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--client-json', required=True, type=Path)
    parser.add_argument('--output-dir', type=Path, default=Path('secrets/google-drive'))
    parser.add_argument('--port', type=int, default=8765)
    args = parser.parse_args()
    try:
        authorize(args.client_json, args.output_dir, args.port)
    except (ValueError, OSError) as error:
        # Do not emit HTTP bodies, request data, tokens or full exception tracebacks.
        parser.exit(1, f'Authorization failed: {error if isinstance(error, ValueError) else type(error).__name__}\n')


if __name__ == '__main__':
    main()
