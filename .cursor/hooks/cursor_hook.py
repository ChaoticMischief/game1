#!/usr/bin/env python3
"""Cursor hook dispatcher: capture Cursor agent sessions into the dev-history repo,
and apply the same secrets-read policy as the Claude Code hooks.

Wired up in .cursor/hooks.json; one script handles every event, keyed on the
payload's `hook_event_name`.

Capture works from hook payloads rather than parsing Cursor's transcript, whose
format Cursor doesn't document:
- sessionStart, beforeSubmitPrompt, afterAgentResponse, postToolUse(Failure),
  sessionEnd each add one event to a pending buffer inside the dev repo's .git
  dir (never the working tree, so nothing unredacted can be committed).
- stop / preCompact / sessionEnd flush: the buffer is redacted with gitleaks and
  appended to sessions/cursor/<date>_<conversation_id>/events.ndjson, the
  transcript at `transcript_path` (if Cursor provides one) is copied in redacted,
  metadata.json is updated, the timeline is rebuilt and tools/safe_push.sh pushes.

Permission hooks (beforeReadFile, beforeShellExecution, preToolUse) answer
"deny" for secret paths and "allow" otherwise. Cursor treats "allow" as advisory:
its own approval prompts and allowlist still apply.

Never fails the agent: every capture error is logged to
<dev repo>/.git/devlog-errors.log and the hook exits 0.
"""
import fcntl
import json
import os
import re
import shutil
import sys
import tempfile
from pathlib import Path

GAME_ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(GAME_ROOT / ".claude" / "hooks"))
import devrepo_common as dc  # noqa: E402
from block_secret_reads import TOKEN_RE, is_sensitive  # noqa: E402

AGENT = "cursor"
TOOL_INPUT_KEYS = ("command", "file_path", "target_file", "path", "pattern", "query",
                   "url", "description", "prompt")
PERMISSION_EVENTS = {"beforeReadFile", "beforeShellExecution", "preToolUse", "beforeMCPExecution"}
FLUSH_EVENTS = {"stop", "preCompact", "sessionEnd"}


# ---------------------------------------------------------------- secrets policy

def secret_targets(event, hook):
    if event == "beforeReadFile":
        return [hook.get("file_path")]
    if event == "beforeShellExecution":
        return TOKEN_RE.findall(hook.get("command") or "")
    if event == "preToolUse":
        tool, inp = hook.get("tool_name", ""), hook.get("tool_input") or {}
        if tool == "Shell":
            return TOKEN_RE.findall(inp.get("command") or "")
        if tool in ("Read", "Grep", "Glob"):
            return [inp.get(k) for k in ("file_path", "target_file", "path") if inp.get(k)]
    return []


def permission(event, hook):
    for target in secret_targets(event, hook):
        if target and not str(target).startswith("-") and is_sensitive(target):
            msg = (f"Blocked by .cursor/hooks/cursor_hook.py: '{target}' matches the secrets policy "
                   "(.env*, *.pem, *.key, secrets/, credentials/, *secret*, *credential*). "
                   "Ask the user to supply any needed values another way.")
            return {"permission": "deny", "user_message": msg, "agent_message": msg}
    return {"permission": "allow"}


# ---------------------------------------------------------------- capture

def summarize_tool(hook):
    name = hook.get("tool_name") or "tool"
    inp = hook.get("tool_input")
    if isinstance(inp, str):
        try:
            inp = json.loads(inp)
        except ValueError:
            inp = {"description": inp}
    detail = next((str(inp[k]) for k in TOOL_INPUT_KEYS if isinstance(inp, dict) and inp.get(k)), "")
    return f"{name}: {detail}" if detail else name


def events_for(event, hook, session_id):
    def ev(etype, summary, **kw):
        return dc.make_event(AGENT, session_id, etype, summary, **kw)

    if event == "sessionStart":
        head = dc.game1_head()
        mode = hook.get("composer_mode") or "agent"
        bg = " background" if hook.get("is_background_agent") else ""
        return [ev("session_start",
                   f"Cursor{bg} {mode} session start, {hook.get('model') or 'model?'} "
                   f"(game1 HEAD {(head or 'none')[:8]})", game1_sha=head)]
    if event == "beforeSubmitPrompt":
        return [ev("user_prompt", hook.get("prompt") or "")]
    if event == "afterAgentResponse":
        return [ev("assistant_response", hook.get("text") or "")]
    if event == "postToolUse":
        return [ev("tool_call", summarize_tool(hook)),
                ev("tool_result", f"{hook.get('tool_name') or 'tool'} ok: {hook.get('tool_output') or ''}")]
    if event == "postToolUseFailure":
        return [ev("tool_call", summarize_tool(hook)),
                ev("tool_result", f"{hook.get('tool_name') or 'tool'} {hook.get('failure_type') or 'error'}: "
                                  f"{hook.get('error_message') or ''}")]
    if event == "sessionEnd":
        return [ev("session_end", f"Cursor session ended ({hook.get('reason') or 'exit'})")]
    return []


def pending_path(dev_repo, session_id):
    path = dev_repo / ".git" / "devlog-pending" / f"{AGENT}-{session_id}.ndjson"
    path.parent.mkdir(parents=True, exist_ok=True)
    return path


def ensure_session(dev_repo, session_id, hook):
    session_dir = dc.session_dir_for(dev_repo, AGENT, session_id)
    meta_path = session_dir / "metadata.json"
    meta = dc.read_json(meta_path, {}) or {}
    if not meta:
        meta = {"session_id": session_id, "agent": AGENT, "start_time": dc.now_iso(),
                "last_synced_time": None, "game1_head": dc.game1_head()}
    meta["cursor_version"] = hook.get("cursor_version") or os.environ.get("CURSOR_VERSION")
    dc.write_json_atomic(meta_path, meta)
    return session_dir, meta_path


def flush(dev_repo, session_id, hook, session_dir, meta_path):
    pending = pending_path(dev_repo, session_id)
    with tempfile.TemporaryDirectory() as tmp:
        tmp = Path(tmp)
        # Take the pending buffer atomically so events added meanwhile go to a fresh file.
        staged_events = tmp / "events.ndjson"
        if pending.exists():
            os.replace(pending, staged_events)
        else:
            staged_events.write_text("", encoding="utf-8")
        if dc.redact_file(staged_events, tmp / "events-redactions.log", dev_repo) is None:
            os.replace(staged_events, pending)  # keep for the next attempt
            return
        events = [json.loads(l) for l in staged_events.read_text(encoding="utf-8").splitlines() if l.strip()]

        log_lines = (tmp / "events-redactions.log").read_text(encoding="utf-8").splitlines()
        source = hook.get("transcript_path") or os.environ.get("CURSOR_TRANSCRIPT_PATH")
        transcript_name = None
        if source and os.path.isfile(source):
            transcript_name = "transcript" + (Path(source).suffix or ".txt")
            staged = tmp / transcript_name
            shutil.copyfile(source, staged)
            if dc.redact_file(staged, tmp / "transcript-redactions.log", dev_repo) is not None:
                log_lines += (tmp / "transcript-redactions.log").read_text(encoding="utf-8").splitlines()
                partial = session_dir / f".{transcript_name}.tmp"
                shutil.copyfile(staged, partial)
                os.replace(partial, session_dir / transcript_name)
            else:
                transcript_name = None

    dc.append_events(session_dir / "events.ndjson", events)
    # Cursor's sandboxed shell can't write to the dev repo, so .githooks/post-commit
    # usually fails for Cursor's commits; record them from here instead.
    dc.record_missing_commits(dev_repo, AGENT, session_id, session_dir)
    if log_lines:
        with open(session_dir / "redactions.log", "a", encoding="utf-8") as fh:
            fh.write("".join(l + "\n" for l in log_lines))
    else:
        (session_dir / "redactions.log").touch()

    meta = dc.read_json(meta_path, {}) or {}
    meta.update({"last_synced_time": dc.now_iso(), "game1_head": dc.game1_head()})
    if transcript_name:
        meta["transcript_file"] = transcript_name
    dc.write_json_atomic(meta_path, meta)

    dc.build_timeline(dev_repo)
    dc.safe_push(dev_repo, f"cursor: sync {session_id[:8]} ({hook.get('hook_event_name')})")


def capture(event, hook):
    session_id = hook.get("conversation_id") or hook.get("session_id")
    dev_repo = dc.dev_repo_path()
    if not session_id or not dev_repo:
        return
    try:
        lock_path = dev_repo / ".git" / f"devlog-sync-{session_id}.lock"
        with open(lock_path, "w") as lock:
            fcntl.flock(lock, fcntl.LOCK_EX)
            session_dir, meta_path = ensure_session(dev_repo, session_id, hook)
            events = events_for(event, hook, session_id)
            if events:
                with open(pending_path(dev_repo, session_id), "a", encoding="utf-8") as fh:
                    fh.write("".join(json.dumps(e, ensure_ascii=False, sort_keys=True) + "\n" for e in events))
            if event in FLUSH_EVENTS:
                flush(dev_repo, session_id, hook, session_dir, meta_path)
    except Exception as exc:
        dc.log_error(dev_repo, f"cursor_hook {event} failed for {session_id}: {exc!r}")


# ---------------------------------------------------------------- entry point

def main():
    try:
        hook = json.load(sys.stdin)
    except ValueError:
        hook = {}
    event = hook.get("hook_event_name", "")

    if event in PERMISSION_EVENTS:
        print(json.dumps(permission(event, hook)))
        return 0

    capture(event, hook)

    if event == "sessionStart":
        sid = hook.get("conversation_id") or hook.get("session_id")
        if sid:
            print(json.dumps({"additional_context": (
                f"Cursor session id: {sid}\nEvery git commit in this repo must end with the trailer "
                f"`Session-Id: {sid}` (e.g. git commit -m \"...\" --trailer \"Session-Id: {sid}\"). "
                "It links the commit to this session's history in the dev-history repo.")}))
            return 0
    if event == "beforeSubmitPrompt":
        print(json.dumps({"continue": True}))
        return 0
    print("{}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
