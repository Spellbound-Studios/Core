#!/usr/bin/env bash
set -euo pipefail

usage() {
    cat >&2 <<'TEXT'
usage: bash scripts/render-issue-body.sh <kind> [--<field> "<text>"]...

Prints an issue body with the same headings the issue form of that kind produces, so an issue an agent
opens with gh reads like one a person opened through the form. A field left out or empty is None.

kinds and fields:
  initiative  --shown-in --target --done-when --out-of-scope
  epic        --initiative --done-when --target-version --needs-from-other-repositories --out-of-scope --red-team
  story       --behavior --checks --days --waits-on --time-box --default-answer --needed-by
  bug         --found-in --expected --how-to-reproduce --actual --severity --regression-test
  task        --why --done-when --estimate

A bug's --severity is Blocker, Major or Minor.

example:
  gh issue create --repo Spellbound-Studios/Inventory --type Story --title "A player can split a stack" \
      --body "$(bash scripts/render-issue-body.sh story --behavior "..." --checks "..." --days 3)"
TEXT
}

if [ "$#" -lt 1 ]; then
    usage
    exit 2
fi

kind="$1"
shift

case "$kind" in
    initiative) fields="shown-in:Shown in|target:Target|done-when:Done when|out-of-scope:Out of scope" ;;
    epic) fields="initiative:Initiative|done-when:Done when|target-version:Target version|needs-from-other-repositories:Needs from other repositories|out-of-scope:Out of scope|red-team:Red team" ;;
    story) fields="behavior:Behavior|checks:Checks|days:Days|waits-on:Waits on|time-box:Time box|default-answer:Default answer|needed-by:Needed by" ;;
    bug) fields="found-in:Found in|expected:Expected|how-to-reproduce:How to reproduce|actual:Actual|severity:Severity|regression-test:Regression test" ;;
    task) fields="why:Why|done-when:Done when|estimate:Estimate" ;;
    decision)
        echo "A decision is a story: its Behavior is the choice to make, its Checks name the options and the default." >&2
        exit 1
        ;;
    *) echo "Unknown kind: $kind" >&2; usage; exit 2 ;;
esac

declare -A values
while [ "$#" -gt 0 ]; do
    case "$1" in
        --*)
            name="${1#--}"
            if ! printf '%s' "|$fields|" | grep -q "|$name:"; then
                echo "Unknown field for $kind: --$name" >&2
                usage
                exit 2
            fi
            values["$name"]="${2:-}"
            [ "$#" -ge 2 ] || set -- "$1" ""
            shift 2
            ;;
        *) usage; exit 2 ;;
    esac
done

severity="${values[severity]:-}"
case "$severity" in
    "" | Blocker | Major | Minor) ;;
    *) echo "Severity is Blocker, Major or Minor, not $severity." >&2; exit 1 ;;
esac

first=1
IFS='|' read -r -a entries <<< "$fields"
for entry in "${entries[@]}"; do
    name="${entry%%:*}"
    label="${entry#*:}"
    value="${values[$name]:-}"
    [ "$first" = 1 ] || printf '\n'
    first=0
    printf '### %s\n\n%s\n' "$label" "${value:-None}"
done
