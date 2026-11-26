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
