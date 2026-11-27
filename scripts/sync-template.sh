#!/usr/bin/env bash
set -euo pipefail

if [ "${SYNC_TEMPLATE_RUNNING_COPY:-}" != "${BASH_SOURCE[0]}" ]; then
    running_copy="$(mktemp)"
    cp "${BASH_SOURCE[0]}" "$running_copy"
    SYNC_TEMPLATE_RUNNING_COPY="$running_copy" \
        SYNC_TEMPLATE_SCRIPTS_FOLDER="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)" \
        exec "$BASH" "$running_copy" "$@"
fi

trap 'rm -f "$SYNC_TEMPLATE_RUNNING_COPY"' EXIT
source "$SYNC_TEMPLATE_SCRIPTS_FOLDER/pull-request-base.sh"

usage() {
    cat >&2 <<'TEXT'
usage: bash scripts/sync-template.sh [--template <owner/repository>] [--ref <branch>] [--check]

Compares every file named in the template's managed-files.txt with this repository's copy and, where one
drifted, opens a pull request that takes the template's copy. A workflow caller is compared with the ref
after the @ ignored, so a repository may point a caller at a branch of the shared workflows to try a
change. A game repository passes --template Spellbound-Studios/GameTemplate. --check only reports.
TEXT
}

template="Spellbound-Studios/Template"
ref="main"
check="false"
while [ "$#" -gt 0 ]; do
    case "$1" in
        --template) template="${2:-}"; shift 2 ;;
        --ref) ref="${2:-}"; shift 2 ;;
        --check) check="true"; shift ;;
        *) usage; exit 2 ;;
    esac
done

root="$(git rev-parse --show-toplevel)"
cd "$root"

package_name=""
for package_file in Packages/*/package.json; do
    [ -f "$package_file" ] || continue
    package_name="$(basename "$(dirname "$package_file")")"
done

fetch() {
    gh api "repos/$template/contents/$1?ref=$ref" -H "Accept: application/vnd.github.raw+json"
}

normalise() {
    case "$1" in
        .github/workflows/*) tr -d '\r' | sed -E 's/(uses: [^@]*)@.*/\1/' ;;
        *) tr -d '\r' ;;
    esac
}

fill() {
    if [ -n "$package_name" ] && [ "$package_name" != "PackageName" ]; then
        sed "s|PackageName|$package_name|g; s|packagename|$(printf '%s' "$package_name" | tr 'A-Z' 'a-z')|g"
    else
        cat
    fi
}

needs_fill() {
    case "$1" in
        .releaserc.yaml|Packages/*) return 0 ;;
        *) return 1 ;;
    esac
}

fill_if() {
    if needs_fill "$1"; then
        fill
    else
        cat
    fi
}

work="$(mktemp -d)"
trap 'rm -rf "$work" "$SYNC_TEMPLATE_RUNNING_COPY"' EXIT

managed="$(fetch managed-files.txt | tr -d '\r')"
drifted=""
while IFS= read -r path; do
    [ -n "$path" ] || continue
    mkdir -p "$work/$(dirname "$path")"
    if ! fetch "$path" | fill_if "$path" > "$work/$path"; then
        echo "The template has no $path on $ref" >&2
        exit 1
    fi
    if [ ! -f "$path" ]; then
        echo "missing: $path"
        drifted="$drifted$path"$'\n'
    elif ! cmp -s <(normalise "$path" < "$path") <(normalise "$path" < "$work/$path"); then
        echo "drifted: $path"
        drifted="$drifted$path"$'\n'
    fi
done <<< "$managed"

dropped=""
if [ -f managed-files.txt ]; then
    while IFS= read -r path; do
        [ -n "$path" ] && [ -e "$path" ] || continue
        printf '%s\n' "$managed" | grep -qxF -- "$path" && continue
        echo "dropped: $path"
        dropped="$dropped$path"$'\n'
    done < <(tr -d '\r' < managed-files.txt)
fi

if [ -z "$drifted" ] && [ -z "$dropped" ]; then
    echo "succeeded: every managed file matches $template@$ref"
    exit 0
fi

if [ "$check" = "true" ]; then
    exit 1
fi

if [ -n "$(git status --porcelain)" ]; then
    echo "The working tree has changes. Commit or stash them before syncing." >&2
    exit 1
fi

base="$(pull_request_base)"
branch="sync/template-$(date +%Y%m%d)"
git checkout -b "$branch"

while IFS= read -r path; do
    [ -n "$path" ] || continue
    mkdir -p "$(dirname "$path")"
    cp "$work/$path" "$path"
    git add "$path"
    case "$path" in
        .githooks/*|*.sh) git update-index --chmod=+x "$path" ;;
    esac
done <<< "$drifted"

while IFS= read -r path; do
    [ -n "$path" ] || continue
    if git ls-files --error-unmatch -- "$path" > /dev/null 2>&1; then
        git rm -q -- "$path"
    else
        rm -f -- "$path"
    fi
done <<< "$dropped"

scope="$(printf '%s' "${package_name:-$(basename "$root")}" | tr 'A-Z' 'a-z')"
address="$(tr -d '\r\n' < rulebook-address.txt 2>/dev/null || true)"
title="build($scope): sync the managed files from ${template#*/}"
body="What you will see: the managed files that drifted from $template@$ref, replaced by the template's copies. The list is in the commit. The rulebook: $address"
renormalise=""
if printf '%s' "$drifted" | grep -qFx '.gitattributes'; then
    renormalise="The line ending rules changed. Once this has merged, a person renormalises the repository once, on a branch of its own: git add --renormalize . and then commit what it staged as build($scope): renormalise the line endings"
    body="$body. $renormalise"
fi
if [ -n "$dropped" ]; then
    body="$body. Removed because the template no longer manages them: $(printf '%s' "$dropped" | paste -sd ' ' -)"
fi
own_repository="$(pull_request_repository "${template%%/*}")"
git commit -q -m "$title" -m "$body"
git push -u origin "$branch"
gh pr create --base "$base" --repo "$own_repository" --title "$title" --body "$body"
[ -z "$renormalise" ] || echo "$renormalise"
