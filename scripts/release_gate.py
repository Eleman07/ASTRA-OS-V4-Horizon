#!/usr/bin/env python3
"""Fail-closed gate. Positive evidence is necessary but still needs human review."""
from __future__ import annotations
import argparse, hashlib, json, re, sys
from pathlib import Path

REQUIRED = (
    'win11_home_build_ui', 'win11_pro_build_ui', 'core_smoke_tests',
    'ai_real_gguf_offline', 'plugin_adversarial_tests', 'webview2_3d_tests',
    'iso_uefi_boot', 'installer_uninstall', 'benchmark_targets',
    'dependency_security_review', 'signed_binaries', 'legal_license_review',
)

def evaluate(evidence_path: Path) -> tuple[bool, list[str]]:
    if not evidence_path.is_file():
        return False, ['release evidence file is missing; no stable release is authorized']
    try:
        payload = json.loads(evidence_path.read_text(encoding='utf-8'))
    except (OSError, ValueError) as exc:
        return False, ['release evidence is unreadable or invalid: ' + type(exc).__name__]
    records = payload.get('gates', {})
    problems = []
    for key in REQUIRED:
        entry = records.get(key)
        if not isinstance(entry, dict) or entry.get('status') != 'passed':
            problems.append(f'{key}: no verified pass')
            continue
        relative = entry.get('artifact', '')
        digest = entry.get('sha256', '')
        if not isinstance(relative, str) or not relative or not isinstance(digest, str) or not re.fullmatch(r'[a-fA-F0-9]{64}', digest):
            problems.append(f'{key}: evidence path/hash missing')
            continue
        path = (evidence_path.parent / relative).resolve()
        if not path.is_relative_to(evidence_path.parent.resolve()) or not path.is_file() or path.stat().st_size < 20:
            problems.append(f'{key}: evidence file absent or outside evidence folder')
            continue
        if hashlib.sha256(path.read_bytes()).hexdigest().lower() != digest.lower():
            problems.append(f'{key}: evidence SHA-256 mismatch')
    return not problems, problems

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--evidence', type=Path, default=Path(__file__).resolve().parents[1] / 'docs/evidence/release-evidence.json')
    parser.add_argument('--expect-blocked', action='store_true', help='CI check of default fail-closed state')
    args = parser.parse_args()
    okay, issues = evaluate(args.evidence)
    print('Stable gate:', 'PASS (subject to independent human review)' if okay else 'BLOCKED')
    for issue in issues: print(' -', issue)
    sys.exit(0 if (not okay if args.expect_blocked else okay) else 1)
