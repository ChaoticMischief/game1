#!/usr/bin/env bash
# Install a pinned gitleaks on a Linux CI runner (the hook tests and the secret scan need it).
set -euo pipefail
VERSION="${GITLEAKS_VERSION:-8.30.1}"
url="https://github.com/gitleaks/gitleaks/releases/download/v${VERSION}/gitleaks_${VERSION}_linux_x64.tar.gz"
curl -sSfL "$url" | sudo tar -xz -C /usr/local/bin gitleaks
gitleaks version
