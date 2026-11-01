"""Record only ASTRA's existing main window, with no audio input or output stream."""
import argparse
import json
from pathlib import Path
import re
import subprocess
import time
from datetime import datetime, timezone

root = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser()
parser.add_argument('name')
parser.add_argument('--seconds', type=float, default=14)
parser.add_argument('--assistant', action='store_true', help='Capture ASTRA\'s own assistant window instead of its main window.')
args = parser.parse_args()
if not re.fullmatch(r'[a-zA-Z0-9_-]{1,80}', args.name) or not 2 <= args.seconds <= 60:
    raise SystemExit('Use a short filename and a duration between 2 and 60 seconds.')
ffmpeg = root / 'downloads/trailer-tools/ffmpeg-9.0.2-essentials_build/bin/ffmpeg.exe'
folder = root / 'dist/trailer/2026-10-28'
clips = folder / '01-clips'
logs = folder / 'logs'
clips.mkdir(parents=True, exist_ok=True)
logs.mkdir(parents=True, exist_ok=True)
output = clips / (args.name + '.mp4')
if output.exists():
    raise SystemExit('A clip with that name already exists; choose a new take name.')
source = ("gfxcapture=window_title='^ASTRA OS V4':"
          "window_exe='(?i)^Astra[.]Desktop[.]exe$':"
          'capture_cursor=0:display_border=0:max_framerate=30:width=-2:height=-2')
if args.assistant:
    source = source.replace("window_title='^ASTRA OS V4'", "window_title='^ASTRA OS .*Assistente$'")
# Mask the per-account name card and the bottom status line in the capture itself.
video = ('hwdownload,format=bgra,drawbox=x=0:y=ih-150:w=218:h=150:color=0x0A1931:t=fill,'
         'drawbox=x=218:y=ih-28:w=iw-218:h=28:color=0x0A1931:t=fill,format=yuv420p')
if args.assistant:
    video = 'hwdownload,format=bgra,format=yuv420p'
sources = folder / '00-source'
sources.mkdir(exist_ok=True)
transport = sources / (args.name + '.ts')
# WGC emits frames when the window changes. A wall-clock deadline and a streaming
# transport container keep static panels from blocking MP4 finalization forever.
command = [str(ffmpeg), '-hide_banner', '-nostdin', '-n', '-f', 'lavfi', '-i', source,
           '-map', '0:v:0', '-an', '-sn', '-dn', '-vf', video, '-fps_mode', 'vfr',
           '-c:v', 'libx264', '-preset', 'ultrafast', '-tune', 'zerolatency', '-bf', '0',
           '-g', '30', '-crf', '17', '-threads', '6', '-flush_packets', '1', '-f', 'mpegts', str(transport)]
record = {'startedUtc': datetime.now(timezone.utc).isoformat(), 'target': 'ASTRA OS · Assistente' if args.assistant else 'ASTRA OS V4 · Horizon',
          'audioInputs': [], 'audioEnabled': False, 'requestedSeconds': args.seconds,
          'file': output.name, 'captureApi': 'Windows.Graphics.Capture via FFmpeg gfxcapture',
          'command': command}
print(f'Recording ASTRA only: {output.name}, {args.seconds:g} seconds, video only.', flush=True)
with (logs / (args.name + '.log')).open('w', encoding='utf8') as log:
    process = subprocess.Popen(command, stdin=subprocess.DEVNULL, stdout=log, stderr=log,
                               creationflags=getattr(subprocess, 'CREATE_NO_WINDOW', 0))
    started = time.monotonic()
    try:
        process.wait(timeout=args.seconds + 1)
        if process.returncode:
            raise RuntimeError(log.name + ': recorder stopped early')
    except subprocess.TimeoutExpired:
        process.terminate()
        process.wait(timeout=15)
    record['wallClockCaptureSeconds'] = time.monotonic() - started
    if not transport.exists() or transport.stat().st_size < 1000:
        raise RuntimeError('No valid window frames were recorded; inspect ' + log.name)
    # Hold the last actual captured frame when Windows emits no new static frame.
    finish = [str(ffmpeg), '-hide_banner', '-nostdin', '-n', '-i', str(transport),
              '-map', '0:v:0', '-an', '-sn', '-dn', '-vf',
              f'setpts=PTS-STARTPTS,fps=30,tpad=stop_mode=clone:stop_duration={args.seconds+2},trim=duration={args.seconds},format=yuv420p',
              '-c:v', 'libx264', '-preset', 'veryfast', '-crf', '18', '-threads', '6',
              '-movflags', '+faststart', str(output)]
    result = subprocess.run(finish, stdin=subprocess.DEVNULL, stdout=log, stderr=log,
                            creationflags=getattr(subprocess, 'CREATE_NO_WINDOW', 0), timeout=90)
record['exitCode'] = result.returncode
(logs / (args.name + '.json')).write_text(json.dumps(record, indent=2), encoding='utf8')
if result.returncode:
    print((logs / (args.name + '.log')).read_text(encoding='utf8')[-6500:])
    raise SystemExit(result.returncode)
print(f'Saved {output.name}: {output.stat().st_size:,} bytes. No audio source was opened.', flush=True)
