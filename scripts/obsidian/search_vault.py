#!/usr/bin/env python3
"""Read-only lexical search for an Obsidian vault, with file and line citations."""

from __future__ import annotations

import argparse
import re
from pathlib import Path

EXCLUDED = {".git", ".obsidian", ".trash", "node_modules", ".local-llm-hub"}
WORD = re.compile(r"[\w'-]+", re.UNICODE)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("vault", type=Path, help="Obsidian vault root")
    parser.add_argument("query", nargs="+", help="search words")
    parser.add_argument("--limit", type=int, default=10, help="maximum matching lines (1-100)")
    args = parser.parse_args()
    if not 1 <= args.limit <= 100:
        parser.error("--limit must be between 1 and 100")
    try:
        root = args.vault.resolve(strict=True)
    except OSError as exc:
        parser.error(f"vault path is unavailable: {exc}")
    if not root.is_dir():
        parser.error("vault path must be a directory")

    terms = list(dict.fromkeys(word.casefold() for part in args.query for word in WORD.findall(part)))
    if not terms:
        parser.error("query must contain at least one word")
    matches: list[tuple[int, str, int, str]] = []
    for path in root.rglob("*.md"):
        try:
            relative = path.relative_to(root)
            if path.is_symlink() or any(part in EXCLUDED for part in relative.parts):
                continue
            path.resolve(strict=True).relative_to(root)
            content = path.read_text(encoding="utf-8-sig")
        except (OSError, UnicodeError, ValueError):
            continue
        for line_number, line in enumerate(content.splitlines(), 1):
            lowered = line.casefold()
            score = sum(lowered.count(term) for term in terms)
            if score:
                excerpt = " ".join(line.split())[:320]
                matches.append((score, relative.as_posix(), line_number, excerpt))
    matches.sort(key=lambda item: (-item[0], item[1].casefold(), item[2]))
    for score, file, line, excerpt in matches[:args.limit]:
        print(f"{file}:{line} [score {score}] {excerpt}")
    print(f"\n{min(len(matches), args.limit)} of {len(matches)} matching lines; read-only, no files changed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
