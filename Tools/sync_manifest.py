#!/usr/bin/env python3
"""Read-only modpack equality preflight. Does not assess MP compatibility."""
import argparse
import hashlib
import json
from pathlib import Path
import xml.etree.ElementTree as ET

SKIP_DIRS = {'.git', 'Source', 'Source_Referenced', 'source', 'src', 'obj', 'bin', '__pycache__'}
SKIP_FILES = {'.DS_Store', 'Thumbs.db'}


def digest(path):
    h = hashlib.sha256()
    with path.open('rb') as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b''):
            h.update(chunk)
    return h.hexdigest()


def files_at(root):
    return {str(p.relative_to(root)).replace('\\', '/'): digest(p)
            for p in sorted(root.rglob('*')) if p.is_file()
            and not any(part in SKIP_DIRS for part in p.relative_to(root).parts)
            and p.name not in SKIP_FILES}


def collect(config, roots, selections):
    doc = ET.parse(config).getroot()
    active = [n.text.lower() for n in doc.findall('activeMods/li')]
    index = {}
    for root in roots:
        for about in sorted(root.glob('*/About/About.xml')):
            pid = ET.parse(about).getroot().findtext('packageId', '').lower()
            index.setdefault(pid, []).append(about.parent.parent.resolve())
    entries, blockers = [], []
    for pid in active:
        candidates = index.get(pid, [])
        if pid in selections:
            selected = Path(selections[pid]).resolve()
            if selected not in candidates:
                raise ValueError(f'Selected directory is not an installed candidate for {pid}')
            candidates = [selected]
        if not candidates:
            blockers.append(f'Missing package: {pid}')
            entries.append({'package_id': pid, 'candidates': []})
            continue
        manifests = [files_at(p) for p in candidates]
        if len({json.dumps(m, sort_keys=True) for m in manifests}) > 1:
            blockers.append(f'Different installed copies; choose actual loaded directory: {pid}')
        # Include candidate content, not machine-specific absolute locations.
        unique = {json.dumps(m, sort_keys=True) for m in manifests}
        entries.append({'package_id': pid, 'candidates': [json.loads(m) for m in sorted(unique)]})
    return {'schema_version': 1, 'game_version': doc.findtext('version'),
            'active_order': active, 'packages': entries, 'blockers': blockers,
            'scope': 'Mod runtime content equality. Excludes sources/build output. Does not resolve LoadFolders or prove compatibility; hash the exact game build and shared gameplay configuration separately.'}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--mods-config', type=Path)
    parser.add_argument('--mods-root', action='append', type=Path, default=[])
    parser.add_argument('--selections', type=Path, help='JSON mapping package IDs to actual loaded directories')
    parser.add_argument('--output', type=Path)
    parser.add_argument('--compare', nargs=2, type=Path, metavar=('HOST', 'CLIENT'))
    args = parser.parse_args()
    if args.compare:
        host, client = [json.loads(p.read_text()) for p in args.compare]
        if host.get('schema_version') != 1 or client.get('schema_version') != 1:
            parser.error('Unsupported manifest schema')
        if host.get('blockers') or client.get('blockers'):
            print('BLOCKED: resolve missing or ambiguous packages first')
            return 2
        keys = ('game_version', 'active_order', 'packages')
        differences = [k for k in keys if host.get(k) != client.get(k)]
        print('DIFFERENT: ' + ', '.join(differences) if differences else 'MATCH: scoped mod content only; gameplay/config validation still required')
        return 1 if differences else 0
    if not args.mods_config or not args.mods_root or not args.output:
        parser.error('Provide --mods-config, --mods-root (repeatable), --output')
    selections = json.loads(args.selections.read_text()) if args.selections else {}
    result = collect(args.mods_config, args.mods_root, selections)
    args.output.write_text(json.dumps(result, indent=2) + '\n')
    print(f'{len(result["packages"])} packages; {len(result["blockers"])} blockers')
    return 2 if result['blockers'] else 0


if __name__ == '__main__':
    raise SystemExit(main())
