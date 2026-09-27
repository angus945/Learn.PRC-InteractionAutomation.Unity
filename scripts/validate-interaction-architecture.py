#!/usr/bin/env python3
"""Static source-identity/assembly graph checks, not a C# compiler or runtime test."""
from pathlib import Path
import json
import re
import sys

REQUIRED_MOUNTS = (
    'Workspace.InteractionAutomation',
    'Workspace.InteractionAutomation.Unity3D',
    'Workspace.Verification',
    'Framework.InteractionAutomation.Runner',
    'Framework.InteractionAutomation.Monkey',
    'Framework.InteractionAutomation.Runner.Unity3D',
    'Framework.InteractionAutomation.Monkey.Unity3D',
)
LEGACY = re.compile(r'\bModule\.InteractionAutomation\.(?:Runner|Monkey)(?:\b|\.)')
GUID = re.compile(r'^guid:\s*([a-fA-F0-9]{32})\s*$', re.MULTILINE)
ACTIVE_SUFFIXES = {'.cs', '.asmdef', '.csproj', '.sln', '.slnx', '.unity', '.prefab', '.asset'}
OWNED_PREFIXES = ('Module.InteractionAutomation.', 'Framework.InteractionAutomation.', 'Module.Verification.')


def imported(path, root):
    return not any(part.startswith('.') or part.endswith('~')
                   for part in path.relative_to(root).parts)


def graph_errors(definitions, guid_to_assembly):
    errors = []
    graph = {}
    for name, data in definitions.items():
        refs = []
        for reference in data.get('references', []):
            if reference.startswith('GUID:'):
                reference = guid_to_assembly.get(reference[5:].lower(), reference)
            refs.append(reference)
        refs += [value[:-4] for value in data.get('precompiledReferences', [])
                 if value.endswith('.dll')]
        graph[name] = [ref for ref in refs if ref in definitions]
        for ref in refs:
            if name.startswith('Module.') and ref.startswith('Framework.'):
                errors.append(f'{name}: Module must not depend on Framework {ref}')
            if ref.startswith(OWNED_PREFIXES) and ref not in definitions:
                errors.append(f'{name}: missing source assembly {ref}')
            if name.startswith('Framework.InteractionAutomation.') and '.Unity3D' not in name and '.Unity3D' in ref:
                errors.append(f'{name}: neutral Framework depends on Unity integration {ref}')
    visited, active = set(), []

    def visit(name):
        if name in active:
            errors.append('assembly dependency cycle: ' + ' -> '.join(active[active.index(name):] + [name]))
            return
        if name in visited:
            return
        active.append(name)
        for dependency in graph[name]:
            visit(dependency)
        active.pop()
        visited.add(name)

    for name in sorted(graph):
        visit(name)
    return errors


def validate(root, required_mounts=REQUIRED_MOUNTS):
    root = Path(root).resolve()
    assets = root / 'Assets'
    mount = assets / 'CraftyRacoon'
    errors, definitions, guid_to_assembly, seen_guids = [], {}, {}, {}
    for name in required_mounts:
        folder = mount / name
        if not folder.is_dir() or not any(folder.rglob('*.asmdef')):
            errors.append(f'{folder}: missing pinned source; initialize Project submodules')
        if name.startswith('Framework.') and not (folder / 'src' / f'{name}.asmdef').is_file():
            errors.append(f'{folder}: missing canonical Framework source assembly')
    if not assets.is_dir():
        errors.append(f'{assets}: missing Unity Assets directory')
        return errors

    for path in sorted(assets.rglob('*')):
        if not path.is_file() or not imported(path, assets):
            continue
        if path.suffix == '.meta':
            match = GUID.search(path.read_text(encoding='utf-8-sig'))
            if match:
                guid = match.group(1).lower()
                if guid in seen_guids:
                    errors.append(f'{path}: duplicate Unity GUID also used by {seen_guids[guid]}')
                seen_guids[guid] = path
        if path.suffix in ACTIVE_SUFFIXES:
            text = path.read_text(encoding='utf-8-sig')
            if LEGACY.search(text):
                errors.append(f'{path}: legacy Runner/Monkey Module identity on active source surface')
        if path.suffix != '.asmdef':
            continue
        try:
            data = json.loads(path.read_text(encoding='utf-8-sig'))
            name = data['name']
            if name != path.stem:
                errors.append(f'{path}: noncanonical assembly filename/name')
            if name in definitions:
                errors.append(f'{path}: duplicate assembly {name}')
            definitions[name] = data
            meta = Path(str(path) + '.meta')
            if meta.is_file():
                match = GUID.search(meta.read_text(encoding='utf-8-sig'))
                if match:
                    guid_to_assembly[match.group(1).lower()] = name
        except (ValueError, KeyError, TypeError) as error:
            errors.append(f'{path}: invalid asmdef: {error}')
    for path in root.glob('*.slnx'):
        if LEGACY.search(path.read_text(encoding='utf-8-sig')):
            errors.append(f'{path}: legacy assembly in Project solution')
    errors.extend(graph_errors(definitions, guid_to_assembly))
    return errors


if __name__ == '__main__':
    project = Path(sys.argv[1]) if len(sys.argv) > 1 else Path(__file__).resolve().parents[1]
    issues = validate(project)
    for issue in issues:
        print('ERROR: ' + issue, file=sys.stderr)
    print(f'Interaction architecture static check: {len(issues)} issue(s).')
    print('This does not establish C# compilation, Unity serialization loading or runtime acceptance.')
    sys.exit(1 if issues else 0)
