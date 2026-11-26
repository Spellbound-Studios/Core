#!/usr/bin/env bash
set -uo pipefail

root="$(cd "${1:-.}" && pwd)"
cd "$root"

rulebook="${RULEBOOK_ADDRESS-https://github.com/Spellbound-Studios/Roadmap/blob/main/Rulebook.md}"
failures=0
fail() {
    echo "failed: $1"
    failures=$((failures + 1))
}

if [ ! -f rulebook-address.txt ]; then
    fail "rulebook-address.txt is missing"
else
    address="$(tr -d '\r\n' < rulebook-address.txt)"
    entries="$(tr -d '\r' < rulebook-address.txt | grep -c . || true)"
    [ "$entries" = 1 ] || fail "rulebook-address.txt must hold one address, it holds $entries"
    [ "$address" = "$rulebook" ] || fail "rulebook-address.txt does not hold the rulebook's address $rulebook: $address"
fi

shopt -s nullglob
for file in AGENTS.md CONTRIBUTING.md Packages/*/AGENTS.md Packages/*/README.md .github/ISSUE_TEMPLATE/*.yml; do
    [ -f "$file" ] || continue
    case "$file" in
        .github/ISSUE_TEMPLATE/bug.yml|.github/ISSUE_TEMPLATE/config.yml) continue ;;
    esac
    if ! grep -qF "$rulebook" "$file" && ! grep -qF 'rulebook-address.txt' "$file"; then
        fail "$file does not point at the rulebook ($rulebook)"
    fi
done
shopt -u nullglob

copies="$(grep -rlF --exclude-dir=.git --exclude-dir=Library --exclude-dir=Tests --exclude-dir='*~' --exclude=check-rulebook-pointer.sh -e '# Spellbound Studios rulebook' -e 'GitHub issues in each repository are the roadmap.' . 2>/dev/null | sed 's|^\./||' || true)"
for file in $copies; do
    fail "$file copies the rulebook instead of pointing at it"
done

if [ "$failures" = 0 ]; then
    echo "succeeded: every pointer names the rulebook and nothing copies it"
    exit 0
fi
exit 1
