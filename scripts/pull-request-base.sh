#!/usr/bin/env bash

pull_request_base() {
    if git ls-remote --exit-code --heads origin beta > /dev/null 2>&1; then
        printf '%s\n' beta
        return 0
    fi
    local default_branch
    default_branch="$(git symbolic-ref refs/remotes/origin/HEAD 2>/dev/null | sed 's|^refs/remotes/origin/||' || true)"
    printf '%s\n' "${default_branch:-main}"
}

pull_request_repository() {
    local organisation="" name
    if [ -f rulebook-address.txt ]; then
        organisation="$(tr -d '\r' < rulebook-address.txt | sed -nE 's|^https://github\.com/([^/]+)/.*$|\1|p' | head -n 1)"
    fi
    name="$(git remote get-url origin | sed -E 's|/+$||; s|\.git$||; s|.*[/:]||')"
    if [ -z "$name" ]; then
        echo "origin names no repository" >&2
        return 1
    fi
    printf '%s/%s\n' "${organisation:-$1}" "$name"
}
