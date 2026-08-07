#!/usr/bin/env bash
# Cloud Agent install script for DapperDemo.
#
# Idempotent bootstrap that a fresh Cloud Agent runs after the repository is
# checked out. It installs the .NET 10 SDK, initialises the AvaloniaFramework
# git submodule (a ProjectReference the build cannot restore without), and warms
# the NuGet cache by building the Linux-relevant projects.
#
# Only the Desktop head and the data-layer test project are built here: the
# iOS/Android/MacOS heads in DapperDemo.sln need mobile workloads that do not
# exist on this Linux image, so `dotnet build DapperDemo.sln` would fail on them.
set -euo pipefail

DOTNET_ROOT="$HOME/.dotnet"
export DOTNET_ROOT
export PATH="$DOTNET_ROOT:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1

REPO_ROOT="$(git -C "$(dirname "${BASH_SOURCE[0]}")" rev-parse --show-toplevel)"
cd "$REPO_ROOT"

# 1. .NET 10 SDK (skip the ~235 MB download when a 10.x SDK is already present).
if ! "$DOTNET_ROOT/dotnet" --list-sdks 2>/dev/null | grep -q '^10\.'; then
    curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
    bash /tmp/dotnet-install.sh --channel 10.0 --install-dir "$DOTNET_ROOT"
fi

# 2. Make the SDK visible to future interactive agent shells (guarded append).
if ! grep -q 'DapperDemo .NET SDK' "$HOME/.bashrc" 2>/dev/null; then
    cat >> "$HOME/.bashrc" <<'PROFILE'

# DapperDemo .NET SDK
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$DOTNET_ROOT:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
PROFILE
fi

# 3. AvaloniaFramework submodule. Its .gitmodules URL is SSH; rewrite it to HTTPS
#    so the Cloud Agent's checkout token can clone it.
git config --global url."https://github.com/".insteadOf "git@github.com:"
git submodule update --init --recursive

# 4. Restore + build the Linux-relevant projects (warms the NuGet cache).
dotnet build DapperDemo/app/DapperDemo.Desktop/DapperDemo.Desktop.csproj -c Debug
dotnet build DapperDemo/tests/Tests.Dapper/Tests.Dapper.csproj -c Debug
