#!/usr/bin/env bash
# Headless Unity for agents, CI and humans. Needs the editor to be CLOSED for this
# project (Unity locks a project to one editor); with the editor open, use the
# MCP for Unity tools instead (run_tests, read_console, refresh/compile, build).
#
#   tools/unity/unity.sh compile                      import + compile scripts, report C# errors
#   tools/unity/unity.sh test [editmode|playmode|all] run tests, print summary + failures
#   tools/unity/unity.sh build <mac|windows|webgl> [--development]
#   tools/unity/unity.sh sync-solution                regenerate .sln/.csproj (Rider, Cursor, csharp-ls)
#   tools/unity/unity.sh ensure-main-scene            create Assets/Scenes/Main.unity if missing
#
# Logs and results go to Logs/agent/ (gitignored). Exit code 0 = success.
set -uo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
VERSION="$(sed -n 's/^m_EditorVersion: //p' "$ROOT/ProjectSettings/ProjectVersion.txt")"
UNITY="${UNITY_EDITOR:-/Applications/Unity/Hub/Editor/$VERSION/Unity.app/Contents/MacOS/Unity}"
OUT="$ROOT/Logs/agent"
mkdir -p "$OUT"

die() { echo "unity.sh: $*" >&2; exit 2; }
[ -x "$UNITY" ] || die "Unity $VERSION not found at $UNITY (set UNITY_EDITOR, or run tools/bootstrap-macos.sh)"

# An open editor holds Temp/UnityLockfile open.
if [ -f "$ROOT/Temp/UnityLockfile" ] && lsof "$ROOT/Temp/UnityLockfile" >/dev/null 2>&1; then
  echo "unity.sh: the Unity Editor has this project open. Close it, or use the MCP for Unity tools instead." >&2
  exit 3
fi

# Run Unity in batch mode; $1 = log name, rest = extra args. Prints C# compiler errors on failure.
run_unity() {
  local name="$1"; shift
  local log="$OUT/$name.log"
  echo "unity.sh: running Unity $VERSION ($name), log: ${log#$ROOT/}"
  "$UNITY" -batchmode -nographics -projectPath "$ROOT" -logFile "$log" "$@"
  local code=$?
  local errors
  errors="$(grep -E 'error CS[0-9]+' "$log" | sort -u)"
  if [ -n "$errors" ]; then
    echo "--- C# compile errors:"; echo "$errors" | head -50
  fi
  local warnings
  warnings="$(grep -E '^Assets/.*: warning (CS|UNT)[0-9]+' "$log" | sort -u)"
  if [ -n "$warnings" ]; then
    echo "--- C# warnings in project code (fix, don't suppress):"; echo "$warnings" | head -30
  fi
  if [ $code -ne 0 ] && [ -z "$errors" ]; then
    echo "--- last error lines in log:"; grep -iE 'error|exception|failed' "$log" | grep -v 'Licensing::' | tail -20
  fi
  return $code
}

summarize_results() {  # NUnit XML from -testResults
  python3 - "$1" <<'EOF'
import sys, xml.etree.ElementTree as ET
try:
    root = ET.parse(sys.argv[1]).getroot()
except (OSError, ET.ParseError) as e:
    print(f"no test results ({e})"); sys.exit(1)
a = root.attrib
print(f"tests: {a.get('total')} total, {a.get('passed')} passed, {a.get('failed')} failed, "
      f"{a.get('skipped')} skipped ({a.get('result')}, {float(a.get('duration', 0)):.1f}s)")
for case in root.iter("test-case"):
    if case.get("result") == "Failed":
        msg = (case.findtext("failure/message") or "").strip()
        trace = (case.findtext("failure/stack-trace") or "").strip().splitlines()[:3]
        print(f"FAILED {case.get('fullname')}: {msg}")
        for line in trace: print(f"    {line}")
sys.exit(0 if a.get("result", "").startswith("Passed") else 1)
EOF
}

cmd="${1:-}"; shift || true
case "$cmd" in
  compile)
    run_unity compile -quit && echo "compile: OK"
    ;;
  test)
    mode="${1:-all}"; status=0
    case "$mode" in all) platforms="EditMode PlayMode" ;; editmode) platforms=EditMode ;;
      playmode) platforms=PlayMode ;; *) die "test mode must be editmode, playmode or all" ;; esac
    for p in $platforms; do
      results="$OUT/test-results-$p.xml"; rm -f "$results"
      run_unity "test-$p" -runTests -testPlatform "$p" -testResults "$results"
      echo "[$p]"; summarize_results "$results" || status=1
    done
    exit $status
    ;;
  build)
    case "${1:-}" in
      mac) target=StandaloneOSX ;; windows) target=StandaloneWindows64 ;; webgl) target=WebGL ;;
      *) die "build target must be mac, windows or webgl" ;;
    esac
    shift; extra=()
    [ "${1:-}" = "--development" ] && extra+=(-development)
    run_unity "build-$target" -quit -buildTarget "$target" \
      -executeMethod Game.EditorTools.AgentCommands.Build ${extra[@]+"${extra[@]}"} \
      && grep -h '\[AgentCommands\] Build' "$OUT/build-$target.log"
    ;;
  sync-solution)
    run_unity sync-solution -executeMethod Game.EditorTools.AgentCommands.SyncSolution \
      && ls "$ROOT"/*.sln "$ROOT"/*.csproj 2>/dev/null | sed "s#$ROOT/##"
    ;;
  ensure-main-scene)
    run_unity ensure-main-scene -executeMethod Game.EditorTools.AgentCommands.EnsureMainScene
    ;;
  *)
    sed -n '2,13p' "$0"; exit 2
    ;;
esac
