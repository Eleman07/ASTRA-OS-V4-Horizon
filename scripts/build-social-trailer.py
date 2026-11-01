"""Edit genuine ASTRA window recordings into silent landscape and portrait trailers."""
from pathlib import Path
import hashlib
import json
import shutil
import subprocess
import textwrap
import zipfile

ROOT = Path(__file__).resolve().parents[1]
WORK = ROOT / 'dist/trailer/2026-10-28'
BIN = ROOT / 'downloads/trailer-tools/ffmpeg-9.0.2-essentials_build/bin'
FFMPEG, FFPROBE = str(BIN / 'ffmpeg.exe'), str(BIN / 'ffprobe.exe')
PACK = WORK / 'social-pack'
EDITS = WORK / 'edits'
for directory in [PACK, PACK / 'clips', EDITS / 'text', EDITS / 'fonts', EDITS / 'segments']:
    directory.mkdir(parents=True, exist_ok=True)
for name in ['segoeui.ttf', 'seguisb.ttf']:
    shutil.copyfile(Path('C:/Windows/Fonts') / name, EDITS / 'fonts' / name)
shutil.copyfile(ROOT / 'src/Astra.Desktop/Assets/astra-logo.png', EDITS / 'logo.png')

SCENES = [
    dict(id='home', file='01-home', duration=6, start=2,
         title='Il tuo spazio. Il tuo ritmo.', short='Il tuo spazio.\nIl tuo ritmo.',
         sub='Una Home personale per concentrarti, creare ed esplorare.',
         roi=(0, 0, 1544, 920), detail=(240, 65, 940, 512)),
    dict(id='orbital', file='02-orbital-take2', duration=7, start=3,
         title='Il desktop, in orbita.', short='Il desktop,\nin orbita.',
         sub='File reali e una sfera animata con i colori del tuo tema.',
         roi=(490, 490, 580, 326), detail=(490, 490, 580, 326)),
    dict(id='ariane', file='03-ariane64', duration=7, start=7,
         title='Esplora un progetto in 3D.', short='Esplora\nin tre dimensioni.',
         sub='Ariane 64: ricostruzione didattica ASTRA da fonti ESA/ArianeGroup.',
         roi=(880, 350, 620, 440), detail=(875, 350, 620, 340)),
    dict(id='simulation', file='04-simulazione-orbitale', duration=7, start=2,
         title='Dai numeri al movimento.', short='Dai numeri\nal movimento.',
         sub='Calcolo orbitale locale e replay dei campioni in STEM Studio.',
         roi=(265, 65, 1235, 590), detail=(265, 160, 1220, 666)),
    dict(id='assistant', file='05-assistente-multimodale', duration=6, start=1,
         title='L’assistente, sul tuo PC.', short='L’assistente,\nsul tuo PC.',
         sub='Testo, documenti, OCR e dati. Scegli tu cosa condividere con l’assistente.',
         assistant=True),
    dict(id='widgets', file='06-widget-automazioni', duration=6, start=1,
         title='Costruisci il tuo spazio.', short='Costruisci\nil tuo spazio.',
         sub='Widget personali e automazioni mentre ASTRA è aperto.',
         roi=(240, 80, 1260, 750), detail=(245, 485, 850, 435)),
    dict(id='gaming', file='07-gaming', duration=5, start=1,
         title='La tua sessione, misurata.', short='La tua sessione,\nmisurata.',
         sub='Gaming Session Lab: CPU, GPU e RAM reali, sul tuo dispositivo.',
         roi=(245, 305, 1250, 375), detail=(245, 310, 1100, 600)),
    dict(id='creator', file='08-creator-palette', duration=5, start=1,
         title='Dall’asset alla tua idea.', short='Dall’asset\nalla tua idea.',
         sub='Creator: colori estratti dalle immagini, metadati e storyboard.',
         roi=(245, 310, 1250, 510), detail=(265, 455, 825, 450)),
    dict(id='plugins', file='09-plugin-catalogo', duration=5, start=1,
         title='43 modi per aggiungere strumenti.', short='43 plugin.\nNuovi strumenti.',
         sub='Estensioni dichiarative e operazioni locali nel Plugin SDK.',
         roi=(245, 85, 1250, 740), detail=(245, 90, 1080, 590)),
    dict(id='theme', file='11-personalizzazione', duration=5, start=1,
         title='Il tuo tema, fino ai dettagli.', short='Il tuo tema.\nI tuoi dettagli.',
         sub='Palette, sfondi, immagini animate e preferenze personali.',
         roi=(240, 65, 1260, 760), detail=(240, 70, 1250, 680)),
]


def run(command, label):
    with (EDITS / (label + '.log')).open('w', encoding='utf8') as log:
        result = subprocess.run(command, cwd=WORK, stdin=subprocess.DEVNULL,
                                stdout=log, stderr=log,
                                creationflags=subprocess.CREATE_NO_WINDOW)
    if result.returncode:
        raise RuntimeError((EDITS / (label + '.log')).read_text(encoding='utf8')[-7000:])


def inspect(path):
    result = subprocess.run([FFPROBE, '-v', 'error', '-show_streams', '-show_format', '-of', 'json', str(path)],
                            capture_output=True, text=True, check=True,
                            creationflags=subprocess.CREATE_NO_WINDOW)
    return json.loads(result.stdout)


def textfile(name, value):
    path = EDITS / 'text' / (name + '.txt')
    path.write_text(value, encoding='utf8')
    return path.relative_to(WORK).as_posix()


def draw(name, value, x, y, size, color='white', font='seguisb.ttf', extra=''):
    if '\n' in value:
        # Explicit line positions avoid font-metric-dependent leading in drawtext.
        return ','.join(draw(name+f'-line-{i}', line, x, f'({y})+{i*round(size*1.2)}', size, color, font, extra)
                        for i, line in enumerate(value.splitlines()))
    return (f"drawtext=fontfile=edits/fonts/{font}:textfile={textfile(name, value)}:"
            f"fontsize={size}:fontcolor={color}:x={x}:y={y}:line_spacing=12{extra}")


def crop(rect):
    x, y, width, height = rect
    return f'crop={width}:{height}:{x}:{y}'


def caption_lines(value, width=43):
    return '\n'.join(textwrap.wrap(value, width=width, break_long_words=False))


ENCODER = ['-c:v', 'h264_nvenc', '-preset', 'p5', '-rc', 'vbr', '-cq', '18', '-b:v', '0',
           '-pix_fmt', 'yuv420p', '-color_primaries', 'bt709', '-color_trc', 'bt709', '-colorspace', 'bt709']


def render(scene, index, portrait=False, card=None):
    width, height = (1080, 1920) if portrait else (1920, 1080)
    duration = 3 if card else scene['duration']
    input_path = WORK / '01-clips' / (scene['file'] + '.mp4')
    name = 'v2-' + ('vertical-' if portrait else 'wide-') + (card or scene['id'])
    output = EDITS / 'segments' / (name + '.mp4')
    if output.exists():
        print('Reusing ' + output.name, flush=True)
        return output, duration
    start = 0 if card else scene['start']
    base = f'[0:v]trim=start={start}:duration={duration},setpts=PTS-STARTPTS,fps=30'
    graph = []
    overlay = []
    if card:
        graph.append(base + f',scale={width}:{height}:force_original_aspect_ratio=increase,crop={width}:{height},boxblur=18:2,eq=brightness=-0.40:saturation=0.7[base]')
        logo_size = 180 if portrait else 150
        logo_y = 335 if portrait else 155
        graph.append(f'[1:v]scale={logo_size}:{logo_size},format=rgba[logo]')
        graph.append(f'[base][logo]overlay=x=(W-w)/2:y={logo_y}:shortest=1[card]')
        main = 'ASTRA OS V4' if card == 'intro' else 'Il tuo spazio.\nIl tuo ritmo.'
        headline_y = 675 if portrait else 405
        size = (80 if portrait else 112) if card == 'intro' else (82 if portrait else 98)
        overlay += [draw(name+'-title', main, '(w-text_w)/2', headline_y, size),
                    draw(name+'-horizon', 'H O R I Z O N', '(w-text_w)/2', 865 if portrait else 650, 44 if portrait else 38, '0xB3CFE5'),
                    draw(name+'-sub', 'Concentrati. Crea. Esplora.' if card == 'intro' else 'Workspace personale per Windows 11',
                         '(w-text_w)/2', 1020 if portrait else 765, 34 if portrait else 28, '0xDCE9F5', 'segoeui.ttf'),
                    'fade=t=in:d=0.4', f'fade=t=out:st={duration-0.35}:d=0.35']
        graph.append('[card]' + ','.join(overlay) + ',format=yuv420p[out]')
    elif scene.get('assistant'):
        graph.append(base + ',split=2[b][f]')
        graph.append(f'[b]scale={width}:{height}:force_original_aspect_ratio=increase,crop={width}:{height},boxblur=35:2,eq=brightness=-0.38:saturation=0.7[bg]')
        if portrait:
            graph.append('[f]scale=900:1218,format=yuv420p[panel]')
            graph.append('[bg][panel]overlay=90:400:shortest=1[base]')
            overlay += [draw(name+'-brand', 'ASTRA OS V4 / HORIZON', 70, 80, 26, '0xB3CFE5'),
                        draw(name+'-title', scene['short'], 70, 170, 66),
                        draw(name+'-sub', 'Testo, documenti, OCR e dati.\nContenuti scelti da te.', 70, 1680, 34, '0xB3CFE5', 'segoeui.ttf')]
        else:
            graph.append('[f]scale=650:880,format=yuv420p[panel]')
            graph.append('[bg][panel]overlay=1170:100:shortest=1[base]')
            overlay += [draw(name+'-brand', 'ASTRA OS V4 / HORIZON', 90, 65, 26, '0xB3CFE5'),
                        draw(name+'-title', 'L’assistente,\nsul tuo PC.', 90, 300, 86),
                        draw(name+'-sub', 'Testo, documenti e OCR.\nDati, scene 3D e widget.\nInput scelti da te.', 95, 610, 34, '0xB3CFE5', 'segoeui.ttf'),
                        draw(name+'-footer', 'Interfaccia reale · Contesto dalla simulazione STEM', 95, 965, 24, '0xB3CFE5', 'segoeui.ttf')]
        graph.append('[base]' + ','.join(overlay) + ',format=yuv420p[out]')
    elif portrait:
        graph.append(base + ',split=3[b][o][d]')
        graph.append('[b]scale=1080:1920:force_original_aspect_ratio=increase,crop=1080:1920,boxblur=35:2,eq=brightness=-0.42:saturation=0.6[bg]')
        graph.append('[o]scale=990:590:force_original_aspect_ratio=decrease,pad=990:590:(ow-iw)/2:(oh-ih)/2:color=0x0A1931[overview]')
        graph.append('[d]' + crop(scene['detail']) + ',scale=990:540:force_original_aspect_ratio=decrease,pad=990:540:(ow-iw)/2:(oh-ih)/2:color=0x0A1931[detail]')
        graph.append('[bg][overview]overlay=45:420:shortest=1[one]')
        graph.append('[one][detail]overlay=45:1045:shortest=1[base]')
        overlay += ['drawbox=x=44:y=419:w=992:h=592:color=0xB3CFE5@0.65:t=2',
                    'drawbox=x=44:y=1044:w=992:h=542:color=0xB3CFE5@0.65:t=2',
                    draw(name+'-brand', 'ASTRA OS V4 / HORIZON', 70, 80, 26, '0xB3CFE5'),
                    draw(name+'-title', scene['short'], 70, 170, 66),
                    draw(name+'-sub', caption_lines(scene['sub'], 43), 70, 1660, 33, '0xDCE9F5', 'segoeui.ttf'),
                    draw(name+'-footer', f'{index:02d} / 10    ·    WINDOWS 11', 70, 1825, 24, '0xB3CFE5', 'segoeui.ttf'),
                    'drawbox=x=0:y=1914:w=iw:h=6:color=0x4A7FA7:t=fill']
        graph.append('[base]' + ','.join(overlay) + ',format=yuv420p[out]')
    else:
        graph.append(base + ',' + crop(scene['roi']) + ',scale=1920:1080:force_original_aspect_ratio=decrease,pad=1920:1080:(ow-iw)/2:(oh-ih)/2:color=0x0A1931,'
                     f"zoompan=z='1.015+0.025*on/{int(duration*30)}':x='iw/2-iw/zoom/2':y='ih/2-ih/zoom/2':d=1:s=1920x1080:fps=30[base]")
        overlay += ['drawbox=x=0:y=0:w=iw:h=100:color=0x061127@0.9:t=fill',
                    'drawbox=x=0:y=910:w=iw:h=170:color=0x061127@0.95:t=fill',
                    draw(name+'-brand', 'ASTRA OS V4 / HORIZON', 80, 38, 26, '0xB3CFE5'),
                    draw(name+'-index', f'{index:02d} / 10', 'w-text_w-80', 38, 25, '0xB3CFE5'),
                    'drawbox=x=80:y=86:w=110:h=3:color=0x4A7FA7:t=fill',
                    draw(name+'-title', scene['title'], 80, 934, 46),
                    draw(name+'-sub', scene['sub'], 82, 1005, 26, '0xDCE9F5', 'segoeui.ttf'),
                    'drawbox=x=0:y=1075:w=iw:h=5:color=0x4A7FA7:t=fill']
        graph.append('[base]' + ','.join(overlay) + ',format=yuv420p[out]')
    filter_path = EDITS / (name + '.ffgraph')
    filter_path.write_text(';\n'.join(graph), encoding='utf8')
    command = [FFMPEG, '-hide_banner', '-nostdin', '-y', '-i', str(input_path), '-loop', '1', '-framerate', '30', '-i', 'edits/logo.png',
               '-filter_complex_threads', '2', '-filter_complex', filter_path.read_text(encoding='utf8'), '-map', '[out]', '-t', str(duration),
               '-an', '-sn', '-dn', *ENCODER, '-r', '30', '-movflags', '+faststart', str(output)]
    print('Rendering ' + name, flush=True)
    run(command, name)
    return output, duration


def assemble(portrait):
    segments = [render(SCENES[0], 0, portrait, 'intro')]
    segments += [render(scene, i+1, portrait) for i, scene in enumerate(SCENES)]
    segments += [render(SCENES[0], 0, portrait, 'outro')]
    name = 'ASTRA-HORIZON-Reel-9x16' if portrait else 'ASTRA-HORIZON-Trailer-16x9'
    destination = PACK / (name + '.mp4')
    command = [FFMPEG, '-hide_banner', '-nostdin', '-y']
    for path, _ in segments:
        command += ['-i', str(path)]
    graph = []
    for i in range(len(segments)):
        graph.append(f'[{i}:v]settb=1/30,setpts=PTS-STARTPTS,format=yuv420p[v{i}]')
    running = segments[0][1]
    previous = 'v0'
    for i in range(1, len(segments)):
        offset = running - 0.35
        output = f'x{i}'
        graph.append(f'[{previous}][v{i}]xfade=transition=fade:duration=0.35:offset={offset:.6f}[{output}]')
        previous = output
        running += segments[i][1] - 0.35
    path = EDITS / (name + '.ffgraph')
    path.write_text(';\n'.join(graph), encoding='utf8')
    command += ['-filter_complex_threads', '2', '-filter_complex', path.read_text(encoding='utf8'), '-map', f'[{previous}]',
                '-an', '-sn', '-dn', *ENCODER, '-r', '30', '-movflags', '+faststart', str(destination)]
    print('Assembling ' + destination.name, flush=True)
    run(command, name)
    return destination


def main():
    for index, scene in enumerate(SCENES, 1):
        source = WORK / '01-clips' / (scene['file'] + '.mp4')
        metadata = inspect(source)
        if any(s['codec_type'] == 'audio' for s in metadata['streams']):
            raise RuntimeError('Audio track found in source: ' + source.name)
        shutil.copyfile(source, PACK / 'clips' / f"{index:02d}-{scene['id']}.mp4")
    landscape = assemble(False)
    portrait = assemble(True)
    outputs = []
    for file in sorted(PACK.rglob('*.mp4')):
        metadata = inspect(file)
        video = [s for s in metadata['streams'] if s['codec_type'] == 'video']
        assert len(video) == 1 and not any(s['codec_type'] == 'audio' for s in metadata['streams'])
        with file.open('rb') as stream:
            sha = hashlib.file_digest(stream, 'sha256').hexdigest()
        outputs.append({'file': file.relative_to(PACK).as_posix(), 'width': video[0]['width'], 'height': video[0]['height'],
                        'duration': float(metadata['format']['duration']), 'fps': video[0]['r_frame_rate'],
                        'codec': video[0]['codec_name'], 'audioStreams': 0, 'sha256': sha, 'bytes': file.stat().st_size})
    (PACK / 'verification.json').write_text(json.dumps(outputs, indent=2), encoding='utf8')
    readme = '''# ASTRA OS V4 Horizon — social video pack

Riprese reali della finestra ASTRA in uso, il 28 ottobre 2026.

- ASTRA-HORIZON-Trailer-16x9.mp4: trailer orizzontale 1920 × 1080, 30 fps.
- ASTRA-HORIZON-Reel-9x16.mp4: versione verticale 1080 × 1920, 30 fps, con panoramica e dettaglio.
- clips/: 10 clip separate, senza titoli aggiunti, per un montaggio personale.
- verification.json: durata, risoluzione, codec, hash e verifica delle tracce audio di ogni video.

Nessuna sorgente microfono o loopback/audio di sistema è stata aperta dal registratore. Tutti gli MP4 contengono solo una traccia video H.264, senza tracce audio. Nessuna musica o voce è stata aggiunta.

Il riquadro del nome account e la riga di stato inferiore sono coperti già nella cattura della finestra principale. La finestra dell'assistente appartiene ad ASTRA. I dettagli del montaggio verticale sono ritagli delle stesse riprese; i filmati non ricostruiscono l'interfaccia. Le viste statiche mantengono l'ultimo fotogramma reale quando Windows non emette nuovi fotogrammi.

La clip AI mostra l'interfaccia e l'importazione dei dati di una simulazione. La richiesta di una risposta testuale ha superato l'attesa: nessuna risposta AI è stata aggiunta o ricreata in postproduzione. La scena Multitasking è stata esclusa perché l'elenco includeva titoli di altre applicazioni.

Per la dimostrazione Creator è stato aggiunto alla bacheca soltanto il banner già incluso in ASTRA. La breve sessione di telemetria Gaming è stata terminata; il modello AI è stato fermato dopo le riprese. ASTRA è stato riportato alla Home e allo schermo intero. Nessun cursore, tema o avvio automatico Windows è stato cambiato durante le riprese.

## Scene

Home; Orbital con file reali; Ariane 64; replay orbitale; assistente multimodale; widget; Gaming Session Lab; Creator/palette; 43 plugin; personalizzazione.

## Crediti

Software, UI e logo: progetto ASTRA OS. La mesh Ariane 64 è una ricostruzione didattica ASTRA basata su dati pubblici ESA/ArianeGroup, non CAD ufficiale né approvazione ESA. Crediti completi nel pacchetto applicativo: Graphics3D/web/models/CREDITS.md e licenses/ariane64.md. Il trailer non usa musica o filmati stock esterni.

La confezione non contiene eseguibili FFmpeg, font Windows, dati del profilo utente o registrazioni delle altre applicazioni. Nessun video è stato pubblicato o caricato sui social.
'''
    (PACK / 'README-TRAILER.md').write_text(readme, encoding='utf8')
    for file, name in [(landscape, 'poster-16x9.jpg'), (portrait, 'poster-9x16.jpg')]:
        run([FFMPEG, '-hide_banner', '-loglevel', 'error', '-y', '-ss', '7', '-i', str(file), '-frames:v', '1', str(PACK/name)], name)
    archive = WORK / 'ASTRA-HORIZON-SOCIAL-PACK.zip'
    with zipfile.ZipFile(archive, 'w', compression=zipfile.ZIP_DEFLATED, compresslevel=3) as package:
        for file in sorted(PACK.rglob('*')):
            if file.is_file():
                package.write(file, 'ASTRA-HORIZON-SOCIAL-PACK/' + file.relative_to(PACK).as_posix())
    print(json.dumps({'archive': str(archive), 'archiveBytes': archive.stat().st_size, 'outputs': outputs}, indent=2), flush=True)


if __name__ == '__main__':
    main()
