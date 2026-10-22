#!/usr/bin/env bash
set -euo pipefail

project_root=$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)
evidence=${1:?Usage: check-wayland-recording.sh /tmp/evidence-directory}
mkdir -p "$evidence"
evidence=$(cd "$evidence" && pwd)
[[ ${XDG_SESSION_TYPE:-} == wayland ]] || { echo "A native Wayland desktop is required." >&2; exit 1; }
lock_state=$(gdbus call --session --dest org.gnome.ScreenSaver \
    --object-path /org/gnome/ScreenSaver --method org.gnome.ScreenSaver.GetActive)
[[ $lock_state == '(false,)' ]] || { echo "Unlock the GNOME desktop before this check." >&2; exit 1; }
config=$(mktemp -d /tmp/screentogif-wayland-config-XXXXXX)
trap 'rm -rf "$config"' EXIT

echo 'In the desktop sharing dialog choose only the window: ScreenToGif Wayland test source'
echo "Evidence: $evidence"
cd "$project_root"
XDG_CONFIG_HOME="$config" \
SCREENTOGIF_WAYLAND_RECORDING_SMOKE=1 \
SCREENTOGIF_WAYLAND_SOURCE_SCRIPT="$project_root/ScreenToGif.Linux/scripts/wayland-test-source.py" \
SCREENTOGIF_DESKTOP_SCREENSHOT_DIR="$evidence" \
dotnet test ScreenToGif.Linux.Tests/ScreenToGif.Linux.Tests.csproj --no-restore \
    --filter FullyQualifiedName~Native_Wayland_source_records_into_editor \
    --logger 'trx;LogFileName=wayland.trx' --results-directory "$evidence"
