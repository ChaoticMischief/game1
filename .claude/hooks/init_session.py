#!/usr/bin/env python3
"""SessionStart hook: create this session's folder in the dev repo and record session_start.

Also prints the session id so Claude knows which Session-Id trailer to put on
commits (SessionStart stdout is added to the model's context).
"""
import json
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import devrepo_common as dc  # noqa: E402

AGENT = "claude-code"


def main():
    try:
        hook = json.load(sys.stdin)
    except ValueError:
        return 0
    session_id = hook.get("session_id")
    if not session_id:
        return 0

    print(f"Claude Code session id: {session_id}\n"
          f"Every git commit in this repo must end with the trailer `Session-Id: {session_id}` "
          f"(e.g. git commit -m \"...\" --trailer \"Session-Id: {session_id}\").")

    dev_repo = dc.dev_repo_path()
    if not dev_repo:
        return 0
    try:
        session_dir = dc.session_dir_for(dev_repo, AGENT, session_id)
        meta_path = session_dir / "metadata.json"
        meta = dc.read_json(meta_path, {}) or {}
        now = dc.now_iso()
        meta.setdefault("session_id", session_id)
        meta.setdefault("agent", AGENT)
        meta.setdefault("start_time", now)
        meta.setdefault("last_synced_time", None)
        meta.setdefault("last_synced_line", 0)
        meta["game1_head"] = dc.game1_head()
        dc.write_json_atomic(meta_path, meta)

        source = hook.get("source") or "startup"
        dc.append_events(session_dir / "events.ndjson", [dc.make_event(
            AGENT, session_id, "session_start",
            f"Claude Code session {source} (game1 HEAD {(meta['game1_head'] or 'none')[:8]})",
            game1_sha=meta["game1_head"])])
    except Exception as exc:  # never break session startup
        dc.log_error(dev_repo, f"init_session failed for {session_id}: {exc!r}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
