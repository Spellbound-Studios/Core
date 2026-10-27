#!/usr/bin/env bash
set -euo pipefail

usage() {
    cat <<USAGE
Usage: bash scripts/unity-ledger.sh [--project <folder>] [--move-form] [--keep-editor]

Runs the Unity ledger check, the operation "core unity-ledger", through the Unity command line and exits with
its code: 0 when no row is stale or wrong, 6 when the check failed, 12 when no editor could be reached. An
editor already open on the project is used as it is; otherwise a headless editor on the project's own version
is started for the run and closed afterwards (--keep-editor leaves it running). --move-form prints the lines
for the Move issue form's "Ledger rows that change" field instead of the report.

Defaults: the project is the repository root, the editor version is ProjectSettings/ProjectVersion.txt.
USAGE
}

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
project="$root"
move_form=false
keep_editor=false

while [ $# -gt 0 ]; do
    case "$1" in
        --project) project="$2"; shift 2 ;;
        --move-form) move_form=true; shift ;;
        --keep-editor) keep_editor=true; shift ;;
        -h|--help) usage; exit 0 ;;
        *) echo "unknown argument: $1"; usage; exit 2 ;;
    esac
done

to_native() {
    if command -v cygpath >/dev/null 2>&1; then cygpath -w "$1"; else printf '%s' "$1"; fi
}

native_project="$(to_native "$project")"
line="core unity-ledger"

if [ "$move_form" = true ]; then
    line="core unity-ledger --move-form true"
fi

editor_answers() {
    unity command editor_status --project-path "$native_project" --timeout 10 >/dev/null 2>&1
}

started_editor=false

if ! editor_answers; then
    editor_version="$(grep '^m_EditorVersion:' "$project/ProjectSettings/ProjectVersion.txt" | head -n 1 | sed 's/^m_EditorVersion:[[:space:]]*//' | tr -d '\r')"
    editor_exe="$(unity editors --format tsv --no-header 2>/dev/null | awk -F'\t' -v v="$editor_version" '$1 == v { print $4 }' | head -n 1)"

    if [ -z "$editor_exe" ]; then
        echo "no installed editor matches $editor_version (unity editors lists them)"
        exit 12
    fi

    log="${TMPDIR:-/tmp}/spellbound-unity-ledger-$$.log"
    echo "starting a headless editor $editor_version on $native_project (log $log)"
    "$editor_exe" -batchmode -nographics -projectPath "$native_project" -logFile "$(to_native "$log")" &
    started_editor=true
    waited=0

    until editor_answers; do
        sleep 5
        waited=$((waited + 5))

        if [ "$waited" -ge 600 ]; then
            echo "the editor did not answer within 600 s; read $log"
            exit 12
        fi
    done
fi

set +e
unity command spellbound_run --project-path "$native_project" --line "$line" --result-only --timeout 300
code=$?
set -e

if [ "$started_editor" = true ] && [ "$keep_editor" = false ]; then
    unity close "$native_project" >/dev/null 2>&1 || true
fi

if [ "$code" -eq 0 ]; then
    exit 0
fi

if [ "$code" -eq 12 ]; then
    echo "no editor answered on $native_project"
    exit 12
fi

exit 6
