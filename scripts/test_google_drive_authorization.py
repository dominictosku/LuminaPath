"""OAuth helper checks without a browser, listening socket, or Google account."""
import base64
import hashlib
import importlib.util
import io
import json
import os
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import urllib.parse

spec = importlib.util.spec_from_file_location('drive_authorization', Path(__file__).with_name('authorize-google-drive.py'))
helper = importlib.util.module_from_spec(spec)
spec.loader.exec_module(helper)


class AuthorizationTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        self.client = self.root / 'oauth.json'
        self.client.write_text(json.dumps({'installed': {'client_id': 'client', 'client_secret': 'secret'}}))
        self.output = self.root / 'secrets'
        self.auth_query = {}
        self.rejected_state = False

    def browser(self, url):
        self.auth_query = urllib.parse.parse_qs(urllib.parse.urlsplit(url).query)
        return True

    def server(self, address, handler_type):
        self.assertEqual(('127.0.0.1', 8765), address)
        test = self

        class Server:
            def __enter__(self):
                return self

            def __exit__(self, *_):
                pass

            def handle_request(self):
                # A forged callback must not complete the flow; the genuine one follows.
                handler = object.__new__(handler_type)
                handler.send_response = lambda *_: None
                handler.send_header = lambda *_: None
                handler.end_headers = lambda: None
                handler.send_error = lambda *_: setattr(test, 'rejected_state', True)
                handler.wfile = io.BytesIO()
                handler.path = '/callback?code=code&state=wrong-state'
                handler.do_GET()
                handler.path = '/callback?' + urllib.parse.urlencode({'code': 'code', 'state': test.auth_query['state'][0]})
                handler.do_GET()

        return Server()

    def exchange(self, url, data, timeout):
        self.assertEqual('https://oauth2.googleapis.com/token', url)
        values = urllib.parse.parse_qs(data.decode())
        self.assertEqual('secret', values['client_secret'][0])
        self.assertEqual('http://127.0.0.1:8765/callback', values['redirect_uri'][0])
        challenge = base64.urlsafe_b64encode(hashlib.sha256(values['code_verifier'][0].encode()).digest()).rstrip(b'=').decode()
        self.assertEqual(challenge, self.auth_query['code_challenge'][0])
        return io.BytesIO(json.dumps({'refresh_token': 'private-refresh', 'scope': helper.SCOPE}).encode())

    def test_authorizes_with_pkce_and_state_and_saves_private_files(self):
        output = io.StringIO()
        with patch.object(helper, 'HTTPServer', self.server), patch.object(helper.webbrowser, 'open', self.browser), \
                patch.object(helper.urllib.request, 'urlopen', self.exchange), patch('sys.stdout', output):
            helper.authorize(self.client, self.output, 8765)
        self.assertTrue(self.rejected_state)
        self.assertEqual('private-refresh', (self.output / 'refresh_token.txt').read_text())
        self.assertEqual('secret', (self.output / 'client_secret.txt').read_text())
        self.assertNotIn('private-refresh', output.getvalue())
        self.assertNotIn('client_secret=', output.getvalue())
        if os.name != 'nt':
            self.assertEqual(0o600, (self.output / 'refresh_token.txt').stat().st_mode & 0o777)

    def test_missing_drive_scope_does_not_save_credentials(self):
        with patch.object(helper, 'HTTPServer', self.server), patch.object(helper.webbrowser, 'open', self.browser), \
                patch.object(helper.urllib.request, 'urlopen', return_value=io.BytesIO(b'{"refresh_token":"private","scope":"openid"}')), \
                patch('sys.stdout', io.StringIO()):
            with self.assertRaisesRegex(ValueError, 'not granted'):
                helper.authorize(self.client, self.output, 8765)
        self.assertFalse(self.output.exists())

    def test_existing_credentials_are_not_overwritten(self):
        self.output.mkdir()
        (self.output / 'refresh_token.txt').write_text('working-token')
        with self.assertRaisesRegex(ValueError, 'already contains'):
            helper.authorize(self.client, self.output, 8765)
        self.assertEqual('working-token', (self.output / 'refresh_token.txt').read_text())

    def test_web_client_requires_registered_loopback_redirect(self):
        self.client.write_text(json.dumps({'web': {'client_id': 'client', 'client_secret': 'secret', 'redirect_uris': []}}))
        with self.assertRaisesRegex(ValueError, 'Register http://127.0.0.1:8765/callback'):
            helper.authorize(self.client, self.output, 8765)

    def test_service_account_is_not_a_personal_drive_oauth_client(self):
        self.client.write_text(json.dumps({'type': 'service_account', 'client_email': 'service@example.invalid'}))
        with self.assertRaisesRegex(ValueError, 'not an API key or service account'):
            helper.authorize(self.client, self.output, 8765)


if __name__ == '__main__':
    unittest.main()
