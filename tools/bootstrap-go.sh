#!/usr/bin/env bash
set -euo pipefail
repo_root="$(cd "$(dirname "$0")/.." && pwd)"
cache_dir="$repo_root/tools/.cache"
go_version=1.27.1
go_sha256=63d339f0da5ab53635a56f2490a7984dfe12dfcff22ad749f63edaf590168445
if [[ -x "$cache_dir/go/bin/go" ]] && [[ "$("$cache_dir/go/bin/go" version)" == "go version go$go_version linux/amd64" ]]; then
  "$cache_dir/go/bin/go" version
  exit 0
fi
mkdir -p "$cache_dir/downloads"
archive="$cache_dir/downloads/go$go_version.linux-amd64.tar.gz"
[[ -f "$archive" ]] || curl -fsSL "https://go.dev/dl/go$go_version.linux-amd64.tar.gz" -o "$archive"
printf '%s  %s\n' "$go_sha256" "$archive" | sha256sum --check
extract_dir="$(mktemp -d "$cache_dir/go-extract.XXXXXX")"
tar -xzf "$archive" -C "$extract_dir"
if [[ -d "$cache_dir/go" ]]; then mv "$cache_dir/go" "$cache_dir/go.previous.$(date +%s)"; fi
mv "$extract_dir/go" "$cache_dir/go"
rmdir "$extract_dir"
"$cache_dir/go/bin/go" version
