#!/usr/bin/env python3
"""Check deployment contracts using Compose itself, without starting containers."""
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[1]
REQUIRED = {
    'POSTGRES_PASSWORD': 'compose-test-database',
    'LUMINAPATH_ADMIN_EMAIL': 'compose@example.invalid',
    'LUMINAPATH_ADMIN_PASSWORD': 'Compose-Test-Admin1!',
}


class ComposeTests(unittest.TestCase):
    def render(self, overrides=None, files=(), profiles=(), shell=None):
        with tempfile.TemporaryDirectory(prefix='luminapath-compose-') as directory:
            folder = Path(directory)
            for name in ('compose.yml', 'compose.dev.yml', 'compose.secrets.yml', 'compose.vllm.yml'):
                shutil.copyfile(ROOT / name, folder / name)
            values = REQUIRED | (overrides or {})
            (folder / '.env').write_text(''.join(f'{key}={value}\n' for key, value in values.items()))
            command = ['docker', 'compose', '-f', 'compose.yml']
            for file in files:
                command += ['-f', file]
            for profile in profiles:
                command += ['--profile', profile]
            command += ['config', '--format', 'json']
            # Never consume the developer's .env or shell configuration.
            environment = {key: os.environ[key] for key in ('PATH', 'HOME', 'DOCKER_CONFIG') if key in os.environ}
            environment.update(shell or {})
            result = subprocess.run(command, cwd=folder, env=environment, text=True, capture_output=True)
            if result.returncode:
                self.fail(f'Compose validation failed: {result.stderr}')
            return json.loads(result.stdout)

    def test_minimal_deployment_and_persistent_data(self):
        config = self.render()
        services = config['services']
        self.assertEqual({'api', 'frontend', 'db', 'redis'}, set(services))
        api = services['api']['environment']
        self.assertEqual('redis:6379', api['Redis__ConnectionString'])
        self.assertEqual('true', api['Database__RunMigrationsOnStartup'])
        self.assertEqual(REQUIRED['POSTGRES_PASSWORD'], api['POSTGRES_PASSWORD'])
        self.assertEqual('http://api:8080', services['frontend']['environment']['LUMINAPATH_API_PROXY_TARGET'])
        self.assertEqual('4200', services['frontend']['ports'][0]['published'])
        for name in services:
            self.assertNotIn('container_name', services[name])
            self.assertNotIn('build', services[name])
        for name in ('api', 'db', 'redis'):
            self.assertFalse(services[name].get('ports'))
        for name in ('postgres', 'data', 'redis'):
            self.assertEqual(f'luminapath_luminapath-{name}', config['volumes'][f'luminapath-{name}']['name'])
        self.assertTrue(any(v['target'] == '/app/App_Data' and v['source'] == 'luminapath-data'
                            for v in services['api']['volumes']))
        self.assertNotIn('Auth__RequireAdminApproval', api)
        self.assertNotIn('Storage__Provider', api)

    def test_optional_backend_settings_reach_only_api(self):
        overrides = {
            'Auth__RequireAdminApproval': 'false',
            'Email__Smtp__Host': 'smtp.example.invalid',
            'GOOGLE_CLIENT_ID': 'test-client',
            'GOOGLE_CLIENT_SECRET': 'test-secret',
            'Database__RunMigrationsOnStartup': 'false',
            'Redis__ConnectionString': 'cache.example.invalid:6380',
        }
        services = self.render(overrides)['services']
        for key, value in overrides.items():
            self.assertEqual(value, services['api']['environment'][key])
            for service in ('frontend', 'db', 'redis'):
                self.assertNotIn(key, services[service].get('environment', {}))
        self.assertNotIn('LUMINAPATH_ADMIN_PASSWORD', services['db']['environment'])

    def test_frontend_overrides_are_forwarded(self):
        values = {'LUMINAPATH_API_ENDPOINT': '/custom-api', 'LUMINAPATH_CSP': "default-src 'self'", 'FRONTEND_HTTP_PORT': '4300'}
        frontend = self.render(values)['services']['frontend']
        self.assertEqual('/custom-api', frontend['environment']['LUMINAPATH_API_ENDPOINT'])
        self.assertEqual(values['LUMINAPATH_CSP'], frontend['environment']['LUMINAPATH_CSP'])
        self.assertEqual('4300', frontend['ports'][0]['published'])

    def test_shared_database_settings_use_the_same_shell_overrides(self):
        values = {'POSTGRES_PASSWORD': 'shell-password', 'POSTGRES_DB': 'custom-db', 'POSTGRES_USER': 'custom-user'}
        services = self.render(shell=values)['services']
        for key, value in values.items():
            self.assertEqual(value, services['api']['environment'][key])
            self.assertEqual(value, services['db']['environment'][key])

    def test_matching_versions_and_independent_image_override(self):
        services = self.render({'LUMINAPATH_VERSION': '1.2.3'})['services']
        for name in ('api', 'frontend'):
            self.assertEqual(f'sekijuo/luminapath-{name}:1.2.3', services[name]['image'])
        services = self.render({'LUMINAPATH_VERSION': '1.2.3', 'LUMINAPATH_API_IMAGE': 'custom/api:test'})['services']
        self.assertEqual('custom/api:test', services['api']['image'])
        self.assertEqual('sekijuo/luminapath-frontend:1.2.3', services['frontend']['image'])

    def test_development_builds_and_host_ports(self):
        services = self.render({'POSTGRES_PORT': '5433', 'REDIS_PORT': '6380', 'LUMINAPATH_VERSION': '1.2.3'}, files=('compose.dev.yml',))['services']
        for name in ('api', 'frontend'):
            self.assertIn('build', services[name])
            self.assertEqual('build', services[name]['pull_policy'])
            self.assertEqual(f'luminapath-{name}:local', services[name]['image'])
        for name in ('api', 'db', 'redis'):
            self.assertEqual('127.0.0.1', services[name]['ports'][0]['host_ip'])
        self.assertEqual('5433', services['db']['ports'][0]['published'])
        self.assertEqual('5432', services['api']['environment']['Database__Port'])
        self.assertEqual('redis:6379', services['api']['environment']['Redis__ConnectionString'])

    def test_secrets_and_combined_development_overlay(self):
        for files in [('compose.secrets.yml',), ('compose.dev.yml', 'compose.secrets.yml')]:
            with self.subTest(files=files):
                config = self.render({'POSTGRES_PASSWORD': '', 'LUMINAPATH_ADMIN_PASSWORD': ''}, files=files)
                services = config['services']
                self.assertEqual('', services['db']['environment']['POSTGRES_PASSWORD'])
                self.assertEqual('', services['api']['environment']['LUMINAPATH_ADMIN_PASSWORD'])
                self.assertEqual('/run/secrets/luminapath_postgres_password', services['db']['environment']['POSTGRES_PASSWORD_FILE'])
                self.assertEqual('/run/secrets/luminapath_admin_password', services['api']['environment']['Admin__PasswordFile'])
                self.assertEqual(2, len(services['api']['secrets']))
                self.assertEqual(1, len(services['db']['secrets']))
                self.assertNotIn('secrets', services['frontend'])
                self.assertTrue(config['secrets']['luminapath_postgres_password']['file'].endswith('/secrets/postgres_password.txt'))

    def test_tools_and_proxy_profiles(self):
        services = self.render(profiles=('tools', 'proxy'))['services']
        self.assertIn('nginx', services)
        self.assertEqual('127.0.0.1', services['pgadmin']['ports'][0]['host_ip'])
        self.assertNotIn('LUMINAPATH_ADMIN_PASSWORD', services['pgadmin']['environment'])

    def test_gpu_profiles_and_shared_model(self):
        for profile in ('amd', 'nvidia'):
            with self.subTest(profile=profile):
                config = self.render({'VLLM_MODEL': 'test/model', 'VLLM_API_KEY': 'test-key'}, files=('compose.vllm.yml',), profiles=(profile,))
                services = config['services']
                vllm = services[f'vllm-{profile}']
                self.assertEqual(5, len(services))
                self.assertIn('test/model', vllm['command'])
                self.assertIn('test-key', vllm['command'])
                self.assertEqual('test/model', services['api']['environment']['OpenAi__Model'])
                self.assertEqual('http://vllm:8000/v1', services['api']['environment']['OpenAi__BaseUrl'])
                self.assertIn('vllm', vllm['networks']['default']['aliases'])
                self.assertNotIn('container_name', vllm)
                self.assertEqual('luminapath_luminapath-vllm-hf-cache', config['volumes']['luminapath-vllm-hf-cache']['name'])


if __name__ == '__main__':
    unittest.main(verbosity=2)
