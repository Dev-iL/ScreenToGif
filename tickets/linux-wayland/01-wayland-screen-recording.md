Kind: shaped
Status: open
Depends on: none
Claimed by: codex
Auto: yes — granted by the user, explicitly in this conversation on 2026-10-22
Type: feature

# Record the Wayland desktop through the screen-sharing portal

## Why

ScreenToGif's Linux application was developed on Ubuntu 24.04 with X11. Its user now runs Ubuntu 26.04 with Wayland and needs to record the desktop into the existing editor. The current capture path reads X11 pixels. An X11 window handle under XWayland does not establish access to native Wayland windows, so the current support check can offer recording that cannot capture the complete desktop.

The outcome is a working Wayland screen-recording workflow: choose a capture source, record it, pause or stop, edit the result, and export it.

## Scope

Add Wayland screen capture through the desktop ScreenCast portal and PipeWire, integrated with the existing Recorder, recording session, project ownership, and editor handoff. Support the existing recording controls where the portal permits their behavior. Preserve X11 recording.

The user wants display/layout compatibility and consistent return from Editor to StartUp addressed first. This is preferred execution order, not a technical dependency. This ticket covers the subsequent Wayland recording work; it does not include another general UI redesign, a native Wayland UI-backend migration, new editing features, global desktop shortcuts, or unrelated deferred recorder features.

## Rules that must hold

- Use the desktop's consent flow. Permission denial, cancellation, and stream termination must produce a clear state without crashing or losing already recorded frames.
- Detect actual capture capability. An X11 window handle alone must not classify a Wayland desktop as fully recordable.
- Keep captured pixels, cursor placement, region mapping, scaling, and frame timing consistent with what the user chose. Expose portal-imposed selection limits honestly instead of promising unsupported window positioning or capture regions.
- Preserve existing pause/resume, discard confirmation, frame/resource limits, asynchronous recording behavior, settings ownership, and stop-to-editor behavior.
- Keep unsaved editor work safe when adopting recordings or closing windows. Explicit application Exit must still exit, and successful Editor close must follow the application’s established return-to-StartUp behavior.
- Keep existing X11 recording and media import/edit/export workflows working.
- Rebase when integration is needed; never merge.
- Report final behavior, validation evidence, and limits, without action-history bookkeeping.

## Watch out for

The current UI may run through XWayland even when the desktop session is Wayland. UI rendering and desktop capture are separate capabilities. Reading the XWayland root cannot prove capture of native Wayland content.

Portal source selection is an asynchronous user interaction. Test denial, cancellation, compositor/portal failure, and source disappearance as well as the happy path. Unattended execution is authorized by the user. Portal consent may still need a person on the live desktop. If consent or desktop access requires that person, preserve the work and report the blocker; the grant does not bypass the desktop consent flow.

Use the stream's real pixel dimensions and timestamps. Fractional scaling, cursor metadata/composition, and monitor geometry can differ from the application's logical coordinates. A working Xvfb test is not evidence of native Wayland capture.

## Suggested approach

Keep the existing recording-session and editor handoff contracts. Select an appropriate capture source behind the existing capture boundary; integrate portal session lifetime and PipeWire stream lifetime with cancellation and cleanup. Prefer existing supported platform libraries over ad hoc capture commands. Treat source selection and capture-region behavior as portal-constrained interactions, while keeping the user's existing workflow recognizable.

## Definition of done

- On Ubuntu 26.04 Wayland, a real Recorder instance requests a source through the desktop portal and records changing content from a native Wayland application. Recorded frames must show the selected content, rather than a blank image or only XWayland windows.
- The user can record, pause, resume, stop, and discard using the existing supported controls. Stopping hands the recording to Editor with correct dimensions, frame order, and measured delays. The user can play, edit, and export it; an independent decoder confirms the exported content and timing match the recorded project within the chosen format's documented precision.
- Tests and retained live evidence cover consent acceptance, denial and cancellation; source disappearance and stream failure; restarting after a canceled or completed session; and cleanup after closing Recorder or exiting the application. Failure preserves captured work and leaves a usable recovery path, with no test-owned portal sessions, streams, or subprocesses left running.
- Demonstrate correct selected-region/source mapping and cursor behavior at the desktop's current scale and an available fractional scale. If a configuration cannot be exercised, identify the missing evidence and do not claim it is supported based only on source inspection.
- Verify X11 recording in an actual X11 test session and rerun the existing Linux build and tests with no failures. Add meaningful regression tests for capability detection and portal/stream lifecycle, using isolated configuration and media. Required native Wayland behavior cannot pass through mocks or skipped tests alone.
- Update user-facing requirements, supported controls, consent behavior, known portal limitations, and the existing recorder follow-up documentation so they describe the shipped behavior accurately.

<!-- manifest-dev-run-ticket-attempt -->
## Execution attempt

Ticket: tickets/linux-wayland/01-wayland-screen-recording.md
Branch: ticket/linux-wayland-01-wayland-screen-recording
Target: origin/linux-support
Recovery purpose: preserve the portal/PipeWire implementation and verification on one stable branch.

Run ID: 20261022T152112Z-ebb56609
Invocation: direct user request; unattended work remains authorized by the Ticket's existing Auto grant.
Writer: codex, acting in the user-invoked local session.
Session: /root; the host exposes no separate resumable runner handle.
Started: 2026-10-22T15:21:12.509077+00:00
Launch count: 1
Base commit: c2a4a729f69ab67471d722e192317ca9228436d9
Ready to land: no
Manifest: not yet defined

Progress: understand
The shaped Ticket is the scope. The existing capture/session/editor ownership contracts, available portal/PipeWire libraries and test coverage are under inspection. Stage 1 is complete at the target base. Implementation, native consent/source verification, X11 regression checks and a ready pull request remain required. No stage-2 completion gate has passed yet. Integration uses rebase in accordance with the user's project rule.
