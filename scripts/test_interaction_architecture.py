#!/usr/bin/env python3
"""Negative and positive fixtures for the static architecture checker itself."""
from pathlib import Path
import importlib.util
import json
import tempfile
import unittest

SPEC = importlib.util.spec_from_file_location(
    'interaction_architecture', Path(__file__).with_name('validate-interaction-architecture.py'))
CHECKER = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(CHECKER)


class ArchitectureTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        (self.root / 'Assets').mkdir()

    def write(self, relative, text):
        path = self.root / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(text, encoding='utf-8')
        return path

    def assembly(self, name, references=(), guid=None, suffix=''):
        path = self.write(f'Assets/CraftyRacoon/{name}{suffix}/src/{name}.asmdef',
                          json.dumps({'name': name, 'references': list(references)}))
        if guid:
            self.write(str(path.relative_to(self.root)) + '.meta', f'fileFormatVersion: 2\nguid: {guid}\n')
        return path

    def issues(self):
        return CHECKER.validate(self.root, required_mounts=())

    def test_valid_framework_to_module(self):
        self.assembly('Module.Test.Input')
        self.assembly('Framework.Test.Runner', ['Module.Test.Input'])
        self.assertEqual([], self.issues())

    def test_module_to_framework_is_rejected(self):
        self.assembly('Framework.Test.Runner')
        self.assembly('Module.Test.Input', ['Framework.Test.Runner'])
        self.assertTrue(any('Module must not depend' in issue for issue in self.issues()))

    def test_guid_reference_cannot_hide_reverse_dependency(self):
        guid = 'a' * 32
        self.assembly('Framework.Test.Runner', guid=guid)
        self.assembly('Module.Test.Input', ['GUID:' + guid])
        self.assertTrue(any('Module must not depend' in issue for issue in self.issues()))

    def test_framework_cycle_is_rejected(self):
        self.assembly('Framework.Test.A', ['Framework.Test.B'])
        self.assembly('Framework.Test.B', ['Framework.Test.A'])
        self.assertTrue(any('dependency cycle' in issue for issue in self.issues()))

    def test_legacy_namespace_is_rejected(self):
        self.write('Assets/Probe.cs', 'using Module.InteractionAutomation.Runner;')
        self.assertTrue(any('legacy' in issue for issue in self.issues()))

    def test_legacy_serialized_type_is_rejected(self):
        self.write('Assets/Probe.unity', 'm_EditorClassIdentifier: Module.InteractionAutomation.Runner.Unity3D::OldType')
        self.assertTrue(any('legacy' in issue for issue in self.issues()))

    def test_duplicate_assembly_is_rejected(self):
        self.assembly('Module.Test.Input')
        self.assembly('Module.Test.Input', suffix='.Copy')
        self.assertTrue(any('duplicate assembly' in issue for issue in self.issues()))

    def test_missing_pin_checkout_is_rejected(self):
        issues = CHECKER.validate(self.root, required_mounts=('Framework.InteractionAutomation.Runner',))
        self.assertTrue(any('missing pinned source' in issue for issue in issues))

    def test_duplicate_guid_is_rejected(self):
        self.assembly('Module.Test.A', guid='b' * 32)
        self.assembly('Module.Test.B', guid='b' * 32)
        self.assertTrue(any('duplicate Unity GUID' in issue for issue in self.issues()))

    def test_missing_owned_dependency_is_rejected(self):
        self.assembly('Framework.InteractionAutomation.Runner', ['Module.InteractionAutomation.Timing'])
        self.assertTrue(any('missing source assembly' in issue for issue in self.issues()))

    def test_neutral_framework_cannot_depend_on_unity(self):
        self.assembly('Framework.InteractionAutomation.Runner', ['Module.InteractionAutomation.Timing.Unity3D'])
        self.assembly('Module.InteractionAutomation.Timing.Unity3D')
        self.assertTrue(any('neutral Framework depends' in issue for issue in self.issues()))

    def test_historical_markdown_is_not_active_surface(self):
        self.write('Assets/HISTORY.md', 'Module.InteractionAutomation.Runner')
        self.assertEqual([], self.issues())


if __name__ == '__main__':
    unittest.main(verbosity=2)
