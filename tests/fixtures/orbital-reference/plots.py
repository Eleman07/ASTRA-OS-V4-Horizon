"""Dependency-free SVG plots from actual numerical samples."""
import html
from pathlib import Path


def chart(path, points, title, xlabel, ylabel, equal_axes=False):
    if len(points) < 2:
        raise ValueError("A plot requires at least two samples")
    width, height, margin = 900, 640, 90
    xs, ys = zip(*points)
    low_x, high_x, low_y, high_y = min(xs), max(xs), min(ys), max(ys)
    if equal_axes:
        span = max(high_x-low_x, high_y-low_y)
        center_x, center_y = (low_x+high_x)/2, (low_y+high_y)/2
        low_x, high_x = center_x-span/2, center_x+span/2
        low_y, high_y = center_y-span/2, center_y+span/2
        height = width
    span_x, span_y = high_x-low_x or 1.0, high_y-low_y or 1.0
    def canvas(x, y):
        return (margin+(x-low_x)/span_x*(width-2*margin), height-margin-(y-low_y)/span_y*(height-2*margin))
    polyline = " ".join("%.4f,%.4f" % canvas(x, y) for x, y in points)
    labels = [f'<text x="{width/2}" y="35" text-anchor="middle">{html.escape(title)}</text>',
              f'<text x="{width/2}" y="{height-20}" text-anchor="middle">{html.escape(xlabel)}</text>',
              f'<text transform="translate(22,{height/2}) rotate(-90)" text-anchor="middle">{html.escape(ylabel)}</text>']
    for index in range(5):
        fraction = index/4
        x, y = margin+fraction*(width-2*margin), height-margin-fraction*(height-2*margin)
        labels.append(f'<text x="{x:.2f}" y="{height-margin+25}" text-anchor="middle">{low_x+fraction*span_x:.5g}</text>')
        labels.append(f'<text x="{margin-12}" y="{y:.2f}" text-anchor="end">{low_y+fraction*span_y:.5g}</text>')
    svg = f'<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {width} {height}" role="img"><title>{html.escape(title)}</title><desc>Grafico ottenuto dai campioni numerici realmente eseguiti; assi e unità espliciti.</desc><rect width="100%" height="100%" fill="#101827"/><g fill="#e4edf8" font-family="sans-serif" font-size="16">' + "".join(labels) + f'</g><path d="M {margin} {margin} V {height-margin} H {width-margin}" fill="none" stroke="#95a9bd"/><polyline points="{polyline}" fill="none" stroke="#64e7d1" stroke-width="2"/></svg>'
    Path(path).write_text(svg, encoding="utf8")


def create_plots(output, rows):
    output = Path(output)
    chart(output/"orbit.svg", [(row[1], row[2]) for row in rows], "Orbita circolare 400 km · RK4 ECI", "X ECI [km]", "Y ECI [km]", True)
    initial_energy = rows[0][7]
    chart(output/"energy.svg", [(row[0], abs((row[7]-initial_energy)/initial_energy)) for row in rows], "Deriva relativa dell'energia specifica · RK4", "Tempo [s]", "|Δε / ε iniziale| [1]")
