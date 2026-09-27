#!/usr/bin/env bash
# Set up a Mac to work on game1, from a fresh clone of this repo. Safe to re-run.
#
#   git clone https://github.com/ChaoticMischief/game1.git ~/dev/game1
#   ~/dev/game1/tools/bootstrap-macos.sh
#
# Run it in a real terminal: the .NET SDK installer asks for your sudo password.
#
# Does:
#   1. Homebrew packages: Unity Hub, Rider, Cursor, Claude Code, .NET SDK, git-lfs, gitleaks,
#      gh, uv; plus csharp-ls (C# language server for agents)
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
command -v uv >/dev/null || brew install uv   # MCP for Unity's Python server runs via uv
for cask in unity-hub rider cursor dotnet-sdk; do
  brew list --cask "$cask" >/dev/null 2>&1 || brew install --cask "$cask"
done
command -v claude >/dev/null || brew install --cask claude-code

step "C# language server for agents (csharp-ls)"
# /etc/paths.d/dotnet-cli-tools uses an unexpanded ~, so put .NET global tools on PATH ourselves.
grep -q '.dotnet/tools' ~/.zprofile 2>/dev/null || printf '\n# .NET global tools (csharp-ls etc.)\nexport PATH="$PATH:$HOME/.dotnet/tools"\n' >> ~/.zprofile
export PATH="$PATH:$HOME/.dotnet/tools"
command -v csharp-ls >/dev/null || /usr/local/share/dotnet/dotnet tool install --global csharp-ls
# Project settings enable the csharp-lsp plugin; install it so Claude Code can load it.
command -v claude >/dev/null && claude plugin install csharp-lsp@claude-plugins-official --scope project >/dev/null \
  && git checkout -- .claude/settings.json 2>/dev/null   # keep the committed formatting

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
  # Private repo: only maintainers can clone it. Everything else works without it.
  git clone "$DEV_URL" "$DEV_DIR" || echo "could not clone the dev-history repo (it's private); continuing without it"
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
     External Script Editor = Rider. MCP for Unity's server starts with the editor
     (Window > MCP for Unity shows status). Skip its "Configure clients" step: the
     committed .mcp.json and .cursor/mcp.json already point at it.
  3. With the editor closed once licensed: tools/unity/unity.sh sync-solution && tools/unity/unity.sh test
  4. Rider: first launch, choose a license. Optional: Settings > Tools > MCP Server >
     Enable, then Auto-Configure for Claude Code and Cursor (leave "brave mode" off).
  5. Cursor: sign in; trust this workspace (runs .cursor/hooks.json); Settings > MCP >
     enable unityMCP.
  6. Claude Code: run `claude` in this folder, trust it, and approve the csharp-lsp plugin
     if asked. gh auth login if git push asks for credentials.
EOF
