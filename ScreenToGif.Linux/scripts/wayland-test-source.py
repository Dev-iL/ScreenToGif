#!/usr/bin/python3
"""Owned changing-content window for the opt-in native Wayland recording check."""
import os
import sys
import gi

gi.require_version("Gtk", "4.0")
gi.require_version("Gdk", "4.0")
gi.require_version("GdkWayland", "4.0")
from gi.repository import Gdk, GdkWayland, Gio, GLib, Gtk

os.environ["GDK_BACKEND"] = "wayland"


class Source(Gtk.Application):
    def __init__(self):
        super().__init__(application_id="io.screentogif.WaylandTestSource", flags=Gio.ApplicationFlags.NON_UNIQUE)
        self.tick = 0
        self.area = None

    def do_activate(self):
        assert isinstance(Gdk.Display.get_default(), GdkWayland.WaylandDisplay), "Source must be native Wayland"
        window = Gtk.ApplicationWindow(application=self, title="ScreenToGif Wayland test source")
        self.area = Gtk.DrawingArea(content_width=640, content_height=360)
        self.area.set_draw_func(self.draw)
        window.set_child(self.area)
        window.present()
        print("NATIVE_WAYLAND_SOURCE_READY", flush=True)
        GLib.timeout_add(100, self.advance)

    def advance(self):
        self.tick += 1
        self.area.queue_draw()
        return True

    def draw(self, area, context, width, height):
        context.set_source_rgb(0.05, 0.08, 0.12)
        context.paint()
        for column, color in enumerate([(1, 0, 0), (0, 1, 0), (0, 0, 1)]):
            context.set_source_rgb(*color)
            context.rectangle(20 + column * 100, 20, 80, 80)
            context.fill()
        context.set_source_rgb(1, 1, 1)
        context.set_font_size(28)
        context.move_to(20, 160)
        context.show_text(f"Native Wayland frame {self.tick:06d}")
        context.set_source_rgb(1, 0.7, 0)
        context.rectangle(20 + (self.tick * 7) % max(1, width - 80), height - 80, 40, 40)
        context.fill()


if __name__ == "__main__":
    sys.exit(Source().run(sys.argv))
