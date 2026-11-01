"""ASTRA STEM: export data, not executable instructions, to the local 3D Studio.
Copy this file beside your project. Python standard library only; NumPy arrays work
through the normal iterable protocol. No network or application dependency.
"""
import json
import math
import os
from pathlib import Path
import tempfile

MAX_POINTS = 50_000

class Scene:
    def __init__(self, title="Progetto STEM", axes=("X", "Y", "Z")):
        if len(axes) != 3:
            raise ValueError("Three axis labels required")
        self.title = str(title)
        self.axes = dict(zip(("x", "y", "z"), map(str, axes)))
        self.series = []

    def _add(self, name, kind, points, color=None, **shape):
        remaining = MAX_POINTS - sum(len(s["points"]) for s in self.series)
        data = []
        for point in points:
            if len(data) >= remaining:
                raise ValueError("At most 50,000 points per scene")
            p = list(point)
            if len(p) != 3:
                raise ValueError("Each point must be [x,y,z]")
            p = [float(v) for v in p]
            if not all(math.isfinite(v) and abs(v) <= 1e100 for v in p):
                raise ValueError("Coordinates must be finite, abs <= 1e100")
            data.append(p)
        if not data or kind == "line" and len(data) < 2:
            raise ValueError("Not enough points")
        if len(self.series) >= 24:
            raise ValueError("At most 24 series")
        entry = dict(name=str(name), kind=kind, points=data, **shape)
        if color is not None:
            import re
            if not re.fullmatch(r"#[0-9a-fA-F]{6}", color):
                raise ValueError("Color must be #RRGGBB")
            entry["color"] = color
        self.series.append(entry)
        return self

    def points(self, name, points, color=None):
        return self._add(name, "points", points, color)

    def line(self, name, points, color=None):
        return self._add(name, "line", points, color)

    def surface(self, name, x, y, z, color=None):
        x, y, z = list(x), list(y), [list(row) for row in z]
        if min(len(x), len(y)) < 2 or len(z) != len(y) or any(len(row) != len(x) for row in z):
            raise ValueError("z must be a rectangular grid: len(y) rows, len(x) columns")
        return self._add(name, "surface", ((a,b,z[j][i]) for j,b in enumerate(y) for i,a in enumerate(x)), color, rows=len(y), columns=len(x))

    def to_json(self):
        if not self.series:
            raise ValueError("Add at least one series")
        for label, limit in [(self.title,120), *[(v,80) for v in self.axes.values()], *[(s["name"],80) for s in self.series]]:
            if not label.strip() or len(label) > limit or any(ord(c)<32 for c in label):
                raise ValueError("Labels must be nonempty and within the length limits")
        result = json.dumps(dict(schemaVersion=1, title=self.title, axes=self.axes, series=self.series), ensure_ascii=False, allow_nan=False)
        if len(result.encode("utf-8")) > 8_000_000:
            raise ValueError("Scene exceeds 8 MB")
        return result

    def save(self, path="astra-scene.json"):
        """Atomic save: ASTRA can reload this file while a Python experiment updates it."""
        content = self.to_json()
        path = Path(path).resolve()
        path.parent.mkdir(parents=True, exist_ok=True)
        handle, temporary = tempfile.mkstemp(prefix=".astra-", suffix=".tmp", dir=path.parent)
        try:
            with os.fdopen(handle,"w",encoding="utf-8") as output:
                output.write(content)
            os.replace(temporary,path)
        finally:
            if os.path.exists(temporary):
                os.unlink(temporary)
        return path
