#!/usr/bin/env bash
# Set up a Mac to work on game1, from a fresh clone of this repo. Safe to re-run.
#
#   git clone https://github.com/ChaoticMischief/game1.git ~/dev/game1
#   ~/dev/game1/tools/bootstrap-macos.sh
#
# Run it in a real terminal: the .NET SDK installer asks for your sudo password.
#
# Does:
#   1. Homebrew packages: Unity Hub, Rider, Cursor, .NET SDK, git-lfs, gitleaks, gh
#   2. Per-clone git config: tracked hooks (.githooks) and the UnityYAMLMerge driver
#   3. Clones the dev-history repo where .claude/dev-repo-config.json says it lives
#   4. Restores Claude Code's auto-memory for this project from the dev repo's mirror
#      (only when this machine has none yet, so it never overwrites newer local memory)
#   5. Installs the Unity editor version in ProjectSettings/ProjectVersion.txt with
#      Windows (Mono) + WebGL build support
# Then prints the steps that need a person (sign-ins, license, editor preference).
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"
step() { printf '\n==> %s\n' "$*"; }

command -v brew >/dev/null || { echo "Install Homebrew first: https://brew.sh" >&2; exit 1; }

step "Homebrew packages"
brew install git-lfs gitleaks gh
for cask in unity-hub rider cursor dotnet-sdk; do
  brew list --cask "$cask" >/dev/null 2>&1 || brew install --cask "$cask"
done

step "Git config for this clone"
git lfs install --skip-repo >/dev/null   # global LFS filters; hooks come from .githooks
git config core.hooksPath .githooks
UNITY_VERSION="$(sed -n 's/^m_EditorVersion: //p' ProjectSettings/ProjectVersion.txt)"
UNITY_CHANGESET="$(sed -n 's/^m_EditorVersionWithRevision: .*(\(.*\))/\1/p' ProjectSettings/ProjectVersion.txt)"
UNITY_APP="/Applications/Unity/Hub/Editor/$UNITY_VERSION/Unity.app"
git config merge.unityyamlmerge.name "Unity SmartMerge"
git config merge.unityyamlmerge.driver "'$UNITY_APP/Contents/Helpers/UnityYAMLMerge' merge -h -p --force %O %B %A %A"
git config merge.unityyamlmerge.recursive binary
git lfs pull

step "Dev-history repo"
read -r DEV_PATH DEV_URL < <(python3 -c '
import json; c = json.load(open(".claude/dev-repo-config.json"))
print(c["dev_repo_path"], c.get("dev_repo_url", ""))')
case "$DEV_PATH" in /*) DEV_DIR="$DEV_PATH" ;; *) DEV_DIR="$ROOT/$DEV_PATH" ;; esac
if [ -d "$DEV_DIR/.git" ]; then
  echo "already present: $DEV_DIR"
elif [ -n "$DEV_URL" ]; then
  git clone "$DEV_URL" "$DEV_DIR"
else
  echo "no dev_repo_url in .claude/dev-repo-config.json; clone the dev repo to $DEV_DIR yourself" >&2
fi

step "Claude Code memory"
# Claude Code keeps per-project data in ~/.claude/projects/<path with non-alphanumerics as ->.
MEM_DIR="$HOME/.claude/projects/$(printf '%s' "$ROOT" | sed 's/[^A-Za-z0-9]/-/g')/memory"
MIRROR="$DEV_DIR/memory/claude-code"
if [ -d "$MEM_DIR" ] && [ -n "$(ls -A "$MEM_DIR" 2>/dev/null)" ]; then
  echo "local memory already exists, leaving it alone: $MEM_DIR"
elif [ -d "$MIRROR" ]; then
  mkdir -p "$MEM_DIR"
  rsync -a --exclude redactions.log "$MIRROR/" "$MEM_DIR/"
  echo "restored $(ls "$MEM_DIR" | wc -l | tr -d ' ') files to $MEM_DIR"
else
  echo "no mirror at $MIRROR; skipping"
fi

step "Unity $UNITY_VERSION"
HUB="/Applications/Unity Hub.app/Contents/MacOS/Unity Hub"
if [ -d "$UNITY_APP" ]; then
  echo "already installed: $UNITY_APP"
else
  "$HUB" -- --headless install --version "$UNITY_VERSION" --changeset "$UNITY_CHANGESET" \
    --architecture "$(uname -m)" --module windows-mono --module webgl
fi

cat <<EOF

==> Done. Remaining manual steps:
  1. Unity Hub: sign in and activate a license (Personal is fine).
     Decline any offer to connect to Unity Cloud or Unity Version Control.
  2. Open the project in the Unity Editor, then Unity > Settings > External Tools >
     External Script Editor = Rider.
  3. Rider: first launch, choose a license. Cursor: sign in; trust this workspace so
     the project hooks in .cursor/hooks.json run.
  4. gh auth login (if git push asks for credentials).
EOF
