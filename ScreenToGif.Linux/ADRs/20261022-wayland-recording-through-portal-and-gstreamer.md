# ADR: Wayland screen capture through the portal and GStreamer

## Status
Accepted

## Area
Recorder

## Context

Ubuntu 26.04 runs a Wayland desktop while Avalonia's current Linux UI uses XWayland. An XID identifies the app window; it does not authorize reading native Wayland content. The X11 capture decision deliberately deferred a native capture backend. The existing recording session already owns scheduling, pause semantics, resource limits and frame/workspace handoff to Editor.

## Decision

Use the desktop ScreenCast portal to select one monitor or window with explicit consent. Negotiate hidden or embedded cursor mode according to the recording's existing Show cursor setting. Each recording uses a dedicated managed D-Bus connection and no persistence/restore token. Subscribe before making request calls, using unpredictable request/session tokens. Disconnecting that dedicated connection closes the client's portal sessions, including creation completing during cancellation; it never disconnects Avalonia's shared portal client.

Consume the authorized PipeWire remote in process through GStreamer's `pipewiresrc` and a core sink accepting raw BGRA/BGRx/RGBA/RGBx buffers. The plugin duplicates the supplied remote FD; the portal session retains the original until the consumer closes. Prefer a portal-provided PipeWire object serial when available; older portals provide a node ID. Do not connect to an unrestricted PipeWire server or read the XWayland root.

The Recorder captures the entire chosen source, in its negotiated pixel size. Its desktop position and logical size have no capture meaning. Disable editable geometry and explain the selection in the existing Recorder surface. A source changing pixel size ends capture with the existing recoverable failure state instead of changing an active project's geometry. Wayland sizes do not overwrite remembered X11 frame dimensions.

Keep the existing recording engine. Read buffer timestamps in the stream's running-time domain and schedule with that same pipeline clock; pause exclusion therefore uses the same time base. Preserve each mode's existing playback-delay rules. A small PipeWire keepalive period supplies timestamped stationary frames; it does not recreate a closed stream. An ordered sink-pad observer retains one immutable native buffer with its segment, crop and orientation snapshot. Recording copies only the requested latest frame into the existing pooled opaque BGRA contract, honoring metadata stride/offset, crop bounds and all eight orientation transforms. Native frame waits are bounded. Portal closure or stream error is a recording failure: pause, report once and retain frames for Stop. Starting a new recording obtains fresh consent.

## Alternatives considered

- GStreamer conversion filters before ingestion: controlled pixel tests showed rotation was ignored without an orientation filter, while conversion/rotation filters discarded crop metadata. Observing raw RGB and ordered metadata together preserves the selected visible region; unsupported formats fail explicitly.
- Direct PipeWire C interop: would duplicate format negotiation, native loop and buffer handling already provided by GStreamer.
- FFmpeg PipeWire input: the installed FFmpeg has no such input. Per-frame external capture commands also break Manual responsiveness and complicate timing/cleanup.
- A separate Wayland recording engine: duplicates established capture limits, settings, pause and Editor ownership rules.
- Overlay cropping from desktop coordinates: compositor selection metadata is not a reliable mapping from this XWayland window's logical coordinates to stream pixels, particularly under fractional scaling. Full-source selection states the supported behavior honestly.

## Consequences

Wayland recording additionally needs the desktop's ScreenCast portal/PipeWire services and GStreamer runtime libraries/plugins. Missing dependencies and unsupported cursor modes produce actionable errors; X11 remains independent of this backend. Desktop-wide shortcuts, cursor following, arbitrary region cropping and native Wayland UI rendering remain separate work.

The live verification scope is recorded with the feature's Ticket and evidence. Controlled native/D-Bus tests establish protocol, pixels, timestamp and cancellation behavior; they cannot prove native desktop capture or support on untested compositors.

## Sources

- [ScreenCast portal](https://flatpak.github.io/xdg-desktop-portal/docs/doc-org.freedesktop.portal.ScreenCast.html)
- [Session lifetime](https://flatpak.github.io/xdg-desktop-portal/docs/doc-org.freedesktop.portal.Session.html)
- [PipeWire GStreamer remote FD ownership](https://github.com/PipeWire/pipewire/blob/master/src/gst/gstpipewirecore.c)
- [GStreamer running-time synchronization](https://gstreamer.freedesktop.org/documentation/additional/design/synchronisation.html)
- [Stage 2 Ticket](../../tickets/linux-wayland/01-wayland-screen-recording.md)
