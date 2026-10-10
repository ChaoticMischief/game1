#!/usr/bin/env python3
"""Report dependencies that are behind upstream, for the weekly `upstream` issue.

Checks:
- Unity editor (ProjectSettings/ProjectVersion.txt) against Unity's release API: newer patches
  of our version line, and newer supported versions (6000.x, Unity 7).
- Unity registry packages in Packages/manifest.json against packages.unity.com, counting only
  stable versions that declare compatibility with our editor. Packages bundled with the editor
  (URP, built-in modules) update with the editor instead.
- Git packages in the manifest pinned to a tag (`...git#v1.2.3`) against the repo's latest
  GitHub release.
- Microsoft.Unity.Analyzers (version in Assets/Plugins/Analyzers/README.md) against NuGet.

GitHub Actions versions are Dependabot's job (.github/dependabot.yml).

Usage: check_upstream.py [--markdown FILE]
Prints a report; with --markdown, also writes the issue body to FILE. Exit code 0 always: being
behind isn't an error. Set GH_TOKEN (or GITHUB_TOKEN) to avoid GitHub API rate limits.
"""
import argparse
import json
import os
import re
import sys
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
# The 25 newest releases (the API maximum per page) cover every current version line.
UNITY_RELEASES = ("https://services.api.unity.com/unity/editor/release/v1/releases"
                  "?limit=25&order=RELEASE_DATE_DESC&stream=LTS&stream=SUPPORTED&stream=TECH")


def unity_version_key(version):
    """6000.6.3f1 -> (6000, 6, 3, 'f', 1); None for anything else."""
    m = re.fullmatch(r"(\d+)\.(\d+)\.(\d+)([abfp])(\d+)", version or "")
    if not m:
        return None
    return int(m[1]), int(m[2]), int(m[3]), m[4], int(m[5])


def semver_key(version):
    """1.2.3 / v1.2.3 -> (1, 2, 3); None for pre-releases (1.2.3-preview) and anything else."""
    m = re.fullmatch(r"v?(\d+)\.(\d+)\.(\d+)", (version or "").strip())
    return tuple(int(x) for x in m.groups()) if m else None


def editor_updates(current, releases):
    """(newest patch on our major.minor line, newest release overall), each None if not newer."""
    cur = unity_version_key(current)
    keyed = [(unity_version_key(v), v) for v in releases]
    keyed = [(k, v) for k, v in keyed if k and k[3] == "f"]  # final releases only
    same_line = [kv for kv in keyed if kv[0][:2] == cur[:2] and kv[0] > cur]
    newer = [kv for kv in keyed if kv[0][:2] > cur[:2]]
    return (max(same_line)[1] if same_line else None,
            max(newer)[1] if newer else None)


def compatible_registry_update(current, versions, editor):
    """Newest stable version newer than `current` whose `unity` field is <= our editor's
    major.minor. `versions` maps version -> registry metadata."""
    editor_mm = unity_version_key(editor)[:2]
    cur = semver_key(current)
    if cur is None:
        return None
    best = None
    for version, meta in versions.items():
        key = semver_key(version)
        if key is None or key <= cur:
            continue
        unity = re.fullmatch(r"(\d+)\.(\d+)", meta.get("unity", "") or "")
        if unity and (int(unity[1]), int(unity[2])) > editor_mm:
            continue
        if best is None or key > best[0]:
            best = (key, version)
    return best[1] if best else None


def git_package(url):
    """https://github.com/owner/repo.git?path=/x#v1.2.3 -> ('owner/repo', 'v1.2.3'); None if not
    a tag-pinned GitHub package."""
    m = re.match(r"https://github\.com/([^/]+/[^/.?#]+)(?:\.git)?[^#]*#(.+)$", url)
    return (m[1], m[2]) if m and semver_key(m[2]) else None


def analyzers_version(readme_text):
    m = re.search(r"Microsoft\.Unity\.Analyzers\]\([^)]*\)\s*\n?\s*(\d+\.\d+\.\d+)", readme_text)
    return m[1] if m else None


def get_json(url, github=False):
    req = urllib.request.Request(url, headers={"Accept": "application/json", "User-Agent": "game1-upstream-check"})
    token = os.environ.get("GH_TOKEN") or os.environ.get("GITHUB_TOKEN")
    if github and token:
        req.add_header("Authorization", f"Bearer {token}")
    with urllib.request.urlopen(req, timeout=30) as resp:
        return json.load(resp)


def check():
    """List of (component, current, available, note) rows, and a list of errors."""
    rows, errors = [], []
    editor = re.search(r"m_EditorVersion: (\S+)", (ROOT / "ProjectSettings/ProjectVersion.txt").read_text())[1]

    try:
        releases = [r["version"] for r in get_json(UNITY_RELEASES)["results"]]
        patch, newer = editor_updates(editor, releases)
        if patch:
            rows.append(("Unity editor", editor, patch, "patch release on our version line"))
        if newer:
            rows.append(("Unity editor", editor, newer, "newer version (check release notes before moving)"))
    except Exception as e:  # noqa: BLE001 - report and carry on with the other checks
        errors.append(f"Unity releases: {e}")

    deps = json.loads((ROOT / "Packages/manifest.json").read_text())["dependencies"]
    for name, version in sorted(deps.items()):
        try:
            if version.startswith("https://"):
                pinned = git_package(version)
                if not pinned:
                    continue
                repo, tag = pinned
                latest = get_json(f"https://api.github.com/repos/{repo}/releases/latest", github=True)["tag_name"]
                if semver_key(latest) and semver_key(latest) > semver_key(tag):
                    rows.append((name, tag, latest, f"git package, github.com/{repo}"))
            elif not name.startswith("com.unity.modules."):
                meta = get_json(f"https://packages.unity.com/{name}")
                update = compatible_registry_update(version, meta.get("versions", {}), editor)
                if update:
                    rows.append((name, version, update, "Unity registry"))
        except Exception as e:  # noqa: BLE001
            errors.append(f"{name}: {e}")

    try:
        readme = ROOT / "Assets/Plugins/Analyzers/README.md"
        current = analyzers_version(readme.read_text())
        versions = get_json("https://api.nuget.org/v3-flatcontainer/microsoft.unity.analyzers/index.json")["versions"]
        stable = [v for v in versions if semver_key(v)]
        latest = max(stable, key=semver_key)
        if current and semver_key(latest) > semver_key(current):
            rows.append(("Microsoft.Unity.Analyzers", current, latest, "NuGet; see Assets/Plugins/Analyzers/README.md"))
    except Exception as e:  # noqa: BLE001
        errors.append(f"Microsoft.Unity.Analyzers: {e}")

    return rows, errors


def markdown(rows, errors):
    lines = []
    if rows:
        lines += ["These dependencies have newer versions upstream. Update them together in one PR (CI",
                  "builds are slow), splitting only if one breaks, and check release notes for breaking changes.", "",
                  "| Component | Current | Available | Notes |", "|---|---|---|---|"]
        lines += [f"| `{c}` | {cur} | **{new}** | {note} |" for c, cur, new, note in rows]
    else:
        lines.append("Everything checked is up to date.")
    if errors:
        lines += ["", "Checks that failed this time (the next run retries):", ""]
        lines += [f"- {e}" for e in errors]
    lines += ["", "_Updated weekly by `.github/workflows/upstream.yml` (`tools/ci/check_upstream.py`)._"]
    return "\n".join(lines) + "\n"


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--markdown", type=Path, help="write the issue body here")
    args = parser.parse_args()
    rows, errors = check()
    body = markdown(rows, errors)
    print(body)
    if args.markdown:
        args.markdown.write_text(body)
        # For the workflow: whether there's anything to report.
        if os.environ.get("GITHUB_OUTPUT"):
            with open(os.environ["GITHUB_OUTPUT"], "a") as out:
                out.write(f"behind={'true' if rows else 'false'}\n")
                out.write(f"failed={'true' if errors else 'false'}\n")
    return 0


if __name__ == "__main__":
    sys.exit(main())
