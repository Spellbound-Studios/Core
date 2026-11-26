#!/usr/bin/env bash
set -euo pipefail

usage() {
    echo 'usage: bash scripts/sprint-report.sh [--sprint "Sprint N"] [--org <organisation>] [--board <number>]' >&2
}

organisation="Spellbound-Studios"
board=13
wanted=""
while [ "$#" -gt 0 ]; do
    case "$1" in
        --sprint) wanted="${2:-}"; shift 2 ;;
        --org) organisation="${2:-}"; shift 2 ;;
        --board) board="${2:-}"; shift 2 ;;
        *) usage; exit 2 ;;
    esac
done

today="$(date +%F)"

sprints="$(gh api graphql -f query="query { organization(login: \"$organisation\") { projectV2(number: $board) { fields(first: 50) { nodes { ... on ProjectV2IterationField { name configuration { duration iterations { title startDate duration } completedIterations { title startDate duration } } } } } } } }" \
    --jq '.data.organization.projectV2.fields.nodes[] | select(.name == "Sprint") | .configuration | (.iterations + .completedIterations)[] | "\(.title)\t\(.startDate)\t\(.duration)"')"

if [ -z "$sprints" ]; then
    echo "The board has no Sprint field" >&2
    exit 1
fi

epoch() {
    date -d "$1" +%s 2>/dev/null || date -j -f %Y-%m-%d "$1" +%s
}

sprint=""
start=""
length=""
while IFS=$'\t' read -r title first_day days; do
    [ -n "$title" ] || continue
    if [ -n "$wanted" ]; then
        if [ "$title" = "$wanted" ]; then
            sprint="$title"; start="$first_day"; length="$days"
        fi
    else
        begin="$(epoch "$first_day")"
        finish=$((begin + days * 86400))
        now="$(epoch "$today")"
        if [ "$now" -ge "$begin" ] && [ "$now" -lt "$finish" ]; then
            sprint="$title"; start="$first_day"; length="$days"
        fi
    fi
done <<< "$sprints"

if [ -z "$sprint" ]; then
    if [ -n "$wanted" ]; then
        echo "No sprint is called $wanted. Sprints:" >&2
    else
        echo "No sprint runs today ($today). Sprints:" >&2
    fi
    printf '%s\n' "$sprints" | awk -F '\t' '{ print "  " $1 " starts " $2 }' >&2
    exit 1
fi

day=$((($(epoch "$today") - $(epoch "$start")) / 86400 + 1))
last_day="$(date -d "$start + $((length - 1)) days" +%F 2>/dev/null || date -j -v+$((length - 1))d -f %Y-%m-%d "$start" +%F)"

query='query($endCursor: String) { organization(login: "ORG") { projectV2(number: BOARD) { items(first: 100, after: $endCursor) { pageInfo { hasNextPage endCursor } nodes {
  status: fieldValueByName(name: "Status") { ... on ProjectV2ItemFieldSingleSelectValue { name } }
  content { ... on Issue { number title body repository { name } issueType { name } assignees(first: 5) { nodes { login } } } ... on DraftIssue { title } }
} } } } }'
query="${query/ORG/$organisation}"
query="${query/BOARD/$board}"

rows="$(gh api graphql --paginate -f query="$query" \
    --jq '.data.organization.projectV2.items.nodes[] | [
        (.status.name // "No status"),
        (.content.issueType.name // "Draft"),
        ((.content.repository.name // "draft") + (if .content.number then "#" + (.content.number | tostring) else "" end)),
        (.content.title // "untitled"),
        ((.content.assignees.nodes // []) | map(.login) | join(",") | if . == "" then "nobody" else . end),
        ((.content.body // "") | gsub("\r"; "") | [splits("\n?### ")]
            | map(select(startswith("Needs from other repositories\n"))) | (.[0] // "")
            | sub("^Needs from other repositories\n"; "") | gsub("[[:space:]]+"; " ") | sub("^ "; "") | sub(" $"; ""))
    ] | @tsv')"

if [ "$day" -lt 1 ]; then
    echo "$sprint, $start to $last_day, starts in $((1 - day)) days"
elif [ "$day" -gt "$length" ]; then
    echo "$sprint, $start to $last_day, ended $((day - length)) days ago"
else
    echo "$sprint, $start to $last_day, day $day of $length"
fi
echo

printf '%s\n' "$rows" | awk -F '\t' '
$2 == "Initiative" || $2 == "Epic" {
    if (!($1 in count)) { states[++state_count] = $1 }
    count[$1]++
    work[++work_count] = $1 FS $3 " " $4 FS $5
    entries[$1] = entries[$1] sprintf("  %s %s (%s, %s)\n", $3, $4, $2, join_people($5))
    if ($2 == "Epic" && $6 != "" && $6 != "None") waiting = waiting sprintf("  %s %s waits on %s\n", $3, $4, $6)
}
function join_people(people) { gsub(",", ", ", people); return people }
END {
    if (work_count == 0) { print "The board has no initiatives or epics."; exit }
    known = "Draft|Ready|In progress|In review|Red team|Done"
    known_count = split(known, order, "|")
    for (i = 1; i <= state_count; i++) if (index("|" known "|", "|" states[i] "|") == 0) order[++known_count] = states[i]
    separator = ""
    for (i = 1; i <= known_count; i++) {
        state = order[i]
        if (!(state in count)) continue
        printf "%s%s: %d\n%s", separator, state, count[state], entries[state]
        separator = "\n"
        for (j = 1; j <= work_count; j++) {
            split(work[j], fields, FS)
            if (fields[1] != state) continue
            person_total = split(fields[3], people, ",")
            for (k = 1; k <= person_total; k++) {
                if (!(people[k] in assigned)) persons[++person_count] = people[k]
                assigned[people[k]] = assigned[people[k]] sprintf("  %s (%s)\n", fields[2], state)
            }
        }
    }
    if (waiting != "") printf "\nWaiting:\n%s", waiting
    printf "\nBy person:\n"
    for (i = 1; i <= person_count; i++) printf "%s:\n%s", persons[i], assigned[persons[i]]
}'
