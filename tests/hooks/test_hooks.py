"""Tests for the agent hooks in .claude/hooks/ and .cursor/hooks/.

Run: python3 -m unittest discover -s tests/hooks -v

The capture tests build a throwaway game repo + dev-history repo in a temp dir, so they
never touch the real dev-history repo (which CI can't see anyway). Tests that need
gitleaks are skipped when it isn't installed.
"""
import json
import os
import shutil
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(REPO / ".claude" / "hooks"))

import block_secret_reads as policy  # noqa: E402
import devrepo_common as dc  # noqa: E402


def run_hook(script, payload, cwd):
    proc = subprocess.run([sys.executable, str(script)], input=json.dumps(payload),
                          capture_output=True, text=True, cwd=cwd)
    return proc.returncode, proc.stdout, proc.stderr


class FileToolPolicy(unittest.TestCase):
    def test_blocks_secret_files(self):
        for path in [".env", "config/.env.local", "certs/server.pem", "deploy/signing.key",
                     "secrets/steam.txt", "/abs/credentials/prod.json", "notes/my-secret-token",
                     "aws_credentials"]:
            self.assertTrue(policy.is_sensitive(path), path)

    def test_allows_game_code_and_assets(self):
        for path in ["Assets/Scripts/SecretDoor.cs", "Assets/Scenes/SecretLevel.unity",
                     "README.md", "Assets/Settings/URP.asset", "docs/credentials-howto.md"]:
            self.assertFalse(policy.is_sensitive(path), path)


class ShellPolicy(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.cwd = self.tmp.name
        Path(self.cwd, "api-secret.json").write_text("{}")
        Path(self.cwd, "secrets").mkdir()

    def tearDown(self):
        self.tmp.cleanup()

    def blocked(self, command):
        return policy.blocked_target("Bash", {"command": command}, self.cwd)

    def test_words_that_are_not_paths_pass(self):
        for command in ["gh secret set UNITY_EMAIL -R ChaoticMischief/game1",
                        "gh api -X PATCH repos/o/r -f security_and_analysis.secret_scanning.status=enabled",
                        "grep -rn credential docs/",
                        "cat nonexistent-secret.json", "git commit -m 'rotate credentials'"]:
            self.assertIsNone(self.blocked(command), command)

    def test_bare_word_passes_when_no_such_directory(self):
        with tempfile.TemporaryDirectory() as empty:
            self.assertIsNone(policy.blocked_target("Bash", {"command": "echo list secrets"}, empty))

    def test_secret_paths_are_blocked(self):
        for command, target in [("cat .env", ".env"), ("source .env.production", ".env.production"),
                                ("openssl x509 -in server.pem", "server.pem"),
                                ("cat config/credentials/prod.json", "config/credentials/prod.json"),
                                ("ls secrets", "secrets"),
                                ("cat api-secret.json", "api-secret.json")]:
            self.assertEqual(self.blocked(command), target, command)

    def test_main_exit_codes(self):
        hook = REPO / ".claude" / "hooks" / "block_secret_reads.py"
        code, _, err = run_hook(hook, {"tool_name": "Bash", "tool_input": {"command": "cat .env"},
                                       "cwd": self.cwd}, self.cwd)
        self.assertEqual(code, 2)
        self.assertIn(".env", err)
        code, _, _ = run_hook(hook, {"tool_name": "Bash", "tool_input": {"command": "gh secret list"},
                                     "cwd": self.cwd}, self.cwd)
        self.assertEqual(code, 0)
        code, _, _ = run_hook(hook, {"tool_name": "Read", "tool_input": {"file_path": "/x/.env"}}, self.cwd)
        self.assertEqual(code, 2)


class CursorPermissions(unittest.TestCase):
    HOOK = REPO / ".cursor" / "hooks" / "cursor_hook.py"

    def decide(self, payload):
        code, out, _ = run_hook(self.HOOK, payload, REPO)
        self.assertEqual(code, 0)
        return json.loads(out)["permission"]

    def test_decisions(self):
        cases = [
            ({"hook_event_name": "beforeReadFile", "file_path": "/p/.env", "content": ""}, "deny"),
            ({"hook_event_name": "beforeReadFile", "file_path": "/p/Assets/SecretDoor.cs"}, "allow"),
            ({"hook_event_name": "beforeShellExecution", "command": "cat a/credentials/b.json", "cwd": "/"}, "deny"),
            ({"hook_event_name": "beforeShellExecution", "command": "gh secret list", "cwd": "/"}, "allow"),
            ({"hook_event_name": "preToolUse", "tool_name": "Grep",
              "tool_input": {"pattern": "x", "path": "secrets/"}}, "deny"),
            ({"hook_event_name": "preToolUse", "tool_name": "Write", "tool_input": {"file_path": ".env"}}, "allow"),
        ]
        for payload, expected in cases:
            self.assertEqual(self.decide(payload), expected, payload)


@unittest.skipUnless(dc.gitleaks_bin(), "gitleaks not installed")
class CursorCapture(unittest.TestCase):
    """End to end: Cursor hook events -> session folder in a throwaway dev repo."""

    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        root = Path(self.tmp.name)
        self.game = root / "game"
        self.dev = root / "game-dev"
        self.game.mkdir()
        for d in (".claude", ".cursor"):
            shutil.copytree(REPO / d, self.game / d, ignore=shutil.ignore_patterns("__pycache__"))
        (self.game / ".claude" / "dev-repo-config.json").write_text(json.dumps({"dev_repo_path": "../game-dev"}))
        for repo in (self.game, self.dev):
            subprocess.run(["git", "init", "-q", str(repo)], check=True)
            subprocess.run(["git", "-C", str(repo), "-c", "user.name=t", "-c", "user.email=t@t",
                            "commit", "-q", "--allow-empty", "-m", "init"], check=True)
        self.transcript = root / "transcript.jsonl"
        self.transcript.write_text('{"text":"aws key AKIAIOSFODNN7EXAMPLF"}\n')

    def tearDown(self):
        self.tmp.cleanup()

    def event(self, name, **fields):
        payload = {"hook_event_name": name, "conversation_id": "conv-1", "session_id": "conv-1",
                   "cursor_version": "test", "model": "m", "transcript_path": str(self.transcript), **fields}
        code, out, err = run_hook(self.game / ".cursor" / "hooks" / "cursor_hook.py", payload, self.game)
        self.assertEqual(code, 0, err)
        return json.loads(out)

    def test_session_is_captured_redacted_with_commits(self):
        start = self.event("sessionStart", composer_mode="agent")
        self.assertIn("Session-Id: conv-1", start["additional_context"])
        self.event("beforeSubmitPrompt", prompt="make a thing")
        self.event("postToolUse", tool_name="Shell", tool_input={"command": "ls"}, tool_output="{}")
        self.event("afterAgentResponse", text="done")
        # A commit the sandboxed post-commit hook couldn't record.
        subprocess.run(["git", "-C", str(self.game), "-c", "user.name=t", "-c", "user.email=t@t",
                        "-c", "core.hooksPath=/dev/null", "commit", "-q", "--allow-empty",
                        "-m", "agent commit", "--trailer", "Session-Id: conv-1"], check=True)
        self.event("stop", status="completed", loop_count=0)
        self.event("stop", status="completed", loop_count=0)  # second flush must not duplicate

        session = next((self.dev / "sessions" / "cursor").glob("*_conv-1"))
        events = [json.loads(l) for l in (session / "events.ndjson").read_text().splitlines()]
        types = [e["event_type"] for e in events]
        self.assertEqual(types.count("commit"), 1)
        for t in ("session_start", "user_prompt", "tool_call", "tool_result", "assistant_response"):
            self.assertIn(t, types)
        self.assertTrue(all(e["agent"] == "cursor" for e in events))
        transcript = (session / "transcript.jsonl").read_text()
        self.assertNotIn("AKIAIOSFODNN7EXAMPLF", transcript)
        self.assertIn("[REDACTED:", transcript)
        self.assertEqual(json.loads((session / "metadata.json").read_text())["agent"], "cursor")

    def test_claude_hooks_stand_down_under_cursor(self):
        payload = {"hook_event_name": "sessionStart", "session_id": "conv-1", "conversation_id": "conv-1",
                   "cursor_version": "test"}
        for script in ("init_session.py", "sync_to_dev_repo.py"):
            run_hook(self.game / ".claude" / "hooks" / script, payload, self.game)
        self.assertFalse((self.dev / "sessions" / "claude-code").exists())


if __name__ == "__main__":
    unittest.main()
