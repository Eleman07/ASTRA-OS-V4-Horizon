#!/usr/bin/env python3
"""Read-only Markdown vault audit. It reports issues; it never edits notes."""

from __future__ import annotations

import argparse
import json
import re
import sys
from collections import defaultdict
from pathlib import Path
from typing import Any

WIKILINK = re.compile(r"\[\[([^\]]+)\]\]")
EXCLUDED = {".git", ".obsidian", ".trash", "node_modules", ".local-llm-hub"}


def markdown_files(root: Path):
    for path in root.rglob("*.md"):
        try:
            relative = path.relative_to(root)
            if path.is_symlink() or any(part in EXCLUDED for part in relative.parts):
                continue
            resolved = path.resolve(strict=True)
            resolved.relative_to(root)
        except (OSError, ValueError):
            continue
        yield path


def yaml_available() -> bool:
    try:
        import yaml  # type: ignore[import-not-found]  # noqa: F401
    except ImportError:
        return False
    return True


def frontmatter(text: str, has_yaml: bool) -> tuple[dict[str, Any] | None, str]:
    lines = text.splitlines()
    if not lines or lines[0].strip() != "---":
        return None, "missing frontmatter"
    closing = next((i for i in range(1, len(lines)) if lines[i].strip() in {"---", "..."}), None)
    if closing is None:
        return None, "frontmatter is not closed"
    block = "\n".join(lines[1:closing])
    if not has_yaml:
        return None, "YAML syntax not checked (install PyYAML to enable parsing)"
    import yaml  # type: ignore[import-not-found]
    try:
        parsed = yaml.safe_load(block) if block.strip() else {}
    except Exception as exc:  # PyYAML exception types vary by version.
        return None, f"invalid YAML: {exc}"
    if parsed is None:
        parsed = {}
    if not isinstance(parsed, dict):
        return None, "frontmatter root must be a YAML mapping"
    return parsed, ""


def resolve_link(root: Path, source: Path, raw: str) -> Path | None:
    target = raw.split("|", 1)[0].split("#", 1)[0].strip()
    if not target:
        return source
    target_path = Path(target)
    candidates = [root / target_path] if target_path.is_absolute() else [source.parent / target_path, root / target_path]
    for candidate in candidates:
        variants = [candidate]
        if not candidate.suffix:
            variants.extend((candidate.with_suffix(".md"), candidate / "index.md"))
        for variant in variants:
            try:
                resolved = variant.resolve(strict=True)
                resolved.relative_to(root)
                if resolved.is_file() and (resolved.suffix.lower() == ".md" or variant == candidate):
                    return resolved
            except (OSError, ValueError):
                continue
    return None


def without_fenced_code(text: str) -> str:
    result: list[str] = []
    fence: str | None = None
    for line in text.splitlines():
        marker = re.match(r"^\s*(`{3,}|~{3,})", line)
        if fence is None:
            if marker:
                fence = marker.group(1)[0]
                continue
            result.append(line)
        elif marker and marker.group(1)[0] == fence:
            fence = None
    return "\n".join(result)


def audit(root: Path, required: list[str]) -> dict[str, Any]:
    notes = list(markdown_files(root))
    has_yaml = yaml_available()
    by_stem: dict[str, list[Path]] = defaultdict(list)
    for note in notes:
        by_stem[note.stem.casefold()].append(note)

    issues: list[dict[str, str]] = []
    for note in notes:
        relative = note.relative_to(root).as_posix()
        try:
            text = note.read_text(encoding="utf-8-sig")
        except (OSError, UnicodeError) as exc:
            issues.append({"severity": "error", "file": relative, "check": "read", "detail": str(exc)})
            continue

        data, frontmatter_error = frontmatter(text, has_yaml)
        if data is not None:
            for field in required:
                if field not in data or data[field] in (None, "", []):
                    issues.append({"severity": "warning", "file": relative, "check": "required field", "detail": f"Missing or empty frontmatter field: {field}"})
            date_value = data.get("date")
            is_template_date = isinstance(date_value, str) and re.fullmatch(r"\{\{date(?::[^{}]+)?\}\}", date_value)
            if date_value is not None and not is_template_date and not re.fullmatch(r"\d{4}-\d{2}-\d{2}", str(date_value)):
                issues.append({"severity": "warning", "file": relative, "check": "date", "detail": f"Expected ISO YYYY-MM-DD, got: {date_value}"})
        elif required or frontmatter_error not in {"missing frontmatter"}:
            issues.append({"severity": "needs-review", "file": relative, "check": "frontmatter", "detail": frontmatter_error})
        # Ignore fenced code examples so sample links are not reported as broken.
        body = without_fenced_code(text)
        for match in WIKILINK.finditer(body):
            raw = match.group(1)
            target = resolve_link(root, note, raw)
            if target is None:
                line = body.count("\n", 0, match.start()) + 1
                issues.append({"severity": "warning", "file": relative, "check": "wiki link", "detail": f"Line {line}: unresolved [[{raw}]]"})

    for paths in by_stem.values():
        distinct = {p.resolve() for p in paths}
        if len(distinct) > 1:
            issues.append({"severity": "needs-review", "file": ", ".join(sorted(p.relative_to(root).as_posix() for p in distinct)), "check": "duplicate title", "detail": "Multiple notes share the same filename stem; confirm links target the intended note."})

    return {
        "root": str(root),
        "notes_scanned": len(notes),
        "yaml_parser_available": has_yaml,
        "read_only": True,
        "issues": issues,
        "summary": {
            "errors": sum(i["severity"] == "error" for i in issues),
            "warnings": sum(i["severity"] == "warning" for i in issues),
            "needs_review": sum(i["severity"] == "needs-review" for i in issues),
        },
    }


def main() -> int:
    for stream in (sys.stdout, sys.stderr):
        reconfigure = getattr(stream, "reconfigure", None)
        if callable(reconfigure):
            reconfigure(encoding="utf-8", errors="replace")

    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("vault", type=Path, help="Obsidian vault root")
    parser.add_argument("--required-field", action="append", default=[], dest="required", help="frontmatter field required in every note; may be repeated")
    parser.add_argument("--json", action="store_true", help="print a machine-readable JSON report")
    args = parser.parse_args()
    try:
        root = args.vault.resolve(strict=True)
    except OSError as exc:
        parser.error(f"vault path is unavailable: {exc}")
    if not root.is_dir():
        parser.error("vault path must be a directory")

    report = audit(root, args.required)
    if args.json:
        print(json.dumps(report, ensure_ascii=False, indent=2))
    else:
        print(f"Read-only audit: {report['notes_scanned']} Markdown notes under {root}")
        if not report["yaml_parser_available"]:
            print("YAML parse status: not checked; install PyYAML to validate frontmatter syntax and required fields.")
        for issue in report["issues"]:
            print(f"{issue['severity'].upper():12} {issue['file']} [{issue['check']}] {issue['detail']}")
        print("Summary:", report["summary"])
        print("No files were changed.")
    return 1 if report["issues"] else 0


if __name__ == "__main__":
    sys.exit(main())
