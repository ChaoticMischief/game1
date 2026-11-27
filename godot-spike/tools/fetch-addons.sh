#!/usr/bin/env bash
# Fetches the third-party Godot addons the spike uses, pinned to a tag, into addons/ (gitignored).
#   gdUnit4 (tests)      https://github.com/godot-gdunit-labs/gdUnit4
#   sentry-godot (opt.)  https://github.com/getsentry/sentry-godot   pass --sentry
set -euo pipefail
cd "$(dirname "$0")/.."
GDUNIT_TAG=v6.2.3
SENTRY_TAG=2.5.1
tmp=$(mktemp -d); trap 'rm -rf "$tmp"' EXIT

if [ ! -d addons/gdUnit4 ]; then
  curl -fsSL "https://github.com/godot-gdunit-labs/gdUnit4/archive/refs/tags/$GDUNIT_TAG.tar.gz" | tar -xz -C "$tmp"
  mkdir -p addons && mv "$tmp"/gdUnit4-*/addons/gdUnit4 addons/
fi

if [ "${1:-}" = "--sentry" ] && [ ! -d addons/sentry ]; then
  curl -fsSL -o "$tmp/sentry.zip" \
    "https://github.com/getsentry/sentry-godot/releases/download/$SENTRY_TAG/$(curl -fsSL "https://api.github.com/repos/getsentry/sentry-godot/releases/tags/$SENTRY_TAG" | grep -o "sentry-godot-$SENTRY_TAG+[0-9a-f]*\.zip" | head -1)"
  unzip -q "$tmp/sentry.zip" -d "$tmp/sentry"
  mkdir -p addons && mv "$tmp"/sentry/addons/sentry addons/
fi
