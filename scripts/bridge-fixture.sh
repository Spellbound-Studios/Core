#!/usr/bin/env bash
set -euo pipefail

usage() {
    cat <<USAGE
Usage: bash scripts/bridge-fixture.sh [--editor <version>] [--pipeline <version>|none] [--entities <version>|none] [--player] [--expect-off] [--keep] [--work <folder>]

Makes a throwaway Unity project outside this repository that installs Core from this working tree and
com.unity.pipeline at the given version, starts a headless editor on the given editor version, and drives
the Spellbound command through the Unity command line: the command is found by search, "core help" exits 0
with "succeeded:" first, a line that does not bind, a line that fails and a line that is refused each come
back as a failure whose message starts with the status word, a dry run answers "previewed:", and a line
that spans frames completes. With --player it also builds a Windows development player with Pipeline's
server switched on and reads "core about" from the player. With --pipeline none it only proves that Core
compiles with no bridge and writes no console line; with --expect-off (a Pipeline outside every range) it
proves the bridge compiled out and the console line names the version and the ranges.

Defaults: the editor version in ProjectSettings/ProjectVersion.txt, the Pipeline and Entities versions in
Packages/manifest.json (Core's world-object assembly still needs Entities; pass --entities none to leave
the lines out and let the editor's built-in Entities resolve), the work folder under the system temp
folder. The project is removed at the end unless --keep is given. Exit codes the Unity command line gave
are printed as the run goes.
USAGE
}

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
editor_version="$(grep '^m_EditorVersion:' "$root/ProjectSettings/ProjectVersion.txt" | head -n 1 | sed 's/^m_EditorVersion:[[:space:]]*//' | tr -d '\r')"
pipeline_version="$(grep '"com.unity.pipeline"' "$root/Packages/manifest.json" | head -n 1 | sed 's/.*: *"\([^"]*\)".*/\1/')"
entities_version="$(grep '"com.unity.entities"' "$root/Packages/manifest.json" | head -n 1 | sed 's/.*: *"\([^"]*\)".*/\1/')"
player=false
expect_off=false
keep=false
work=""

while [ $# -gt 0 ]; do
    case "$1" in
        --editor) editor_version="$2"; shift 2 ;;
        --pipeline) pipeline_version="$2"; shift 2 ;;
        --entities) entities_version="$2"; shift 2 ;;
        --player) player=true; shift ;;
        --expect-off) expect_off=true; shift ;;
        --keep) keep=true; shift ;;
        --work) work="$2"; shift 2 ;;
        -h|--help) usage; exit 0 ;;
        *) echo "unknown argument: $1"; usage; exit 2 ;;
    esac
done

to_native() {
    if command -v cygpath >/dev/null 2>&1; then cygpath -w "$1"; else printf '%s' "$1"; fi
}

editor_exe="$(unity editors --format tsv --no-header 2>/dev/null | awk -F'\t' -v v="$editor_version" '$1 == v { print $4 }' | head -n 1)"

if [ -z "$editor_exe" ]; then
    echo "no installed editor matches $editor_version (unity editors lists them)"
    exit 4
fi

if [ -z "$work" ]; then
    work="${TMPDIR:-/tmp}/spellbound-bridge-fixture-$$"
fi

project="$work/BridgeFixture"
log="$work/editor.log"
mkdir -p "$project/Assets" "$project/Packages" "$project/ProjectSettings"
core_path="$(to_native "$root/Packages/Core" | sed 's|\\|/|g')"

if [ "$entities_version" = "none" ]; then
    entities_lines=""
else
    entities_lines="    \"com.unity.entities\": \"$entities_version\",
    \"com.unity.entities.graphics\": \"$entities_version\",
"
fi

if [ "$pipeline_version" = "none" ]; then
    pipeline_line=""
else
    pipeline_line="    \"com.unity.pipeline\": \"$pipeline_version\",
"
fi

printf 'm_EditorVersion: %s\n' "$editor_version" > "$project/ProjectSettings/ProjectVersion.txt"

cat > "$project/Packages/manifest.json" <<MANIFEST
{
  "dependencies": {
    "com.spellboundstudios.core": "file:$core_path",
$entities_lines    "com.unity.inputsystem": "1.19.0",
    "com.unity.render-pipelines.universal": "17.4.0",
    "com.unity.test-framework": "1.6.0",
    "com.unity.ugui": "2.0.0",
$pipeline_line    "com.unity.modules.physics": "1.0.0",
    "com.unity.modules.ui": "1.0.0",
    "com.unity.modules.uielements": "1.0.0",
    "com.unity.modules.jsonserialize": "1.0.0",
    "com.unity.modules.screencapture": "1.0.0",
    "com.unity.modules.imgui": "1.0.0",
    "com.unity.modules.unitywebrequest": "1.0.0",
    "com.unity.modules.animation": "1.0.0",
    "com.unity.modules.audio": "1.0.0",
    "com.unity.modules.particlesystem": "1.0.0",
    "com.unity.modules.terrain": "1.0.0"
  },
  "testables": [
    "com.spellboundstudios.core"
  ]
}
MANIFEST

native_project="$(to_native "$project")"
native_log="$(to_native "$log")"
echo "editor $editor_version at $editor_exe"
echo "pipeline $pipeline_version, entities $entities_version, Core from $core_path"
echo "project $native_project"

failures=0
project_arg="$native_project"
command_words=""

if [ "$pipeline_version" = "none" ]; then
    echo "running Core's edit mode tests in a one-shot editor with no Pipeline installed..."
    set +e
    unity test "$native_project" --mode EditMode --editor-version "$editor_version" --output "$(to_native "$work/test-results.xml")" --format json > "$work/test.out" 2>&1
    code=$?
    set -e
    summary="$(grep -o '<test-run [^>]*' "$work/test-results.xml" 2>/dev/null | head -n 1 | grep -o 'total="[0-9]*"\|passed="[0-9]*"\|failed="[0-9]*"' | tr -s '[:space:]' ' ' || true)"
    echo "unity test exit $code, ${summary:-no summary}"
    console_line="$(grep -rl "Spellbound agent road off" "$project/Logs" "$work" 2>/dev/null | head -n 1 || true)"

    if [ "$keep" = false ]; then
        rm -rf "$work"
    else
        echo "kept $work"
    fi

    if [ "$code" -ne 0 ]; then
        echo "FAIL Core's tests did not pass without Pipeline; read $work/test.out and $native_log"
        exit 6
    fi

    if [ -n "$console_line" ]; then
        echo "FAIL the console line was written although no Pipeline is installed: $console_line"
        exit 6
    fi

    echo "all checks passed: editor $editor_version, no Pipeline, Core compiled, its tests passed, no console line"
    exit 0
fi

"$editor_exe" -batchmode -nographics -projectPath "$native_project" -logFile "$native_log" &
editor_pid=$!

stop_editor() {
    unity close "$project_arg" >/dev/null 2>&1 || true
    sleep 3

    if kill -0 "$editor_pid" 2>/dev/null; then
        kill "$editor_pid" 2>/dev/null || true
    fi

    if [ "$keep" = false ]; then
        rm -rf "$work"
    else
        echo "kept $work"
    fi
}

trap stop_editor EXIT

editor_ready() {
    local answer
    answer="$(unity command --project-path "$project_arg" editor_status 2>&1 || true)"

    if printf '%s' "$answer" | grep -q "renamed"; then
        answer="$(unity command --project-path "$project_arg" editor status 2>&1 || true)"
    fi

    printf '%s' "$answer" | grep -q '"status":"ready"'
}

wait_ready() {
    local waited=0

    until editor_ready; do
        sleep 5
        waited=$((waited + 5))

        if ! kill -0 "$editor_pid" 2>/dev/null; then
            echo "the editor exited; the last lines of its log:"
            tail -n 40 "$log"
            exit 6
        fi

        if [ "$waited" -ge 1500 ]; then
            echo "the editor did not become ready in 25 minutes"
            exit 6
        fi
    done
}

find_command() {
    local listing
    listing="$(unity command --project-path "$project_arg" --query spellbound 2>/dev/null || true)"

    if printf '%s' "$listing" | grep -q 'spellbound_run'; then
        command_words="spellbound_run"
    elif printf '%s' "$listing" | grep -Eq 'spellbound[[:space:]]+run|spellbound\.run'; then
        command_words="spellbound run"
    else
        echo "FAIL the Spellbound command is not listed; the bridge did not compile for Pipeline $pipeline_version"
        echo "console lines at warning level:"
        unity command --project-path "$project_arg" console --level warning 2>/dev/null | head -n 20 || true
        return 1
    fi

    echo "found: unity command $command_words --line \"...\""
}

expect() {
    local label=$1 expected_exit=$2 expected_text=$3 line=$4 output code
    set +e
    output="$(unity command --project-path "$project_arg" --format json $command_words --line "$line" 2>&1)"
    code=$?
    set -e

    if [ "$code" -eq "$expected_exit" ] && printf '%s' "$output" | grep -qF "$expected_text"; then
        echo "ok   $label: exit $code, contains \"$expected_text\""
    else
        echo "FAIL $label: exit $code (expected $expected_exit), looked for \"$expected_text\" in:"
        printf '%s\n' "$output" | head -n 12
        failures=$((failures + 1))
    fi
}

wait_for_console_line() {
    local waited=0

    until grep -q "Spellbound agent road off" "$log" 2>/dev/null; do
        sleep 5
        waited=$((waited + 5))

        if ! kill -0 "$editor_pid" 2>/dev/null; then
            echo "the editor exited; the last lines of its log:"
            tail -n 40 "$log"
            exit 6
        fi

        if [ "$waited" -ge 1500 ]; then
            echo "the editor did not write the console line in 25 minutes"
            exit 6
        fi
    done

    sleep 10
}

if [ "$expect_off" = true ]; then
    echo "waiting for the editor to import with a Pipeline outside every range..."
    wait_for_console_line

    if ! grep -q "Spellbound agent road off: com.unity.pipeline $pipeline_version" "$log"; then
        echo "FAIL the console line naming com.unity.pipeline $pipeline_version was not written"
        grep -n "agent road" "$log" | head -n 3 || true
        exit 6
    fi

    if ! grep -q "Spellbound.Core.Pipeline.dll.*skipped" "$log"; then
        echo "FAIL the bridge assembly was not compiled out"
        exit 6
    fi

    echo "all checks passed: editor $editor_version, pipeline $pipeline_version outside every range, the bridge compiled out and the console line reads:"
    grep -m 1 -o "Spellbound agent road off.*" "$log"
    exit 0
fi

echo "waiting for the editor..."
wait_ready
echo "editor ready"

if ! find_command; then
    exit 6
fi

expect "core help" 0 'succeeded: ' "core help"
expect "invalid line" 6 'invalid: no operation' "core nope"
expect "failed line" 6 'failed: No match score exists.' "fixture boom"
expect "refused line" 6 'refused: this operation deletes data' "fixture wipe"
expect "dry run" 0 'previewed: would wipe 3 things' "fixture wipe --dry-run"
expect "spans frames" 0 'succeeded: waited 1500' "fixture wait --milliseconds 1500"
expect "context" 0 'com.spellboundstudios.core' "core context"

if [ "$player" = true ]; then
    echo "building a development player with Pipeline's runtime server on..."
    build_dir="$work/Player"
    native_build="$(to_native "$build_dir" | sed 's|\\|/|g')"
    mkdir -p "$build_dir"
    unity command --project-path "$project_arg" set_runtime_pipeline_settings --settings '{"enableInBuilds":true}' --confirm true 2>&1 | tail -n 1
    unity command --project-path "$project_arg" get_runtime_pipeline_settings 2>&1 | tail -n 1
    mkdir -p "$project/Assets/Editor"
    cat > "$project/Assets/Editor/FixturePlayerBuild.cs" <<'BUILD'
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;

public static class FixturePlayerBuild {
    public static void Build() {
        var output = System.Environment.GetEnvironmentVariable("SPELLBOUND_FIXTURE_PLAYER");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        EditorSceneManager.SaveScene(scene, "Assets/Fixture.unity");
        EditorUserBuildSettings.development = true;
        var options = new BuildPlayerOptions {
            scenes = new[] { "Assets/Fixture.unity" },
            locationPathName = output + "/BridgeFixture.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development
        };
        var report = BuildPipeline.BuildPlayer(options);
        var stripping = PlayerSettings.GetManagedStrippingLevel(NamedBuildTarget.Standalone);
        System.IO.File.WriteAllText(output + "/result.txt", report.summary.result + ", " + report.summary.totalErrors + " errors, stripping " + stripping);
    }
}
BUILD
    unity command --project-path "$project_arg" eval 'UnityEditor.AssetDatabase.Refresh(); return "ok";' >/dev/null 2>&1 || true
    unity recompile --project-path "$project_arg" >/dev/null 2>&1 || true
    unity command --project-path "$project_arg" --detach eval "System.Environment.SetEnvironmentVariable(\"SPELLBOUND_FIXTURE_PLAYER\", \"$native_build\"); FixturePlayerBuild.Build(); return \"built\";" 2>&1 | tail -n 1 || true

    for i in $(seq 1 120); do
        sleep 5
        [ -f "$build_dir/result.txt" ] && break
    done

    if [ ! -f "$build_dir/result.txt" ]; then
        echo "FAIL the player build wrote no result in 10 minutes"
        failures=$((failures + 1))
    elif [ ! -f "$build_dir/BridgeFixture.exe" ]; then
        echo "FAIL player build: $(cat "$build_dir/result.txt")"
        grep -n "Error building Player" "$log" | tail -n 3 || true
        failures=$((failures + 1))
    else
        echo "player build: $(cat "$build_dir/result.txt")"
        "$build_dir/BridgeFixture.exe" -batchmode -nographics -logFile "$build_dir/player.log" &
        player_pid=$!
        sleep 20
        set +e
        answer="$(unity command --runtime BridgeFixture.exe --format json $command_words --line "core about" 2>&1)"
        code=$?

        if [ "$code" -ne 0 ]; then
            answer="$(unity command --runtime-path "$native_build" --format json $command_words --line "core about" 2>&1)"
            code=$?
        fi

        set -e
        grep -m 1 "Pipeline: Runtime server" "$build_dir/player.log" || echo "the player log has no runtime server line"
        kill "$player_pid" 2>/dev/null || true

        if [ "$code" -eq 0 ] && printf '%s' "$answer" | grep -q 'player'; then
            echo "ok   player: exit $code, the player answered core about"
            printf '%s\n' "$answer" | grep -o '"result": *"[^"]*"' | head -n 1 || true
        else
            echo "FAIL player: exit $code"
            printf '%s\n' "$answer" | head -n 12
            tail -n 20 "$build_dir/player.log" || true
            failures=$((failures + 1))
        fi
    fi
fi

if [ "$failures" -ne 0 ]; then
    echo "$failures check(s) failed"
    exit 6
fi

echo "all checks passed: editor $editor_version, pipeline $pipeline_version, command \"$command_words\""
