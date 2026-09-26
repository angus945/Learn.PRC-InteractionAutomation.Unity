#!/usr/bin/env python3
"""Check this Project's Unity source/import policy; this is not a C# compiler."""
from pathlib import Path
import json
import re
import sys

MODULES = (
    'Module.Communication.JsonRpc',
    'Module.Communication.JsonRpc.WebSocket',
    'Module.Communication.Unity3D',
    'Workspace.InteractionAutomation',
)
UNSUPPORTED = (
    ('file-scoped namespace (C# 10)', r'^\s*namespace\s+[\w.]+\s*;'),
    ('global using (C# 10)', r'^\s*global\s+using\s+'),
    ('record struct (C# 10)', r'\brecord\s+struct\s+'),
    ('raw string literal (C# 11)', r'"{3,}'),
    ('required member (C# 11)', r'^\s*(?:public|internal|protected|private)\s+required\s+'),
)

def imported(path, root):
    return not any(part.startswith('.') or part.endswith('~')
                   for part in path.relative_to(root).parts)

def validate(root):
    root = Path(root).resolve()
    mount = root / 'Assets' / 'CraftyRacoon'
    errors = []
    active = excluded = 0
    definitions = {}
    sources = []

    for module in MODULES:
        folder = mount / module
        if not folder.is_dir() or not any(folder.iterdir()):
            errors.append(f'{folder}: initialize the pinned submodule first')
            continue

        for path in folder.rglob('*'):
            if not imported(path, folder):
                continue
            if path.is_dir() and path.name.lower() in ('bin', 'obj'):
                errors.append(f'{path}: SDK artifacts inside Assets; review/remove generated output locally')
            if any(part.lower() in ('bin', 'obj') for part in path.relative_to(folder).parts):
                continue

            if path.suffix == '.asmdef':
                try:
                    data = json.loads(path.read_text(encoding='utf-8-sig'))
                    if data['name'] != path.stem:
                        errors.append(f'{path}: noncanonical assembly name')
                    if path.parent in definitions:
                        errors.append(f'{path}: multiple asmdefs in one folder')
                    definitions[path.parent] = data

                    precompiled = data.get('precompiledReferences', [])
                    for forbidden in (
                        'Module.Communication.JsonRpc.dll',
                        'Module.Communication.JsonRpc.WebSocket.dll',
                        'Module.Communication.Unity3D.dll',
                    ):
                        if forbidden in precompiled:
                            errors.append(f'{path}: Communication modules must be source assemblies')
                except (ValueError, KeyError) as error:
                    errors.append(f'{path}: invalid asmdef: {error}')
            elif path.suffix == '.cs':
                sources.append(path)

    for path in sources:
        parent = path.parent
        definition = None
        while parent != root and root in parent.parents:
            if parent in definitions:
                definition = definitions[parent]
                break
            parent = parent.parent

        if definition is None:
            errors.append(f'{path}: no explicit assembly boundary (would leak into Assembly-CSharp)')
            continue

        if '!UNITY_5_3_OR_NEWER' in definition.get('defineConstraints', []):
            excluded += 1
            continue

        active += 1
        text = path.read_text(encoding='utf-8-sig')
        for label, pattern in UNSUPPORTED:
            if re.search(pattern, text, re.MULTILINE):
                errors.append(f'{path}: {label}')

    web_asm_path = mount / 'Module.Communication.JsonRpc.WebSocket' / 'src' / 'Module.Communication.JsonRpc.WebSocket.asmdef'
    if web_asm_path.exists():
        web = json.loads(web_asm_path.read_text(encoding='utf-8-sig'))
        if 'Module.Communication.JsonRpc' not in web.get('references', []):
            errors.append(f'{web_asm_path}: WebSocket source must reference JsonRpc source')
        if web.get('precompiledReferences', []) != ['Newtonsoft.Json.dll']:
            errors.append(f'{web_asm_path}: WebSocket source must reference only Newtonsoft.Json.dll')
        if '!UNITY_5_3_OR_NEWER' in web.get('defineConstraints', []):
            errors.append(f'{web_asm_path}: WebSocket production source must compile in Unity')

    manifest_path = root / 'Packages' / 'manifest.json'
    if manifest_path.exists():
        manifest = json.loads(manifest_path.read_text(encoding='utf-8-sig'))
        if manifest.get('dependencies', {}).get('com.unity.nuget.newtonsoft-json') != '3.2.2':
            errors.append(f'{manifest_path}: Project must own com.unity.nuget.newtonsoft-json 3.2.2')

    assets = root / 'Assets'
    if assets.exists():
        forbidden_dlls = {
            'Module.Communication.JsonRpc.dll',
            'Module.Communication.JsonRpc.WebSocket.dll',
            'Module.Communication.Unity3D.dll',
        }
        for dll in assets.rglob('*.dll'):
            if imported(dll, assets) and dll.name in forbidden_dlls:
                errors.append(f'{dll}: duplicates a source-imported Communication module')

    return errors, active, excluded

if __name__ == '__main__':
    project = Path(sys.argv[1]) if len(sys.argv) > 1 else Path(__file__).resolve().parents[1]
    issues, active_count, excluded_count = validate(project)
    for issue in issues:
        print('ERROR: ' + issue, file=sys.stderr)
    print(f'Static import scan: {active_count} active C# files; {excluded_count} intentionally excluded .NET files.')
    print('This does not establish C# compilation, package resolution or Unity runtime compatibility.')
    sys.exit(1 if issues else 0)
