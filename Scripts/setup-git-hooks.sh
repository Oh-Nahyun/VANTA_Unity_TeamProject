#!/usr/bin/env sh
set -eu

script_dir=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
repo_root=$(CDPATH= cd -- "$script_dir/.." && pwd)

cd "$repo_root"

if [ "${1:-}" != "--skip-tool-restore" ]; then
    dotnet tool restore
fi

dotnet husky install

hooks_path=$(git config --get core.hooksPath || true)

if [ "$hooks_path" != ".husky" ] || [ ! -f ".husky/_/husky.sh" ]; then
    printf '%s\n' "Git hooks installation failed. Expected core.hooksPath='.husky' and .husky/_/husky.sh to exist." >&2
    exit 1
fi

printf '%s\n' "Git hooks are installed for this repository."
