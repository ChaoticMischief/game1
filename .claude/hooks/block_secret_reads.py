#!/usr/bin/env python3
"""PreToolUse hook: block Claude from reading secret-bearing files.

Read/Grep/Glob on a path matching .env, .env.*, *.pem, *.key, secrets/**,
credentials/**, or any path containing "secret"/"credential" (case-insensitive)
is blocked (exit 2). Game code and Unity assets are exempt from the substring
rule so things like SecretDoor.cs or SecretLevel.unity stay readable.

Bash is checked per argument, and only for things that are actually file paths,
so commands that merely mention the words (`gh secret set`, an API field named
secret_scanning) still run. An argument is blocked if it:
- names a .env / .env.* / *.pem / *.key file,
- is a path through a secrets/ or credentials/ directory (contains "/" or exists), or
- names an existing file or directory whose path contains "secret"/"credential"
  (same exemptions as above).

.cursor/hooks/cursor_hook.py applies the same policy to Cursor.
"""
import json
import os
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
POLICY = ".env*, *.pem, *.key, secrets/, credentials/, *secret*, *credential*"


def _path(value):
    return PurePosixPath(str(value).replace("\\", "/").strip())


def _is_secret_file_name(name):
    name = name.lower()
    return name == ".env" or name.startswith(".env.") or name.endswith(".pem") or name.endswith(".key")


def _in_secret_dir(p):
    parts = [part.lower() for part in p.parts]
    return "secrets" in parts or "credentials" in parts


def _has_secret_word(p):
    if p.suffix.lower() in EXEMPT_SUFFIXES:
        return False
    lowered = str(p).lower()
    return "secret" in lowered or "credential" in lowered


def is_sensitive(path):
    """Policy for file tools (Read/Grep/Glob): every argument is a path."""
    p = _path(path)
    return _is_secret_file_name(p.name) or _in_secret_dir(p) or _has_secret_word(p)


def is_sensitive_shell_token(token, cwd=None):
    """Policy for one shell-command argument: only block what is really a secret path."""
    token = str(token).strip()
    if not token or token.startswith("-"):
        return False
    p = _path(token)
    if _is_secret_file_name(p.name):
        return True
    resolved = os.path.join(cwd or os.getcwd(), os.path.expanduser(token))
    exists = os.path.exists(resolved)
    if _in_secret_dir(p) and ("/" in token or exists):
        return True
    return exists and _has_secret_word(p)


def blocked_target(tool, tool_input, cwd=None):
    """The first argument that violates the policy, or None."""
    if tool in ("Read", "Grep", "Glob", "NotebookRead"):
        for key in ("file_path", "path", "notebook_path", "pattern"):
            value = tool_input.get(key)
            if value and not (tool in ("Grep", "Glob") and key == "pattern") and is_sensitive(value):
                return value
    elif tool == "Bash":
        for token in TOKEN_RE.findall(tool_input.get("command") or ""):
            if is_sensitive_shell_token(token, cwd):
                return token
    return None


def main():
    try:
        hook = json.load(sys.stdin)
    except ValueError:
        return 0
    tool = hook.get("tool_name", "")
    target = blocked_target(tool, hook.get("tool_input") or {}, hook.get("cwd"))
    if target:
        print(f"Blocked by .claude/hooks/block_secret_reads.py: {tool} touches '{target}', "
              f"which matches the secrets policy ({POLICY}). "
              "Ask the user to supply any needed values another way.", file=sys.stderr)
        return 2
    return 0


if __name__ == "__main__":
    sys.exit(main())
