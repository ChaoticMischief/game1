"""Shared helpers for everything in this repo that writes to the dev-history repo.

The dev repo's location comes only from .claude/dev-repo-config.json
(`dev_repo_path`, relative to this repo's root, or absolute). Nothing else
should know where it lives.
"""
import fcntl
import json
import os
import re
import shutil
import subprocess
import sys
import tempfile
import uuid
from datetime import datetime, timezone
from pathlib import Path

PROJECT_ROOT = Path(__file__).resolve().parents[2]
CONFIG_PATH = PROJECT_ROOT / ".claude" / "dev-repo-config.json"
GITLEAKS_FALLBACK_PATHS = ("/opt/homebrew/bin/gitleaks", "/usr/local/bin/gitleaks")


def dev_repo_path():
    """Resolved dev repo path, or None if the config or the repo is missing."""
    try:
        cfg = json.loads(CONFIG_PATH.read_text(encoding="utf-8"))
    except (OSError, ValueError):
        return None
    raw = cfg.get("dev_repo_path")
    if not raw:
        return None
    path = Path(os.path.expanduser(raw))
    if not path.is_absolute():
        path = PROJECT_ROOT / path
    path = path.resolve()
    return path if (path / ".git").exists() else None


def invoked_by_cursor(hook):
    """True when Cursor runs a Claude Code hook via its third-party hook import.

    Cursor loads .claude/settings.json hooks by default; its payloads carry
    Cursor-only fields. Checked on the payload, not the environment, because
    Claude Code itself may run inside Cursor's terminal. Cursor sessions are
    captured by .cursor/hooks/cursor_hook.py instead.
    """
    return bool(hook.get("cursor_version") or hook.get("conversation_id"))


def log_error(dev_repo, message):
    """Append to an untracked log inside the dev repo's .git dir; never raise."""
    try:
        target = (dev_repo / ".git" / "devlog-errors.log") if dev_repo else None
        line = f"{now_iso()} {message}\n"
        if target:
            with target.open("a", encoding="utf-8") as fh:
                fh.write(line)
        print(message, file=sys.stderr)
    except Exception:
        pass


def now_iso():
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"


def to_utc_iso(value):
    """Normalize any ISO-8601 timestamp to UTC 'YYYY-MM-DDTHH:MM:SS.mmmZ'."""
    try:
        dt = datetime.fromisoformat(str(value).replace("Z", "+00:00"))
    except ValueError:
        return now_iso()
    if not dt.tzinfo:
        dt = dt.replace(tzinfo=timezone.utc)
    return dt.astimezone(timezone.utc).strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"


def today_utc():
    return datetime.now(timezone.utc).strftime("%Y-%m-%d")


def game1_head():
    out = subprocess.run(["git", "-C", str(PROJECT_ROOT), "rev-parse", "HEAD"],
                         capture_output=True, text=True)
    return out.stdout.strip() if out.returncode == 0 else None


def find_session_dir(dev_repo, session_id, agent="*"):
    """Existing folder for this session under sessions/<agent>/<date>_<id>/, or None."""
    if not session_id:
        return None
    for meta in sorted(dev_repo.glob(f"sessions/{agent}/*_{session_id}/metadata.json")):
        return meta.parent
    return None


def session_dir_for(dev_repo, agent, session_id):
    existing = find_session_dir(dev_repo, session_id, agent)
    if existing:
        return existing
    path = dev_repo / "sessions" / agent / f"{today_utc()}_{session_id}"
    path.mkdir(parents=True, exist_ok=True)
    return path


def read_json(path, default=None):
    try:
        return json.loads(Path(path).read_text(encoding="utf-8"))
    except (OSError, ValueError):
        return default


def write_json_atomic(path, data):
    path = Path(path)
    fd, tmp = tempfile.mkstemp(dir=path.parent, prefix=f".{path.name}.", suffix=".tmp")
    with os.fdopen(fd, "w", encoding="utf-8") as fh:
        json.dump(data, fh, indent=2, sort_keys=True)
        fh.write("\n")
    os.replace(tmp, path)


def make_event(agent, session_id, event_type, summary, timestamp=None, game1_sha=None, raw_ref=None):
    return {
        "event_id": str(uuid.uuid4()),
        "timestamp": to_utc_iso(timestamp) if timestamp else now_iso(),
        "agent": agent,
        "session_id": session_id,
        "event_type": event_type,
        "game1_sha": game1_sha,
        "summary": one_line(summary),
        "raw_ref": raw_ref,
    }


def one_line(text, limit=160):
    text = re.sub(r"\s+", " ", str(text or "")).strip()
    return text if len(text) <= limit else text[: limit - 1] + "…"


def append_events(path, events):
    """Append events as NDJSON lines under an exclusive lock, in one write."""
    if not events:
        return
    payload = "".join(json.dumps(e, ensure_ascii=False, sort_keys=True) + "\n" for e in events)
    Path(path).parent.mkdir(parents=True, exist_ok=True)
    with open(path, "a", encoding="utf-8") as fh:
        fcntl.flock(fh, fcntl.LOCK_EX)
        try:
            fh.write(payload)
            fh.flush()
        finally:
            fcntl.flock(fh, fcntl.LOCK_UN)


def gitleaks_bin():
    return shutil.which("gitleaks") or next(
        (p for p in GITLEAKS_FALLBACK_PATHS if os.path.exists(p)), None)


def redact_file(path, log_path, dev_repo=None):
    """Replace every gitleaks finding in `path` with [REDACTED:<rule>].

    Writes `log_path` (rule + line, never the value). Returns the number of
    findings, or None if gitleaks is unavailable -- in that case the caller
    must not publish the file.
    """
    binary = gitleaks_bin()
    if not binary:
        log_error(dev_repo, "gitleaks not found (brew install gitleaks); refusing to sync unredacted content")
        return None
    fd, report = tempfile.mkstemp(suffix=".json")
    os.close(fd)
    try:
        proc = subprocess.run(
            [binary, "dir", str(path), "--no-banner", "--log-level", "error",
             "--report-format", "json", "--report-path", report, "--exit-code", "0"],
            capture_output=True, text=True)
        if proc.returncode != 0:
            log_error(dev_repo, f"gitleaks failed: {proc.stderr.strip()}")
            return None
        findings = read_json(report, []) or []
    finally:
        os.unlink(report)

    text = Path(path).read_text(encoding="utf-8")
    entries = []
    # Longest secrets first so a secret that contains another is replaced whole.
    for f in sorted(findings, key=lambda f: -len(f.get("Secret") or "")):
        secret, rule = f.get("Secret") or "", f.get("RuleID", "unknown")
        if secret:
            text = text.replace(secret, f"[REDACTED:{rule}]")
        entries.append(f"{Path(path).name}:{f.get('StartLine')} rule={rule}")
    if findings:
        Path(path).write_text(text, encoding="utf-8")
    Path(log_path).write_text("".join(e + "\n" for e in sorted(set(entries))), encoding="utf-8")
    return len(findings)


def record_missing_commits(dev_repo, agent, session_id, session_dir, limit=200):
    """Append commit events for game1 commits carrying this session's trailer that
    .githooks/post-commit didn't record, e.g. because the agent's sandbox (Cursor runs
    shell commands sandboxed) couldn't write to the dev repo. Returns the number added."""
    out = subprocess.run(
        ["git", "-C", str(PROJECT_ROOT), "log", "--all", f"-n{limit}", "--format=%H%x1f%cI%x1f%B%x1e"],
        capture_output=True, text=True).stdout
    events_path = Path(session_dir) / "events.ndjson"
    try:
        recorded = events_path.read_text(encoding="utf-8")
    except OSError:
        recorded = ""
    trailer = re.compile(rf"^Session-Id:\s*{re.escape(session_id)}\s*$", re.M | re.I)
    missing = []
    for record in out.split("\x1e"):
        parts = record.strip("\n").split("\x1f")
        if len(parts) != 3 or not trailer.search(parts[2]):
            continue
        sha, committed_at, message = parts
        if f'"game1_sha": "{sha}"' in recorded:
            continue
        first_line = message.strip().splitlines()[0] if message.strip() else "(empty commit message)"
        missing.append(make_event(agent, session_id, "commit", first_line,
                                  timestamp=committed_at, game1_sha=sha))
    append_events(events_path, sorted(missing, key=lambda e: e["timestamp"]))
    return len(missing)


def build_timeline(dev_repo):
    subprocess.run([sys.executable, str(dev_repo / "tools" / "build_timeline.py")],
                   capture_output=True, text=True)


def safe_push(dev_repo, message):
    proc = subprocess.run(["bash", str(dev_repo / "tools" / "safe_push.sh"), message],
                          capture_output=True, text=True)
    if proc.returncode != 0:
        log_error(dev_repo, f"safe_push failed ({message}): {proc.stderr.strip()}")
    return proc.returncode == 0
