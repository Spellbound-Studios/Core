#!/usr/bin/env bash
set -euo pipefail

if [ "$#" -ne 2 ] || [ "$1" != "--title" ]; then
    echo "usage: check-commit-message.sh --title \"<text>\"" >&2
    echo "Commits on a branch are free: only the pull request title is checked." >&2
    exit 2
fi
title="$2"

if [ "$(printf '%s' "$title" | wc -l)" -eq 0 ] && printf '%s\n' "$title" | grep -Eq '^(feat|fix|perf|refactor|docs|test|build|ci|chore)(\([a-z0-9._-]+\))?!?: .{1,100}$'; then
    exit 0
fi

echo "Invalid title: $title" >&2
echo "Expected: type(scope): what changed, with an optional ! before the colon for a breaking change" >&2
echo "For example: feat(terrain): draw terrain to the horizon" >&2
echo "Allowed types: feat fix perf refactor docs test build ci chore" >&2
exit 1
