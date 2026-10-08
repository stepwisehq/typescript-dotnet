#!/usr/bin/env bash
set -euo pipefail
repo_root="$(cd "$(dirname "$0")/.." && pwd)"
mapfile -t upstream_values < <(python3 - "$repo_root/upstream.lock.json" <<'PY'
import json,sys
lock=json.load(open(sys.argv[1]))
print(lock['repository'])
print(lock['revision'])
print(lock['checkout'])
PY
)
upstream_repository="${upstream_values[0]}"
upstream_commit="${upstream_values[1]}"
upstream_dir="$repo_root/${upstream_values[2]}"
if [[ ! -e "$upstream_dir" ]]; then
 mkdir -p "$repo_root/upstream"
 git init "$upstream_dir"
 git -C "$upstream_dir" remote add origin "$upstream_repository"
 git -C "$upstream_dir" fetch --depth=1 origin "$upstream_commit"
 git -C "$upstream_dir" checkout --detach FETCH_HEAD
fi
[[ -d "$upstream_dir/.git" ]] || { echo "Upstream destination is not an independent Git checkout" >&2; exit 1; }
actual_origin="$(git -C "$upstream_dir" remote get-url origin)"
[[ "${actual_origin%.git}" == "${upstream_repository%.git}" ]] || { echo "Upstream origin does not match lock file" >&2; exit 1; }
[[ "$(git -C "$upstream_dir" rev-parse HEAD)" == "$upstream_commit" ]] || { echo "Upstream checkout is not the pinned commit; existing checkout left untouched" >&2; exit 1; }
git -C "$upstream_dir" diff --quiet HEAD -- || { echo "Tracked upstream files differ from pinned commit; existing changes left untouched" >&2; exit 1; }
