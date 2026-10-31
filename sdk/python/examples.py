"""Computed mathematical examples, not measurements or validated simulations."""
import argparse
import math
from pathlib import Path
from astra_scene import Scene

def mathematics():
    x = [-6 + i*12/64 for i in range(65)]
    z = [[math.sin(math.hypot(a,b)) for a in x] for b in x]
    return Scene("Matematica · superficie sin(r)",("x","y","sin(sqrt(x²+y²))")).surface("Superficie",x,x,z)

def physics():
    scene = Scene("Fisica · traiettorie ideali",("x (m)","y (m)","z (m)"))
    for degrees in (25,45,65):
        angle=math.radians(degrees); speed=12; end=2*speed*math.sin(angle)/9.81
        scene.line(f"Lancio {degrees}°",((speed*math.cos(angle)*t,0,speed*math.sin(angle)*t-4.905*t*t) for t in (i*end/120 for i in range(121))))
    return scene

def biology():
    scene=Scene("Biologia · doppia elica illustrativa",("x","y","z"))
    for phase,name in ((0,"Filamento A"),(math.pi,"Filamento B")):
        scene.line(name,((math.cos(t+phase),math.sin(t+phase),t/5) for t in (i*8*math.pi/400 for i in range(401))))
    return scene

if __name__ == "__main__":
    parser=argparse.ArgumentParser();parser.add_argument("--output",type=Path,default=Path.cwd());args=parser.parse_args()
    for name,factory in (("mathematics",mathematics),("physics",physics),("biology",biology)):
        print(factory().save(args.output/(name+".json")))
