#!/usr/bin/env python3
"""Stop / PreCompact / SessionEnd hook: sync this session's transcript to the dev repo.

Writes only inside this session's own folder
(<dev_repo>/sessions/claude-code/<date>_<session_id>/): a redacted copy of the
transcript, metadata.json, redactions.log, and new events appended to
events.ndjson. Then rebuilds the derived timeline and commits/pushes via the
dev repo's retry wrapper. Always exits 0 so a sync problem never blocks Claude.
"""
import fcntl
import json
import os
import re
import shutil
import sys
import tempfile
from pathlib import Path

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import devrepo_common as dc  # noqa: E402

AGENT = "claude-code"
MEMORY_REDACTION_LOG = "redactions.log"
TOOL_INPUT_KEYS = ("command", "file_path", "path", "pattern", "url", "query", "description", "prompt")
TAG_RE = re.compile(r"<(system-reminder|command-[a-z-]+|local-command-[a-z-]+)>.*?</\1>", re.S)


def text_of(content):
    if isinstance(content, str):
        return content
    if isinstance(content, list):
        return " ".join(text_of(c.get("text") or c.get("content")) for c in content if isinstance(c, dict))
    return ""


def clean(text):
    return TAG_RE.sub(" ", text or "")


def tool_input_summary(inp):
    if not isinstance(inp, dict):
        return ""
    for key in TOOL_INPUT_KEYS:
        if inp.get(key):
            return str(inp[key])
    return ""


def normalize(lines, start, session_id, ref_prefix):
    """Events for transcript lines[start:], with raw_ref pointing at the 1-based line."""
    tool_names = {}
    for line in lines:
        try:
            rec = json.loads(line)
        except ValueError:
            continue
        content = (rec.get("message") or {}).get("content")
        if rec.get("type") == "assistant" and isinstance(content, list):
            for block in content:
                if isinstance(block, dict) and block.get("type") == "tool_use":
                    tool_names[block.get("id")] = block.get("name")

    events = []
    for idx in range(start, len(lines)):
        try:
            rec = json.loads(lines[idx])
        except ValueError:
            continue
        rtype = rec.get("type")
        if rtype not in ("user", "assistant") or rec.get("isMeta") or rec.get("isCompactSummary"):
            continue
        ts = rec.get("timestamp")
        ref = f"{ref_prefix}:{idx + 1}"
        content = (rec.get("message") or {}).get("content")
        blocks = [{"type": "text", "text": content}] if isinstance(content, str) else (content or [])

        for block in blocks:
            if not isinstance(block, dict):
                continue
            btype = block.get("type")
            if btype == "text":
                text = clean(block.get("text")).strip()
                if not text:
                    continue
                etype = "user_prompt" if rtype == "user" else "assistant_response"
                events.append(dc.make_event(AGENT, session_id, etype, text, ts, raw_ref=ref))
            elif btype == "tool_use":
                name = block.get("name", "tool")
                detail = tool_input_summary(block.get("input"))
                events.append(dc.make_event(AGENT, session_id, "tool_call",
                                            f"{name}: {detail}" if detail else name, ts, raw_ref=ref))
            elif btype == "tool_result":
                name = tool_names.get(block.get("tool_use_id"), "tool")
                status = "error" if block.get("is_error") else "ok"
                body = clean(text_of(block.get("content"))).strip()
                events.append(dc.make_event(AGENT, session_id, "tool_result",
                                            f"{name} {status}: {body}", ts, raw_ref=ref))
    return events


def sync_memory(transcript_path, dev_repo):
    """Mirror Claude Code's auto-memory for this project into <dev_repo>/memory/claude-code/.

    The memory dir sits next to the transcript (~/.claude/projects/<project>/memory/).
    Unlike transcripts it is shared by every session of the project, so the mirror
    is a snapshot (git history versions it) written under a repo-wide lock; any
    session mirroring it produces the same result.
    """
    project_dir = Path(transcript_path).resolve().parent
    # Only game1's own project folder (Claude Code names it after the path with
    # non-alphanumerics as "-"). A session launched elsewhere, e.g. ~/dev, has a
    # different memory dir, and mirroring it would wipe game1's memories.
    if project_dir.name != re.sub(r"[^A-Za-z0-9]", "-", str(dc.PROJECT_ROOT)):
        return
    src = project_dir / "memory"
    dest = dev_repo / "memory" / AGENT
    if not src.is_dir() or (not any(src.iterdir()) and not dest.exists()):
        return
    lock_path = dev_repo / ".git" / "devlog-memory.lock"
    with open(lock_path, "w") as lock, tempfile.TemporaryDirectory() as tmp:
        fcntl.flock(lock, fcntl.LOCK_EX)
        staged_root = Path(tmp) / "memory"
        shutil.copytree(src, staged_root)
        staged = sorted(p for p in staged_root.rglob("*") if p.is_file())
        log_lines = []
        for path in staged:
            file_log = Path(tmp) / "file-redactions.log"
            if dc.redact_file(path, file_log, dev_repo) is None:
                return
            rel = path.relative_to(staged_root).as_posix()
            log_lines += [f"{rel}:{line.split(':', 1)[1]}" for line in file_log.read_text().splitlines()]
        (staged_root / MEMORY_REDACTION_LOG).write_text("".join(l + "\n" for l in log_lines), encoding="utf-8")

        wanted = {p.relative_to(staged_root) for p in staged} | {Path(MEMORY_REDACTION_LOG)}
        for rel in wanted:
            new = (staged_root / rel).read_bytes()
            target = dest / rel
            if target.exists() and target.read_bytes() == new:
                continue
            target.parent.mkdir(parents=True, exist_ok=True)
            partial = target.parent / f".{target.name}.tmp"
            partial.write_bytes(new)
            os.replace(partial, target)
        if dest.is_dir():
            for existing in dest.rglob("*"):
                if existing.is_file() and not existing.name.endswith(".tmp") \
                        and existing.relative_to(dest) not in wanted:
                    existing.unlink()  # deleted from memory -> deleted from the mirror


def sync(hook, dev_repo):
    session_id = hook["session_id"]
    event_name = hook.get("hook_event_name", "Stop")
    source = hook.get("transcript_path")
    if not source or not os.path.exists(source):
        return

    session_dir = dc.session_dir_for(dev_repo, AGENT, session_id)
    meta_path = session_dir / "metadata.json"
    meta = dc.read_json(meta_path, {}) or {}

    # Redact in a temp dir outside the dev repo so an unredacted copy never
    # sits in the working tree where a concurrent `git add -A` could pick it up.
    with tempfile.TemporaryDirectory() as tmp:
        staged = os.path.join(tmp, "transcript.jsonl")
        shutil.copyfile(source, staged)
        redaction_log = os.path.join(tmp, "redactions.log")
        if dc.redact_file(staged, redaction_log, dev_repo) is None:
            return
        for name in ("transcript.jsonl", "redactions.log"):
            dest = session_dir / name
            partial = session_dir / f".{name}.tmp"
            shutil.copyfile(os.path.join(tmp, name), partial)
            os.replace(partial, dest)

    with (session_dir / "transcript.jsonl").open(encoding="utf-8") as fh:
        lines = fh.read().splitlines()
    start = min(int(meta.get("last_synced_line") or 0), len(lines))
    ref_prefix = f"{session_dir.relative_to(dev_repo).as_posix()}/transcript.jsonl"
    events = normalize(lines, start, session_id, ref_prefix)
    if event_name == "SessionEnd":
        events.append(dc.make_event(AGENT, session_id, "session_end",
                                    f"Claude Code session ended ({hook.get('reason') or 'exit'})"))
    dc.append_events(session_dir / "events.ndjson", events)

    now = dc.now_iso()
    meta.setdefault("session_id", session_id)
    meta.setdefault("agent", AGENT)
    meta.setdefault("start_time", now)
    meta.update({"last_synced_time": now, "last_synced_line": len(lines), "game1_head": dc.game1_head()})
    dc.write_json_atomic(meta_path, meta)

    sync_memory(source, dev_repo)
    dc.build_timeline(dev_repo)
    dc.safe_push(dev_repo, f"claude-code: sync {session_id[:8]} ({event_name})")


def main():
    try:
        hook = json.load(sys.stdin)
    except ValueError:
        return 0
    dev_repo = dc.dev_repo_path()
    if not dev_repo or not hook.get("session_id") or dc.invoked_by_cursor(hook):
        return 0
    try:
        # Serialize overlapping syncs of the *same* session (e.g. PreCompact
        # racing Stop) so last_synced_line is never read stale.
        lock_path = dev_repo / ".git" / f"devlog-sync-{hook['session_id']}.lock"
        with open(lock_path, "w") as lock:
            fcntl.flock(lock, fcntl.LOCK_EX)
            sync(hook, dev_repo)
    except Exception as exc:
        dc.log_error(dev_repo, f"sync_to_dev_repo failed for {hook.get('session_id')}: {exc!r}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
