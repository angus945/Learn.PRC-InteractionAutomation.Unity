import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

spec = importlib.util.spec_from_file_location('policy', Path(__file__).with_name('validate-unity-imports.py'))
policy = importlib.util.module_from_spec(spec)
spec.loader.exec_module(policy)

class ImportPolicyTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        for module in policy.MODULES:
            src = self.root / 'Assets' / 'CraftyRacoon' / module / 'src'
            src.mkdir(parents=True)
            asm = {'name': module, 'references': [], 'precompiledReferences': []}
            if module.endswith('WebSocket'):
                asm['defineConstraints'] = ['!UNITY_5_3_OR_NEWER']
            (src / f'{module}.asmdef').write_text(json.dumps(asm))
            (src / 'Example.cs').write_text('namespace Example { public class Value {} }')
        self.core = self.root / 'Assets' / 'CraftyRacoon' / policy.MODULES[0]
    def tearDown(self):
        self.temp.cleanup()
    def errors(self):
        return policy.validate(self.root)[0]
    def test_csharp9_passes(self):
        self.assertEqual([], self.errors())
    def test_filescoped_namespace_rejected(self):
        (self.core / 'src' / 'Example.cs').write_text('namespace Example;\npublic class Value {}')
        self.assertTrue(any('file-scoped' in e for e in self.errors()))
    def test_unscoped_test_rejected(self):
        test = self.core / 'test'
        test.mkdir()
        (test / 'Tests.cs').write_text('class Tests {}')
        self.assertTrue(any('assembly boundary' in e for e in self.errors()))
    def test_net_only_test_excluded(self):
        test = self.core / 'test'
        test.mkdir()
        (test / 'DotNet.Tests.asmdef').write_text(json.dumps({'name':'DotNet.Tests','defineConstraints':['!UNITY_5_3_OR_NEWER']}))
        (test / 'Tests.cs').write_text('namespace Modern;\nclass Test { string X = """raw"""; }')
        self.assertEqual([], self.errors())
    def test_duplicate_core_binary_rejected(self):
        plugin = self.root / 'Assets' / 'Plugins'
        plugin.mkdir()
        (plugin / 'Module.Communication.JsonRpc.dll').write_bytes(b'fixture')
        self.assertTrue(any('duplicates' in e for e in self.errors()))
    def test_generated_obj_rejected(self):
        obj = self.core / 'src' / 'obj'
        obj.mkdir()
        (obj / 'Generated.cs').write_text('global using System;')
        self.assertTrue(any('SDK artifacts' in e for e in self.errors()))
    def test_tilde_sample_not_imported(self):
        sample = self.core / 'samples~'
        sample.mkdir()
        (sample / 'Program.cs').write_text('namespace Modern;\nConsole.WriteLine("""hello""");')
        self.assertEqual([], self.errors())
    def test_precompiled_core_reference_rejected(self):
        path = self.core / 'src' / f'{policy.MODULES[0]}.asmdef'
        asm = json.loads(path.read_text())
        asm['precompiledReferences'] = ['Module.Communication.JsonRpc.dll']
        path.write_text(json.dumps(asm))
        self.assertTrue(any('source assembly' in e for e in self.errors()))

if __name__ == '__main__':
    unittest.main(verbosity=2)
