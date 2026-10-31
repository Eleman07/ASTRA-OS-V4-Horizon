"""Compatibility entry point: download the default open model (Ministral)."""
import subprocess,sys
from pathlib import Path
subprocess.run([sys.executable,str(Path(__file__).with_name('fetch-local-model.py')),'ministral'],check=True)
