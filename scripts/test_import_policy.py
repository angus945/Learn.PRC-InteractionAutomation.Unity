import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

spec = importlib.util.spec_from_file_location(
    'policy', Path(__file__).with_name('validate-unity-imports.py'))
policy = importlib.util.module_from_spec(spec)
spec.loader.exec_module(policy)

class ImportPolicyTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        for module in policy.MODULES:
            src = self.root / 'Assets' / 'CraftyRacoon' / module / 'src'
            src.mkdir(parents=True)
            asm = {
                'name': module,
                'references': [],
                'precompiledReferences': [],
                'defineConstraints': []
            }
            if module == 'Module.Communication.JsonRpc.WebSocket':
                asm['references'] = ['Module.Communication.JsonRpc']
                asm['precompiledReferences'] = ['Newtonsoft.Json.dll']
            (src / f'{module}.asmdef').write_text(json.dumps(asm))
            (src / 'Example.cs').write_text(
                'namespace Example { public class Value {} }')

        packages = self.root / 'Packages'
        packages.mkdir()
        (packages / 'manifest.json').write_text(json.dumps({
            'dependencies': {
                'com.unity.nuget.newtonsoft-json': '3.2.2'
            }
        }))

        self.core = (
            self.root / 'Assets' / 'CraftyRacoon' /
            'Module.Communication.JsonRpc')

    def tearDown(self):
        self.temp.cleanup()

    def errors(self):
        return policy.validate(self.root)[0]

    def test_source_first_stack_passes(self):
        self.assertEqual([], self.errors())

    def test_filescoped_namespace_rejected(self):
        (self.core / 'src' / 'Example.cs').write_text(
            'namespace Example;\npublic class Value {}')
        self.assertTrue(any('file-scoped' in e for e in self.errors()))

    def test_unscoped_source_rejected(self):
        test = self.core / 'other'
        test.mkdir()
        (test / 'Leak.cs').write_text('class Leak {}')
        self.assertTrue(any('assembly boundary' in e for e in self.errors()))

    def test_net_only_test_can_be_excluded(self):
        test = self.core / 'test'
        test.mkdir()
        (test / 'DotNet.Tests.asmdef').write_text(json.dumps({
            'name':'DotNet.Tests',
            'defineConstraints':['!UNITY_5_3_OR_NEWER']
        }))
        (test / 'Tests.cs').write_text(
            'namespace Modern;\nclass Test { string X = """raw"""; }')
        self.assertEqual([], self.errors())

    def test_communication_binary_rejected(self):
        plugin = self.root / 'Assets' / 'Plugins'
        plugin.mkdir()
        (plugin / 'Module.Communication.JsonRpc.WebSocket.dll').write_bytes(b'fixture')
        self.assertTrue(any('duplicates' in e for e in self.errors()))

    def test_generated_obj_rejected(self):
        obj = self.core / 'src' / 'obj'
        obj.mkdir()
        (obj / 'Generated.cs').write_text('global using System;')
        self.assertTrue(any('SDK artifacts' in e for e in self.errors()))

    def test_websocket_unity_exclusion_rejected(self):
        path = (
            self.root / 'Assets' / 'CraftyRacoon' /
            'Module.Communication.JsonRpc.WebSocket' / 'src' /
            'Module.Communication.JsonRpc.WebSocket.asmdef')
        asm = json.loads(path.read_text())
        asm['defineConstraints'] = ['!UNITY_5_3_OR_NEWER']
        path.write_text(json.dumps(asm))
        self.assertTrue(any('must compile in Unity' in e for e in self.errors()))

    def test_wrong_newtonsoft_reference_rejected(self):
        path = (
            self.root / 'Assets' / 'CraftyRacoon' /
            'Module.Communication.JsonRpc.WebSocket' / 'src' /
            'Module.Communication.JsonRpc.WebSocket.asmdef')
        asm = json.loads(path.read_text())
        asm['precompiledReferences'] = []
        path.write_text(json.dumps(asm))
        self.assertTrue(any('Newtonsoft.Json.dll' in e for e in self.errors()))

    def test_missing_newtonsoft_package_rejected(self):
        manifest = self.root / 'Packages' / 'manifest.json'
        manifest.write_text(json.dumps({'dependencies': {}}))
        self.assertTrue(any('newtonsoft-json' in e for e in self.errors()))

if __name__ == '__main__':
    unittest.main(verbosity=2)
