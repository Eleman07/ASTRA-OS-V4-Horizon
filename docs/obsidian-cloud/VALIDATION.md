# Generic workspace package validation

Checked on 2026-10-31. Scope: the shipped generic Command Center package and its installer, using synthetic workspace-only vaults. No personal vault content was used by these checks.

| Check | Actual result |
| --- | --- |
| Package note count | 38 Markdown notes: 20 dashboards/reference notes and 18 templates |
| Wiki-link audit | 0 unresolved wiki links; 0 read errors; 0 duplicate-title warnings |
| YAML parsing | Not executed because PyYAML was unavailable; the audit marked 19 frontmatter-bearing notes as needs-review |
| PowerShell syntax | Installer parsed without errors |
| Dry run | Reported 38 missing files and created no Command Center folder |
| First install | Created 38 files; overwrote 0 |
| Second install | Created 0; preserved all 38 |
| Edited note | SHA-256 unchanged across the second install |
| Unrelated private fixture note | SHA-256 unchanged |
| Directory without `.obsidian` | Rejected before creating an install subtree |
| Existing file in a required directory path | Rejected during preflight before creating package notes |
| Junction at the target subtree | Rejected; the linked target remained empty |
| Junction in a vault ancestor | Rejected before creating the Command Center subtree |

New files are staged under their target directory and moved with the two-argument, no-replacement `File.Move` operation. Existing destinations are preserved. The installer cleans up only its own temporary files and does not read unrelated note contents.

These checks validate packaging, links and preservation behavior. They do not verify a user's entire vault, plugin behavior, OpenRouter credentials, live endpoint availability, model arithmetic or the correctness of generated scientific work.
