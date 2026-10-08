#!/usr/bin/env bash
# Runs every crowd-physics benchmark suite on both prototype branches and collects the CSVs.
# Never touches the user's working tree: every variant runs from a detached worktree that is
# built, imported and removed here. Full documentation in this folder's README.md.
#
# Env overrides for partial reruns:
#   SUITES="core events"   subset of: sanity core blocking load load-rate events realtime
#   REPS=1                 repetitions of the throughput suites (default 3)
#   VARIANTS="characterbody"  subset of: characterbody rigidbody (default both)
#   BENCH_OUT=/path        where the CSVs and logs go (default ~/bench-results/crowd-physics/<ts>)
set -u -o pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../../.." && pwd)"

: "${GODOT_EXE:?Set GODOT_EXE to the Godot executable and re-run}"

OUT="${BENCH_OUT:-$HOME/bench-results/crowd-physics/$(date +%Y%m%d-%H%M%S)}"
LOGS="$OUT/logs"
mkdir -p "$LOGS"

WORKTREES="$(cd "$REPO_ROOT/.." && pwd)/crowd-physics-worktrees"
mkdir -p "$WORKTREES"

VARIANTS="${VARIANTS:-characterbody rigidbody}"
SUITES="${SUITES:-sanity core blocking load load-rate events realtime}"
THROUGHPUT_SUITES="core blocking load load-rate events"
REPS="${REPS:-3}"
TIMEOUT_SECONDS=7200

OK_COUNT=0
FAIL_COUNT=0

branch_of() {
    case "$1" in
        characterbody) echo "proto/crowd-physics-characterbody" ;;
        rigidbody)     echo "proto/crowd-physics-rigidbody" ;;
        *)             return 1 ;;
    esac
}

contains() {
    case " $1 " in *" $2 "*) return 0 ;; *) return 1 ;; esac
}

# <variant> <suite> <rep> <ccd: yes|no> -> log file name stem
stem_of() {
    local stem="$1-$2-r$3"
    [ "$4" = yes ] && stem="$stem-ccd"
    echo "$stem"
}

# <variant> <suite> <rep> <mode: throughput|realtime> <ccd: yes|no>
run_suite() {
    local variant="$1" suite="$2" rep="$3" mode="$4" ccd="$5"
    local wt="$WORKTREES/$variant"
    local log="$LOGS/$(stem_of "$variant" "$suite" "$rep" "$ccd").log"
    local csv code

    local args=(--headless --path "$wt" --physics-benchmark --bench-suite "$suite"
                --bench-rep "$rep" --bench-out "$OUT")
    [ "$mode" = throughput ] && args+=(--fixed-fps 60 --bench-throughput)
    [ "$ccd" = yes ] && args+=(--bench-ccd)

    timeout "$TIMEOUT_SECONDS" "$GODOT_EXE" "${args[@]}" >"$log" 2>&1
    code=$?
    csv="$(sed -n 's/.*Benchmark CSV written to //p' "$log" | tail -n 1)"

    if [ "$code" -eq 0 ] && [ -n "$csv" ] && [ -f "$csv" ]; then
        echo "OK $csv"
        OK_COUNT=$((OK_COUNT + 1))
    else
        echo "FAIL $log (exit $code)"
        tail -n 20 "$log" | sed 's/^/    /'
        FAIL_COUNT=$((FAIL_COUNT + 1))
    fi
}

# The throughput sanity gate: a run whose first window is slower than 70 TPS is not measuring the
# physics, it is measuring whatever is throttling the loop; stop instead of collecting garbage.
sanity_gate() {
    local variant="$1"
    local log="$LOGS/$(stem_of "$variant" sanity 1 no).log"
    local csv
    csv="$(sed -n 's/.*Benchmark CSV written to //p' "$log" | tail -n 1)"

    if [ -z "$csv" ] || [ ! -f "$csv" ]; then
        echo "sanity gate failed: no CSV from $variant (see $log) — stopping"
        exit 1
    fi
    if awk -F, '$6 == 0 { found = 1 } END { exit found ? 1 : 0 }' "$csv"; then
        echo "sanity gate OK: $csv"
    else
        echo "sanity gate failed: mode_ok = 0 in $csv —"
        echo "throughput mode not effective (was --fixed-fps 60 passed? is the headless frame cap active?) — stopping"
        exit 1
    fi
}

if ! command -v timeout >/dev/null 2>&1; then
    echo "timeout(1) is required (coreutils)" >&2
    exit 1
fi

echo "Results: $OUT"
planned=0
for suite in $SUITES; do
    case "$suite" in
        sanity)    planned=$((planned + $(echo "$VARIANTS" | wc -w))) ;;
        realtime)  planned=$((planned + $(echo "$VARIANTS" | wc -w))) ;;
        load-rate) planned=$((planned + $(echo "$VARIANTS" | wc -w) * REPS)) ;;
        core|blocking|load|events)
            planned=$((planned + $(echo "$VARIANTS" | wc -w) * REPS)) ;;
    esac
done
contains "$SUITES" events && contains "$VARIANTS" rigidbody && planned=$((planned + 1))
echo "Runs planned: $planned"
echo "Rough estimate for the full default run on one machine: 1-1.5 h (assumption — measure from the first run)"

# --- worktrees, build, import: fail fast ---
for variant in $VARIANTS; do
    branch="$(branch_of "$variant")" || { echo "Unknown variant: $variant"; exit 1; }
    wt="$WORKTREES/$variant"
    echo "Worktree: $wt <- $branch (local ref)"
    git -C "$REPO_ROOT" worktree add --detach --force "$wt" "$branch" || exit 1

    # A relative path in the .csproj would need the worktree placed so that .. still resolves;
    # they are siblings of the repo, which is exactly that place. Warn so it can be checked.
    if grep -Eq '\.\.|HintPath' "$wt/NeonWarfare.csproj"; then
        echo "WARNING: $wt/NeonWarfare.csproj references relative paths — check the build log"
    fi

    echo "Building $variant..."
    (cd "$wt" && dotnet build --nologo -v q) || { echo "Build failed in $wt — stopping"; exit 1; }

    echo "Importing resources (headless)..."
    if ! "$GODOT_EXE" --headless --path "$wt" --import >"$LOGS/import-$variant.log" 2>&1; then
        echo "--import failed, falling back to --editor --quit"
        "$GODOT_EXE" --headless --path "$wt" --editor --quit >>"$LOGS/import-$variant.log" 2>&1 \
            || { echo "Import failed in $wt (see $LOGS/import-$variant.log) — stopping"; exit 1; }
    fi
done

# --- env.txt: what these numbers were measured on ---
{
    echo "date: $(date -u '+%Y-%m-%d %H:%M:%S UTC')"
    if command -v lscpu >/dev/null 2>&1; then
        lscpu | grep -E 'Model name|^CPU\(s\):' | sed 's/^/cpu: /'
    fi
    if command -v free >/dev/null 2>&1; then
        free -h | sed -n '1,2p' | sed 's/^/mem: /'
    fi
    [ -r /sys/devices/system/cpu/cpu0/cpufreq/scaling_governor ] \
        && echo "cpu governor: $(cat /sys/devices/system/cpu/cpu0/cpufreq/scaling_governor)"
    for online in /sys/class/power_supply/A*online; do
        [ -e "$online" ] && echo "AC power: $online=$(cat "$online")"
    done
    echo "godot: $("$GODOT_EXE" --version)"
    for variant in $VARIANTS; do
        echo "$variant: $(git -C "$WORKTREES/$variant" rev-parse HEAD)"
    done
} >"$OUT/env.txt"

# --- sanity first: both variants, throughput mode; stop on a failed gate ---
if contains "$SUITES" sanity; then
    for variant in $VARIANTS; do
        run_suite "$variant" sanity 1 throughput no
        sanity_gate "$variant"
    done
fi

# --- the throughput suites: suite first, then repetitions, then variants, so the two variants of
# --- the same suite run back to back (A r1, B r1, A r2, B r2, ...) and thermal drift hits both equally
for suite in $THROUGHPUT_SUITES; do
    contains "$SUITES" "$suite" || continue
    for rep in $(seq 1 "$REPS"); do
        for variant in $VARIANTS; do
            run_suite "$variant" "$suite" "$rep" throughput no
        done
    done
done

# --- realtime: no --fixed-fps, no --bench-throughput, one repetition ---
if contains "$SUITES" realtime; then
    for variant in $VARIANTS; do
        run_suite "$variant" realtime 1 realtime no
    done
fi

# --- events with CCD: variant B only (for A it is a no-op), one repetition ---
if contains "$SUITES" events && contains "$VARIANTS" rigidbody; then
    run_suite rigidbody events 1 throughput yes
fi

# --- summary and cleanup ---
echo "Done: $OK_COUNT ok / $FAIL_COUNT failed. Results in $OUT"

for variant in $VARIANTS; do
    git -C "$REPO_ROOT" worktree remove --force "$WORKTREES/$variant" || true
done
git -C "$REPO_ROOT" worktree prune

[ "$FAIL_COUNT" -eq 0 ] || exit 1
