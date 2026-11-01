"""Build the ASTRA Horizon 4K trailer from genuine app recordings."""
from __future__ import annotations

import argparse
import json
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
TOOLS = ROOT / "downloads/trailer-tools/ffmpeg-9.0.2-essentials_build/bin"
FFMPEG = TOOLS / "ffmpeg.exe"
FFPROBE = TOOLS / "ffprobe.exe"
SOURCE = ROOT / "dist/trailer/2026-10-28/01-clips"
WORK = ROOT / "dist/trailer/2026-10-30"
FONTS = ROOT / "design/trailer-horizon/assets/fonts"
LOGO = Path("src/Astra.Desktop/Assets/astra-logo.png")
XFADE = 0.6

SCENES = [
    {"id": "home", "file": "01-home", "start": 0.0, "duration": 8.4,
     "title": "A NEW HORIZON.", "subtitle": "One workspace. Room to think.",
     "section": "A SPACE TO BEGIN"},
    {"id": "orbit", "file": "02-orbital-take2", "start": 3.4, "duration": 8.5,
     "title": "YOUR DESKTOP. IN ORBIT.", "subtitle": "Explore your files in a new dimension.",
     "section": "DESKTOP / ORBITAL 3D"},
    {"id": "ariane", "file": "03-ariane64", "start": 6.0, "duration": 8.5,
     "title": "COMPLEXITY, MADE VISIBLE.", "subtitle": "Explore the Ariane 64 model in 3D.",
     "section": "STEM / 3D MODEL"},
    {"id": "simulation", "file": "04-simulazione-orbitale", "start": 2.0, "duration": 8.5,
     "title": "SEE THE MOTION.", "subtitle": "Simulate. Trace. Understand.",
     "section": "STEM / SIMULATION"},
    {"id": "assistant", "file": "05-assistente-multimodale", "start": 0.0, "duration": 7.5,
     "title": "BRING YOUR CONTEXT.", "subtitle": "Documents. Images. Data.",
     "section": "ASSISTANT / MULTIMODAL"},
    {"id": "widgets", "file": "06-widget-automazioni", "start": 0.2, "duration": 7.4,
     "title": "MAKE SPACE FOR FOCUS.", "subtitle": "Your widgets. Your rhythm. Your flow.",
     "section": "PERSONAL WORKSPACE"},
    {"id": "gaming", "file": "07-gaming", "start": 0.0, "duration": 7.4,
     "title": "KNOW YOUR SESSION.", "subtitle": "CPU. GPU. Memory. In context.",
     "section": "GAMING SESSION LAB"},
    {"id": "creator", "file": "08-creator-palette", "start": 0.0, "duration": 7.4,
     "title": "FROM ASSET TO STORY.", "subtitle": "Colors. Metadata. Storyboards.",
     "section": "CREATOR STUDIO"},
    {"id": "plugins", "file": "09-plugin-catalogo", "start": 0.0, "duration": 7.4,
     "title": "EXTEND YOUR WORKSPACE.", "subtitle": "A flexible catalog and plugin SDK.",
     "section": "PLUGINS / SDK"},
    {"id": "theme", "file": "11-personalizzazione", "start": 0.0, "duration": 7.4,
     "title": "MAKE IT YOURS.", "subtitle": "Personalize the details.",
     "section": "YOUR THEME"},
]

INTRO = {"id": "intro", "file": "01-home", "start": 0.0, "duration": 5.0}
OUTRO = {"id": "credits", "file": "01-home", "start": 3.0, "duration": 7.0}
TRANSITIONS = ["fade", "smoothleft", "fade", "wipeleft", "fadeblack",
               "fade", "smoothup", "fade", "wipeleft", "fadeblack", "fade"]


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--music", required=True, type=Path,
                        help="User-supplied, authorized Counting Stars audio file.")
    parser.add_argument("--output", type=Path,
                        default=WORK / "ASTRA-HORIZON-REAL-DEMO-TRAILER-4K-30FPS.mp4")
    parser.add_argument("--encoder", choices=["h264_nvenc", "libx264"], default="h264_nvenc")
    parser.add_argument("--force", action="store_true", help="Replace the selected master output.")
    parser.add_argument("--refresh-cards", action="store_true", help="Re-render the intro and end cards.")
    parser.add_argument("--rerender-scenes", action="store_true", help="Re-render all captured demo scenes.")
    return parser.parse_args()


def ffprobe(path: Path, *, count_frames: bool = False) -> dict:
    command = [str(FFPROBE), "-v", "error"]
    if count_frames:
        command.append("-count_frames")
    command += ["-show_streams", "-show_format", "-of", "json", str(path)]
    result = subprocess.run(command, cwd=ROOT, capture_output=True, text=True, check=True,
                            creationflags=subprocess.CREATE_NO_WINDOW)
    return json.loads(result.stdout)


def run_ffmpeg(command: list[str], label: str, log_path: Path) -> None:
    command = [str(FFMPEG), "-hide_banner", "-nostdin", "-y", "-loglevel", "warning",
               "-progress", "pipe:1", "-stats_period", "8", *command]
    print(label, flush=True)
    with log_path.open("w", encoding="utf-8") as log:
        process = subprocess.Popen(command, cwd=ROOT, stdin=subprocess.DEVNULL,
                                   stdout=subprocess.PIPE, stderr=log, text=True, bufsize=1,
                                   creationflags=subprocess.CREATE_NO_WINDOW)
        assert process.stdout is not None
        for line in process.stdout:
            if line.startswith("out_time="):
                print(f"  {label}: {line.strip()[9:]}", flush=True)
        code = process.wait()
    if code:
        tail = log_path.read_text(encoding="utf-8", errors="replace")[-5000:]
        raise RuntimeError(f"FFmpeg failed for {label} (exit {code}):\n{tail}")


def relative(path: Path) -> str:
    absolute = path if path.is_absolute() else ROOT / path
    return absolute.resolve().relative_to(ROOT).as_posix()


def write_text(name: str, value: str) -> str:
    path = WORK / "titles" / f"{name}.txt"
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(value, encoding="utf-8")
    return relative(path)


def ff_expr(value: str) -> str:
    return value.replace(",", r"\,")


def drawtext(text_path: str, font: str, size: int, color: str, x: str, y: int,
             alpha: str = "1") -> str:
    return (f"drawtext=fontfile={font}:textfile={text_path}:expansion=none:"
            f"fontsize={size}:fontcolor={color}:x='{ff_expr(x)}':y={y}:"
            f"alpha='{ff_expr(alpha)}':shadowcolor=0x000000@0.82:shadowx=3:shadowy=4:"
            f"borderw=0:fix_bounds=true:line_spacing=12")


def visual_base(duration: float, *, portrait: bool = False) -> list[str]:
    if portrait:
        return [
            f"[0:v]trim=duration={duration:.3f},setpts=PTS-STARTPTS,fps=30,split=2[blur0][sharp0]",
            "[blur0]scale=1920:1080:force_original_aspect_ratio=increase,crop=1920:1080,"
            "boxblur=26:8,eq=brightness=-0.58:saturation=0.56,scale=3840:2160,"
            "vignette=PI/3,setsar=1[bg]",
            "[sharp0]scale=1536:1980:force_original_aspect_ratio=decrease,setsar=1,"
            "zoompan=z='min(1.025,1+0.00011*on)':x='iw/2-(iw/zoom/2)':"
            "y='ih/2-(ih/zoom/2)':d=1:s=1536x1980:fps=30,"
            "eq=contrast=1.04:saturation=1.02:brightness=-0.012,"
            "unsharp=5:5:0.35:5:5:0,format=rgba[fg]",
            "[bg][fg]overlay=x=2180:y=(main_h-overlay_h)/2:shortest=1,"
            "drawbox=x=2177:y=96:w=1542:h=1988:color=0xB3CFE5@0.45:t=2[base]",
        ]
    return [
        f"[0:v]trim=duration={duration:.3f},setpts=PTS-STARTPTS,fps=30,split=2[blur0][sharp0]",
        "[blur0]scale=1920:1080:force_original_aspect_ratio=increase,crop=1920:1080,"
        "boxblur=26:8,eq=brightness=-0.58:saturation=0.56,scale=3840:2160,"
        "vignette=PI/3,setsar=1[bg]",
        "[sharp0]scale=3624:2160:force_original_aspect_ratio=decrease,setsar=1,"
        "zoompan=z='min(1.035,1+0.00014*on)':x='iw/2-(iw/zoom/2)':"
        "y='ih/2-(ih/zoom/2)':d=1:s=3624x2160:fps=30,"
        "eq=contrast=1.045:saturation=1.03:brightness=-0.012,"
        "unsharp=5:5:0.35:5:5:0,format=rgba[fg]",
        "[bg][fg]overlay=x=(main_w-overlay_w)/2:y=(main_h-overlay_h)/2:shortest=1,"
        "drawbox=x=106:y=0:w=3628:h=2160:color=0xB3CFE5@0.26:t=2[base]",
    ]


def create_scene_segment(scene: dict, index: int, total: int, *, special: str | None = None) -> Path:
    out_dir = WORK / "segments"
    log_dir = WORK / "logs"
    out_dir.mkdir(parents=True, exist_ok=True)
    log_dir.mkdir(parents=True, exist_ok=True)
    output = out_dir / f"{index:02d}-{scene['id']}.mp4"
    if output.exists() and not args.rerender_scenes:
        print(f"Reusing real demo {index}/{total}: {scene['id']}", flush=True)
        return output
    output.unlink(missing_ok=True)
    source = SOURCE / f"{scene['file']}.mp4"
    meta = ffprobe(source)
    video = next(stream for stream in meta["streams"] if stream["codec_type"] == "video")
    if video["avg_frame_rate"] != "30/1":
        raise ValueError(f"Expected a 30fps real demo source: {source.name}")
    if any(stream["codec_type"] == "audio" for stream in meta["streams"]):
        raise ValueError(f"Unexpected source audio in real app capture: {source.name}")
    if scene["start"] + scene["duration"] > float(meta["format"]["duration"]):
        raise ValueError(f"Trim exceeds source duration: {source.name}")

    graph = visual_base(scene["duration"], portrait=(scene["id"] == "assistant"))
    if scene["id"] == "assistant":
        title_plate = "drawbox=x=0:y=1210:w=2050:h=950:color=0x050A12@0.48:t=fill"
    else:
        title_plate = "drawbox=x=0:y=1470:w=3840:h=690:color=0x050A12@0.47:t=fill"
    graph.append("[base]vignette=PI/5,noise=alls=2:allf=t,"
                 "drawbox=x=0:y=0:w=3840:h=195:color=0x050A12@0.58:t=fill,"
                 f"{title_plate}[frame0]")
    brand = write_text(f"{scene['id']}-brand", "ASTRA OS V4  /  HORIZON")
    section = write_text(f"{scene['id']}-section", f"{index:02d}  /  {scene['section']}")
    title = write_text(f"{scene['id']}-title", scene["title"])
    subtitle = write_text(f"{scene['id']}-subtitle", scene["subtitle"])
    mono = relative(FONTS / "IBMPlexMono-Regular.ttf")
    display = relative(FONTS / "SpaceGrotesk-Variable.ttf")
    x = "170" if scene["id"] != "assistant" else "160"
    y_title = 1635 if scene["id"] != "assistant" else 1390
    y_sub = 1750 if scene["id"] != "assistant" else 1505
    title_alpha = "if(lt(t,0.42),t/0.42,1)"
    sub_alpha = "if(lt(t,0.48),0,if(lt(t,0.92),(t-0.48)/0.44,1))"
    brand_alpha = "if(lt(t,0.35),t/0.35,1)"
    graph += [
        f"[frame0]{drawtext(brand, mono, 31, '0xB3CFE5', '170', 89, brand_alpha)}[f1]",
        f"[f1]{drawtext(section, mono, 25, '0xB3CFE5', '3840-text_w-170', 90, brand_alpha)}[f2]",
        f"[f2]drawbox=x={x}:y=1800:w='{ff_expr('min(620,620*t/0.65)')}':h=7:color=0x75B8E8@0.96:t=fill[f3]",
        f"[f3]{drawtext(title, display, 116, '0xF6FAFD', f'{x}+70*(1-min(t/0.62,1))', y_title, title_alpha)}[f4]",
        f"[f4]{drawtext(subtitle, relative(FONTS / 'Inter-Variable.ttf'), 48, '0xDCE9F5', x, y_sub, sub_alpha)}"
        ",fade=t=in:st=0:d=0.24,format=yuv420p[out]",
    ]
    filter_file = WORK / "filters" / f"{index:02d}-{scene['id']}.ffgraph"
    filter_file.parent.mkdir(parents=True, exist_ok=True)
    filter_file.write_text(";\n".join(graph), encoding="utf-8")
    command = ["-ss", f"{scene['start']:.3f}", "-i", str(source),
               "-filter_complex_threads", "4", "-filter_complex", filter_file.read_text(encoding="utf-8"), "-map", "[out]",
               "-t", f"{scene['duration']:.3f}", "-an", "-sn", "-dn",
               "-c:v", args.encoder]
    if args.encoder == "h264_nvenc":
        command += ["-preset", "p5", "-tune", "hq", "-rc", "vbr", "-cq", "16", "-b:v", "0",
                    "-maxrate", "55M", "-bufsize", "110M", "-spatial-aq", "1", "-temporal-aq", "1"]
    else:
        command += ["-preset", "fast", "-crf", "17"]
    command += ["-pix_fmt", "yuv420p", "-profile:v", "high", "-r", "30", "-fps_mode", "cfr",
                "-color_primaries", "bt709", "-color_trc", "bt709", "-colorspace", "bt709",
                "-movflags", "+faststart", str(output)]
    run_ffmpeg(command, f"Rendering real demo {index}/{total}: {scene['id']}", log_dir / f"{index:02d}-{scene['id']}.log")
    return output


def create_title_card(scene: dict, index: int, *, intro: bool) -> Path:
    out_dir = WORK / "segments"
    log_dir = WORK / "logs"
    out_dir.mkdir(parents=True, exist_ok=True)
    log_dir.mkdir(parents=True, exist_ok=True)
    output = out_dir / f"{index:02d}-{scene['id']}.mp4"
    if output.exists() and not args.refresh_cards:
        print(f"Reusing existing {scene['id']} card", flush=True)
        return output
    output.unlink(missing_ok=True)
    source = SOURCE / f"{scene['file']}.mp4"
    logo_path = ROOT / LOGO
    if intro:
        title_main, title_sub = "A NEW HORIZON.", "FOCUS. CREATE. EXPLORE."
        y_main, y_sub = 1065, 1285
        title_start, sub_start = 0.35, 0.65
    else:
        title_main, title_sub = "ASTRA OS V4", "FOCUS. CREATE. EXPLORE."
        y_main, y_sub = 1055, 1280
        title_start, sub_start = 0.35, 0.60
    label = write_text(f"{scene['id']}-brand", "ASTRA OS V4  /  HORIZON")
    headline = write_text(f"{scene['id']}-headline", title_main)
    strap = write_text(f"{scene['id']}-strap", title_sub)
    if intro:
        credit_filters = ""
    else:
        portfolio = write_text("credits-portfolio", "elemanprojects.com")
        credit_label = write_text("credits-music-label", "MUSIC CREDITS")
        credit_line = write_text("credits-music-line", '"COUNTING STARS"  -  ONEREPUBLIC')
        credit_filters = (
            f";[card4]{drawtext(portfolio, relative(FONTS / 'Inter-Variable.ttf'), 44, '0xB3CFE5', '1360', 1610, 'if(lt(t,1.65),0,if(lt(t,2.1),(t-1.65)/0.45,1))')}[card5]"
            f";[card5]{drawtext(credit_label, relative(FONTS / 'IBMPlexMono-Regular.ttf'), 25, '0x8AAAC3', '1360', 1770, 'if(lt(t,2.15),0,if(lt(t,2.7),(t-2.15)/0.55,1))')}[card6]"
            f";[card6]{drawtext(credit_line, relative(FONTS / 'Inter-Variable.ttf'), 42, '0xDCE9F5', '1360', 1840, 'if(lt(t,2.25),0,if(lt(t,2.8),(t-2.25)/0.55,1))')}[card7]"
        )
    graph = [
        f"[0:v]trim=duration={scene['duration']:.3f},setpts=PTS-STARTPTS,fps=30,"
        "scale=1920:1080:force_original_aspect_ratio=increase,crop=1920:1080,"
        "boxblur=28:9,eq=brightness=-0.74:saturation=0.50,scale=3840:2160,"
        "vignette=PI/2,setsar=1[bg]",
        "[1:v]scale=500:500,format=rgba[logo]",
        "[bg][logo]overlay=x=700:y=840:shortest=1[card0]",
        f"[card0]drawbox=x=1320:y=900:w=4:h=610:color=0x4A7FA7@0.9:t=fill[card1]",
        f"[card1]{drawtext(label, relative(FONTS / 'IBMPlexMono-Regular.ttf'), 28, '0xB3CFE5', '1370', 920, '1')}[card2]",
        f"[card2]{drawtext(headline, relative(FONTS / 'SpaceGrotesk-Variable.ttf'), 118, '0xF6FAFD', '1360', y_main, f'if(lt(t,{title_start}),0,if(lt(t,{title_start+0.5}),(t-{title_start})/0.5,1))')}[card3]",
        f"[card3]{drawtext(strap, relative(FONTS / 'Inter-Variable.ttf'), 54, '0xDCE9F5', '1360', y_sub, f'if(lt(t,{sub_start}),0,if(lt(t,{sub_start+0.5}),(t-{sub_start})/0.5,1))')}[card4]"
        + credit_filters,
    ]
    terminal = "[card7]" if not intro else "[card4]"
    graph.append(f"{terminal}fade=t=in:st=0:d=0.28,format=yuv420p[out]")
    filter_file = WORK / "filters" / f"{index:02d}-{scene['id']}.ffgraph"
    filter_file.parent.mkdir(parents=True, exist_ok=True)
    filter_file.write_text(";\n".join(graph), encoding="utf-8")
    command = ["-ss", f"{scene['start']:.3f}", "-i", str(source), "-loop", "1", "-framerate", "30",
               "-i", str(logo_path), "-filter_complex_threads", "4", "-filter_complex", filter_file.read_text(encoding="utf-8"), "-map", "[out]",
               "-t", f"{scene['duration']:.3f}", "-an", "-sn", "-dn", "-c:v", args.encoder]
    if args.encoder == "h264_nvenc":
        command += ["-preset", "p5", "-tune", "hq", "-rc", "vbr", "-cq", "16", "-b:v", "0",
                    "-maxrate", "55M", "-bufsize", "110M", "-spatial-aq", "1", "-temporal-aq", "1"]
    else:
        command += ["-preset", "fast", "-crf", "17"]
    command += ["-pix_fmt", "yuv420p", "-profile:v", "high", "-r", "30", "-fps_mode", "cfr",
                "-color_primaries", "bt709", "-color_trc", "bt709", "-colorspace", "bt709",
                "-movflags", "+faststart", str(output)]
    run_ffmpeg(command, f"Rendering {('opening' if intro else 'end credits')} card", log_dir / f"{index:02d}-{scene['id']}.log")
    return output


def assemble(segments: list[Path], music: Path, output: Path) -> dict:
    log_dir = WORK / "logs"
    log_dir.mkdir(parents=True, exist_ok=True)
    durations = [float(ffprobe(path)["format"]["duration"]) for path in segments]
    final_duration = sum(durations) - XFADE * (len(durations) - 1)
    if final_duration > 90:
        raise ValueError(f"Edit exceeds the 90-second limit: {final_duration:.3f}s")
    command: list[str] = []
    for path in segments:
        command += ["-i", str(path)]
    command += ["-i", str(music)]
    graph = [f"[{i}:v]settb=1/30,setpts=PTS-STARTPTS,fps=30,format=yuv420p[v{i}]"
             for i in range(len(segments))]
    running = durations[0]
    previous = "v0"
    for i, transition in enumerate(TRANSITIONS, start=1):
        out_label = f"x{i}"
        offset = running - XFADE
        graph.append(f"[{previous}][v{i}]xfade=transition={transition}:duration={XFADE:.3f}:offset={offset:.6f}[{out_label}]")
        previous = out_label
        running += durations[i] - XFADE
    music_index = len(segments)
    fade_start = final_duration - 3.0
    graph.append(f"[{music_index}:a]atrim=start=0:duration={final_duration:.6f},asetpts=PTS-STARTPTS,"
                 f"afade=t=out:st={fade_start:.6f}:d=3.0,loudnorm=I=-16:TP=-1.5:LRA=11,"
                 "alimiter=limit=0.95[aout]")
    filter_file = WORK / "filters" / "master.ffgraph"
    filter_file.write_text(";\n".join(graph), encoding="utf-8")
    command += ["-filter_complex_threads", "6", "-filter_complex", filter_file.read_text(encoding="utf-8"), "-map", f"[{previous}]", "-map", "[aout]",
                "-t", f"{final_duration:.6f}", "-c:v", args.encoder]
    if args.encoder == "h264_nvenc":
        command += ["-preset", "p5", "-tune", "hq", "-rc", "vbr", "-cq", "18", "-b:v", "0",
                    "-maxrate", "55M", "-bufsize", "110M", "-spatial-aq", "1", "-temporal-aq", "1"]
    else:
        command += ["-preset", "slow", "-crf", "18"]
    command += ["-pix_fmt", "yuv420p", "-profile:v", "high", "-r", "30", "-fps_mode", "cfr",
                "-g", "60", "-color_primaries", "bt709", "-color_trc", "bt709", "-colorspace", "bt709",
                "-c:a", "aac", "-b:a", "256k", "-ar", "48000", "-ac", "2",
                "-metadata", "title=ASTRA OS V4 Horizon — Real Demo Trailer",
                "-metadata", "artist=ASTRA OS V4",
                "-metadata", "comment=Music: Counting Stars — OneRepublic. Portfolio: elemanprojects.com. User-supplied audio.",
                "-metadata:s:a:0", "title=Counting Stars — OneRepublic",
                "-metadata:s:a:0", "language=eng", "-movflags", "+faststart", str(output)]
    run_ffmpeg(command, "Assembling 4K/30 master with Counting Stars", log_dir / "master.log")
    return {"durationSeconds": round(final_duration, 3), "framesExpected": round(final_duration * 30),
            "audioFadeOutStartsAt": round(fade_start, 3), "transitionSeconds": XFADE}


def verify(path: Path, expected: dict) -> dict:
    data = ffprobe(path, count_frames=True)
    video = next(stream for stream in data["streams"] if stream["codec_type"] == "video")
    audio = [stream for stream in data["streams"] if stream["codec_type"] == "audio"]
    frames = int(video.get("nb_read_frames", 0))
    duration = float(data["format"]["duration"])
    checks = {
        "width3840": video["width"] == 3840,
        "height2160": video["height"] == 2160,
        "fps30": video["avg_frame_rate"] == "30/1",
        "h264": video["codec_name"] == "h264",
        "within90Seconds": duration <= 90.0,
        "oneAudioTrack": len(audio) == 1,
        "aacStereo": len(audio) == 1 and audio[0]["codec_name"] == "aac" and audio[0].get("channels") == 2,
        "decodedFrames": frames > 0 and abs(frames - expected["framesExpected"]) <= 1,
    }
    report = {
        "file": path.name, "width": video["width"], "height": video["height"],
        "fps": video["avg_frame_rate"], "codec": video["codec_name"],
        "pixelFormat": video.get("pix_fmt"), "durationSeconds": round(duration, 3),
        "decodedFrames": frames, "audioCodec": audio[0]["codec_name"] if audio else None,
        "audioChannels": audio[0].get("channels") if audio else None,
        "audioSampleRate": audio[0].get("sample_rate") if audio else None,
        "musicCredit": "Counting Stars — OneRepublic", "sizeBytes": path.stat().st_size,
        "checks": checks,
    }
    if not all(checks.values()):
        raise RuntimeError("Master verification failed: " + json.dumps(report, indent=2))
    return report


def main() -> None:
    global args
    args = parse_args()
    music = args.music.expanduser().resolve()
    output = args.output.expanduser().resolve()
    if not FFMPEG.exists() or not FFPROBE.exists():
        raise FileNotFoundError(f"FFmpeg tools missing from {TOOLS}")
    if not music.is_file():
        raise FileNotFoundError(f"Music file not found: {music}")
    if output.exists() and not args.force:
        raise FileExistsError(f"Output exists; pass --force to replace it: {output}")
    output.parent.mkdir(parents=True, exist_ok=True)
    WORK.mkdir(parents=True, exist_ok=True)

    segments: list[Path] = []
    total_segments = len(SCENES) + 2
    segments.append(create_title_card(INTRO, 1, intro=True))
    for i, scene in enumerate(SCENES, start=2):
        segments.append(create_scene_segment(scene, i, total_segments))
    segments.append(create_title_card(OUTRO, total_segments, intro=False))
    expected = assemble(segments, music, output)
    report = verify(output, expected)
    report["sourceMedia"] = "Genuine ASTRA app screen recordings (recorded at 1544x920 or 724x980, 30fps); upscaled and reframed to 4K."
    report["musicSource"] = "User-supplied file; audio file is not copied into the project."
    report["segmentDurations"] = {p.stem: round(float(ffprobe(p)["format"]["duration"]), 3) for p in segments}
    (WORK / "verification-4k-30.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
    (WORK / "CREDITS-4K-30.md").write_text(
        "# ASTRA OS V4 Horizon — Trailer credits\n\n"
        "Visuals: genuine ASTRA OS V4 application screen recordings from this project.\n\n"
        "Music: **Counting Stars — OneRepublic**. Audio supplied by the user; the original MP3 is not redistributed.\n\n"
        "Portfolio: **elemanprojects.com**.\n\n"
        "Format: 3840 × 2160, 30 fps, H.264 video, AAC stereo.\n\n"
        "The application recordings were captured at 1544 × 920 (landscape) and 724 × 980 (assistant panel), then reframed/upscaled for the 4K master.\n",
        encoding="utf-8")
    print(json.dumps(report, indent=2), flush=True)


if __name__ == "__main__":
    main()
