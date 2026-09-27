#!/usr/bin/env python3
"""PreToolUse hook: block Claude from reading secret-bearing files.

Blocks (exit 2) Read/Grep on, or any Bash command that references, a path
matching: .env, .env.*, *.pem, *.key, secrets/**, credentials/**, or any path
containing "secret"/"credential" (case-insensitive). Game code and Unity
assets are exempt from the substring rule so things like SecretDoor.cs or
SecretLevel.unity stay readable.
"""
import json
import re
import sys
from pathlib import PurePosixPath

EXEMPT_SUFFIXES = {
    ".cs", ".py", ".sh", ".md", ".txt", ".unity", ".prefab", ".asset", ".meta", ".mat",
    ".anim", ".controller", ".shader", ".hlsl", ".cginc", ".asmdef", ".uss", ".uxml",
    ".png", ".jpg", ".psd", ".wav", ".ogg", ".mp3", ".fbx",
}
# Path-ish tokens in a shell command: split on whitespace, quotes and shell operators.
TOKEN_RE = re.compile(r"[^\s'\"`|;&<>()=]+")


def is_sensitive(path):
    p = PurePosixPath(str(path).replace("\\", "/").strip())
    name = p.name.lower()
    parts = [part.lower() for part in p.parts]
    if name == ".env" or name.startswith(".env."):
        return True
    if name.endswith(".pem") or name.endswith(".key"):
        return True
    if "secrets" in parts or "credentials" in parts:
        return True
    if p.suffix.lower() in EXEMPT_SUFFIXES:
        return False
    lowered = str(p).lower()
    return "secret" in lowered or "credential" in lowered


def targets(tool, tool_input):
    if tool in ("Read", "Grep", "Glob", "NotebookRead"):
        return [tool_input.get(k) for k in ("file_path", "path", "notebook_path", "pattern")
                if tool_input.get(k) and not (tool in ("Grep", "Glob") and k == "pattern")]
    if tool == "Bash":
        return TOKEN_RE.findall(tool_input.get("command") or "")
    return []


def main():
    try:
        hook = json.load(sys.stdin)
    except ValueError:
        return 0
    tool = hook.get("tool_name", "")
    for target in targets(tool, hook.get("tool_input") or {}):
        if target and not target.startswith("-") and is_sensitive(target):
            print(f"Blocked by .claude/hooks/block_secret_reads.py: {tool} touches '{target}', "
                  "which matches the secrets policy (.env*, *.pem, *.key, secrets/, credentials/, "
                  "*secret*, *credential*). Ask the user to supply any needed values another way.",
                  file=sys.stderr)
            return 2
    return 0


if __name__ == "__main__":
    sys.exit(main())
