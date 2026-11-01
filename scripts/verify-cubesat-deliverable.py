"""Independently validate actual CubeSat project files; default never runs code.

--reviewed-project requires manual review and an exact --fingerprint before
executing a fresh copy in a QA venv. No dependencies are installed. A venv is
not an OS or network sandbox. Fixed numerical fixtures are reference-only and
cannot establish that the requested remote model produced a working project.
"""
from __future__ import annotations
import argparse
import ast
import csv
from datetime import datetime, timezone
import hashlib
import json
import math
import os
from pathlib import Path, PurePosixPath
import re
import shutil
import stat
import subprocess
import sys
import uuid
import venv
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
TEMPLATE = ROOT / "tests/fixtures/orbital-reference"
MU, EARTH_RADIUS, ALTITUDE = 398600.4418, 6378.137, 400.0
RADIUS = EARTH_RADIUS + ALTITUDE
SPEED = math.sqrt(MU/RADIUS)
PERIOD = 2*math.pi*math.sqrt(RADIUS**3/MU)
FIELDS = ("t_s", "x_km", "y_km", "z_km", "vx_km_s", "vy_km_s", "vz_km_s")


def norm(values):
    return math.sqrt(sum(value*value for value in values))


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def independent_reference():
    """Independent implementation, not imported from submitted project code."""
    initial = [RADIUS, 0., 0., 0., SPEED, 0.]
    def rhs(state):
        scale = -MU/sum(value*value for value in state[:3])**1.5
        return state[3:]+[scale*value for value in state[:3]]
    runs = []
    for count in (128, 256, 512):
        state, step, drift = initial[:], PERIOD/count, 0.
        energy0 = SPEED*SPEED/2-MU/RADIUS
        for _ in range(count):
            slopes = []
            for fraction in (0., .5, .5, 1.):
                intermediate = state if not slopes else [value+step*fraction*slope for value,slope in zip(state,slopes[-1])]
                slopes.append(rhs(intermediate))
            state = [value+step*(a+2*b+2*c+d)/6 for value,a,b,c,d in zip(state,*slopes)]
            energy = sum(value*value for value in state[3:])/2-MU/norm(state[:3])
            drift = max(drift,abs((energy-energy0)/energy0))
        error = norm([a-b for a,b in zip(state[:3],initial[:3])])
        runs.append({"steps":count,"stepSeconds":step,"positionErrorKm":error,"maxRelativeEnergyDrift":drift})
    return {"kind":"independent-validator-tool-calculation","scope":"Reference calculations only; not an ASTRA/model-generated project success or flight validation.","constants":{"muKm3S2":MU,"earthRadiusKm":EARTH_RADIUS,"altitudeKm":ALTITUDE,"frame":"ECI"},"circular":{"radiusKm":RADIUS,"speedKmS":SPEED,"periodSeconds":PERIOD,"specificEnergyKm2S2":-MU/(2*RADIUS)},"rk4":runs,"observedOrder":math.log2(runs[0]["positionErrorKm"]/runs[1]["positionErrorKm"])}


def inventory(project):
    result = {}
    pending = [project]
    while pending:
        folder = pending.pop()
        with os.scandir(folder) as entries:
            for entry in entries:
                if entry.name in {".venv", "__pycache__", ".git"}:
                    continue
                info = entry.stat(follow_symlinks=False)
                if entry.is_symlink() or getattr(info,"st_file_attributes",0)&stat.FILE_ATTRIBUTE_REPARSE_POINT:
                    raise ValueError("Linked project entry is not accepted")
                path = Path(entry.path)
                if entry.is_dir(follow_symlinks=False):
                    pending.append(path)
                elif entry.is_file(follow_symlinks=False):
                    result[path.relative_to(project).as_posix()] = path
    return result


def fingerprint(project):
    files=inventory(project)
    return hashlib.sha256("".join(name+":"+digest(path)+"\n" for name,path in sorted(files.items())).encode()).hexdigest()


def project_file(project, name):
    if not isinstance(name,str):
        raise ValueError("Artifact paths must be strings")
    path = PurePosixPath(name)
    if path.is_absolute() or ":" in name or "\\" in name or any(part in {"", ".", ".."} for part in name.split("/")):
        raise ValueError("Unsafe artifact path")
    candidate = project/name
    if not candidate.resolve().is_relative_to(project) or not candidate.is_file():
        raise ValueError("Missing or outside artifact: "+name)
    return candidate


def read_trajectory(path, columns=None, scale=None):
    rows = []
    columns=columns or {name:name for name in FIELDS}
    scale=scale or {}
    if set(columns)!=set(FIELDS) or not all(isinstance(value,str) for value in columns.values()):
        raise ValueError("CSV column mapping must name all seven canonical state/time quantities")
    if any(key not in FIELDS or not math.isfinite(float(value)) or float(value)<=0 for key,value in scale.items()):
        raise ValueError("CSV conversion scales must be explicit finite positive factors")
    with path.open(newline="",encoding="utf-8-sig") as stream:
        reader = csv.DictReader(stream)
        if not set(columns.values()) <= set(reader.fieldnames or []):
            raise ValueError("Trajectory CSV must declare units in all seven state/time column names")
        for record in reader:
            row = tuple(float(record[columns[name]])*float(scale.get(name,1)) for name in FIELDS)
            if not all(math.isfinite(value) for value in row):
                raise ValueError("Trajectory contains non-finite state/time")
            if rows and row[0] <= rows[-1][0]:
                raise ValueError("Trajectory time must strictly increase")
            rows.append(row)
    if len(rows)<3 or abs(rows[0][0])>1e-10:
        raise ValueError("Trajectory must start at zero with multiple propagated states")
    return rows


def measured_trajectory(rows):
    initial = rows[0][1:]
    radius, speed = norm(initial[:3]), norm(initial[3:])
    radial_dot = sum(a*b for a,b in zip(initial[:3],initial[3:]))
    if abs(radius-RADIUS)>1e-6 or abs(speed-SPEED)>1e-8 or abs(radial_dot)>1e-5:
        raise ValueError("Initial state is not the required circular400km orbit with coherent km/s units")
    def energy(state):
        return sum(value*value for value in state[3:])/2-MU/norm(state[:3])
    energy0 = energy(initial)
    final_time = rows[-1][0]
    phase = math.sqrt(MU/RADIUS**3)*final_time
    angular_rate = math.sqrt(MU/RADIUS**3)
    exact_r = [r*math.cos(phase)+v/angular_rate*math.sin(phase) for r,v in zip(initial[:3],initial[3:])]
    exact_v = [-angular_rate*r*math.sin(phase)+v*math.cos(phase) for r,v in zip(initial[:3],initial[3:])]
    return {"samples":len(rows),"durationSeconds":final_time,"nominalStepSeconds":max(b[0]-a[0] for a,b in zip(rows,rows[1:])),"maxRelativeEnergyDrift":max(abs((energy(row[1:])-energy0)/energy0) for row in rows),"finalPositionErrorKm":norm([a-b for a,b in zip(rows[-1][1:4],exact_r)]),"finalVelocityErrorKmS":norm([a-b for a,b in zip(rows[-1][4:7],exact_v)])}


def validate_numerics(project):
    """Only independent CSV math; never claim all eight deliverables accepted."""
    inventory(project)
    manifest=json.loads(project_file(project,"cubesat-manifest.json").read_text(encoding="utf8"))
    artifacts=manifest["artifacts"]
    kwargs={"columns":manifest.get("csvColumns"),"scale":manifest.get("csvScale")}
    coarse=measured_trajectory(read_trajectory(project_file(project,artifacts["trajectory"]),**kwargs))
    fine=measured_trajectory(read_trajectory(project_file(project,artifacts["trajectoryHalfStep"]),**kwargs))
    ratio=coarse["finalPositionErrorKm"]/fine["finalPositionErrorKm"] if fine["finalPositionErrorKm"]>0 else float("inf")
    checks={"fullOrbitCommonFinalTime":coarse["durationSeconds"]>=PERIOD-1e-8 and fine["durationSeconds"]>=PERIOD-1e-8 and abs(coarse["durationSeconds"]-fine["durationSeconds"])<1e-8,"nominalStepHalved":math.isclose(coarse["nominalStepSeconds"],2*fine["nominalStepSeconds"],rel_tol=0,abs_tol=1e-8),"energyBound":coarse["maxRelativeEnergyDrift"]<1e-7 and fine["maxRelativeEnergyDrift"]<1e-8,"fineAnalyticStateAccuracy":fine["finalPositionErrorKm"]<.01 and fine["finalVelocityErrorKmS"]<1e-5,"fourthOrderConvergence":math.isfinite(ratio) and 8<ratio<32}
    passed=all(checks.values())
    return {"kind":"independent-cubesat-numerical-artifact-validation","status":"PASS_NUMERICAL_CHECKS_ONLY" if passed else "FAIL","passed":passed,"fullDeliverableValidated":False,"remoteModelProjectAcceptance":"UNVERIFIED: numerical CSV checks alone do not establish model provenance, code execution or all eight deliverables.","projectCodeExecutedByValidator":False,"executionReceiptIndependentlyAuthenticated":False,"projectFingerprintSha256":fingerprint(project),"declaredProvenance":manifest.get("provenance",manifest.get("producer","UNVERIFIED")),"csvColumns":kwargs["columns"],"csvScale":kwargs["scale"],"checks":checks,"measuredFromCsv":{"coarse":coarse,"fine":fine,"positionErrorRatio":ratio if math.isfinite(ratio) else None},"reference":independent_reference()}


def validate(project):
    checks=[]
    def check(name,ok,detail="",method="artifact inspection"):
        checks.append({"name":name,"passed":bool(ok),"method":method,"detail":detail})
    files=inventory(project)
    project_fingerprint=fingerprint(project)
    manifest=json.loads(project_file(project,"cubesat-manifest.json").read_text(encoding="utf8"))
    artifacts=manifest["artifacts"]
    texts={key:project_file(project,artifacts[key]).read_text(encoding="utf8") for key in ("requirements","assumptions","architecture","readme","report")}
    check("REQ01 acceptance matrix has eight distinct requirements and measurable criteria",len(set(re.findall(r"REQ-\d+",texts["requirements"])))>=8 and "criterio" in texts["requirements"].lower() and any(symbol in texts["requirements"] for symbol in ("<", "entro", "tolleranz")),method="structural heuristic; human document review still required")
    constants=manifest.get("constants",{})
    check("REQ02 explicit benchmark constants and ECI frame",all(math.isclose(float(constants.get(k,float("nan"))),v,rel_tol=0,abs_tol=1e-9) for k,v in (("muKm3S2",MU),("earthRadiusKm",EARTH_RADIUS),("altitudeKm",ALTITUDE))) and constants.get("frame")=="ECI")
    check("REQ02 units and assumptions are documented",all(token in texts["assumptions"] for token in ("400", "6378.137", "398600.4418", "ECI", "km", "s")),method="structural heuristic; numerical CSV units checked separately")
    check("REQ03 equations and architecture are documented",all(token in texts["architecture"].lower() for token in ("dr/dt", "dv/dt", "rk4", "csv", "test")),method="structural heuristic; dynamics accuracy checked from CSV")
    python_names=[manifest["entrypoint"],artifacts["propagator"],artifacts["plotter"],artifacts["tests"]]
    for name in python_names:
        tree=ast.parse(project_file(project,name).read_text(encoding="utf8"),filename=name)
        incomplete=any(isinstance(node,ast.Constant) and node.value is Ellipsis or isinstance(node,ast.Name) and node.id=="NotImplementedError" for node in ast.walk(tree))
        check("REQ04 complete parseable Python: "+name,not incomplete,method="Python AST parsing only; no execution")
    dependencies=project_file(project,artifacts["dependencies"]).read_text(encoding="utf8")
    check("REQ04 dependency declaration and reproducible Python commands",bool(dependencies.strip()) and "python" in texts["readme"].lower() and "venv" in texts["readme"].lower())
    check("REQ07 Italian README and technical report",all(token in texts["report"].lower() for token in ("risultati", "ipotesi", "limiti", "energia")) and "esecuzione" in texts["readme"].lower(),method="structural language/section heuristic; human prose review still required")
    check("REQ06 J2 is explicitly future/unverified and physical limits are stated","j2" in texts["report"].lower() and "UNVERIFIED_NOT_IMPLEMENTED" in texts["report"] and any(token in texts["report"].lower() for token in ("atmosfera", "attrito", "drag")))
    plots=[]
    for name in artifacts["plots"]:
        svg=ET.parse(project_file(project,name)).getroot()
        coordinate_text=" ".join(element.get("points","")+" "+element.get("d","") for element in svg.iter())
        finite=not re.search(r"(?i)(?<![a-z])(nan|inf(?:inity)?)(?![a-z])",coordinate_text)
        point_count=sum(len(element.get("points","").split()) for element in svg.iter() if element.tag.endswith("polyline"))
        plots.append(svg.tag.endswith("svg") and finite and point_count>=3)
    check("REQ04 two actual SVG plots contain finite multi-sample polylines",len(plots)>=2 and all(plots),method="XML and coordinate inspection")
    note_paths=[project_file(project,name) for name in artifacts["notes"]]
    stems={path.stem.casefold() for path in note_paths}
    links=[target.split("|",1)[0].split("#",1)[0].strip() for path in note_paths for target in re.findall(r"\[\[([^\]]+)\]\]",path.read_text(encoding="utf8"))]
    unresolved=[link for link in links if Path(link).stem.casefold() not in stems]
    check("REQ08 at least three final Obsidian notes have resolvable wikilinks",len(note_paths)>=3 and len(links)>=len(note_paths) and not unresolved,", ".join(unresolved))
    sources=project/"fonti_vault.md"
    check("REQ08 actual vault-source ledger or explicit absence is recorded",sources.is_file() and bool(sources.read_text(encoding="utf8").strip()),"Ledger text requires review against actual ASTRA retrieval records; validator does not infer retrieval from prose.")
    measured={}
    try:
        coarse_rows=read_trajectory(project_file(project,artifacts["trajectory"]),manifest.get("csvColumns"),manifest.get("csvScale"))
        fine_rows=read_trajectory(project_file(project,artifacts["trajectoryHalfStep"]),manifest.get("csvColumns"),manifest.get("csvScale"))
        coarse,fine=measured_trajectory(coarse_rows),measured_trajectory(fine_rows)
        measured={"coarse":coarse,"fine":fine}
        check("REQ05 both CSVs span at least one full orbit at a common final time",coarse["durationSeconds"]>=PERIOD-1e-8 and fine["durationSeconds"]>=PERIOD-1e-8 and abs(coarse["durationSeconds"]-fine["durationSeconds"])<1e-8,method="CSV state/time calculations")
        check("REQ05 actual nominal step is halved",math.isclose(coarse["nominalStepSeconds"],2*fine["nominalStepSeconds"],rel_tol=0,abs_tol=1e-8),method="CSV time differences")
        check("REQ05 energy conservation computed from every CSV state",coarse["maxRelativeEnergyDrift"]<1e-7 and fine["maxRelativeEnergyDrift"]<1e-8,json.dumps({"coarse":coarse["maxRelativeEnergyDrift"],"fine":fine["maxRelativeEnergyDrift"]}),"independent v²/2−μ/r calculation; reported energy column ignored")
        check("REQ05 fine state agrees with analytic circular solution",fine["finalPositionErrorKm"]<.01 and fine["finalVelocityErrorKmS"]<1e-5,method="independent analytic ECI state at exact final time")
        ratio=coarse["finalPositionErrorKm"]/fine["finalPositionErrorKm"] if fine["finalPositionErrorKm"]>0 else float("inf")
        measured["positionErrorRatio"]=ratio if math.isfinite(ratio) else None
        check("REQ05 observed step-halving convergence matches fourth-order RK4",math.isfinite(ratio) and 8<ratio<32,str(ratio),"independent final position-error ratio; no trust in claimed convergence")
        metrics=json.loads(project_file(project,artifacts["metrics"]).read_text(encoding="utf8"))
        check("REQ05 reported speed/period match independent reference",math.isclose(metrics["reference"]["speedKmS"],SPEED,rel_tol=0,abs_tol=1e-10) and math.isclose(metrics["reference"]["periodSeconds"],PERIOD,rel_tol=0,abs_tol=1e-7))
        discrepancies=[]
        for label,actual in (("coarse",coarse),("fine",fine)):
            for key in actual:
                reported=metrics["runs"][label][key]
                if not math.isclose(float(reported),float(actual[key]),rel_tol=1e-6,abs_tol=1e-9):
                    discrepancies.append(label+"."+key)
        check("REQ06 metrics match independently recomputed CSV quantities",not discrepancies,", ".join(discrepancies))
        hashes=metrics.get("codeSha256",{})
        check("REQ06 execution record source hashes match actual submitted Python",set(hashes)==set(python_names) and all(digest(project_file(project,name))==expected for name,expected in hashes.items()),method="SHA256 association; self-authored receipt alone is not proof of execution")
        tests=json.loads(project_file(project,artifacts["testResults"]).read_text(encoding="utf8"))
        check("REQ05 test receipt records actual unittest names and zero failures",tests.get("passed") is True and tests.get("run",0)>=6 and tests.get("failures")==0 and tests.get("errors")==0 and len(re.findall(r"test_\w+",tests.get("log","")))>=6,method="receipt inspection; not independent execution")
        check("REQ06 metrics execution status and test receipt agree",metrics.get("executed") is True and metrics.get("status")=="PASS" and metrics.get("tests")=={key:tests[key] for key in ("passed","run","failures","errors")} and bool(metrics.get("executedAtUtc")))
    except (ValueError,KeyError,TypeError,ZeroDivisionError,OSError) as error:
        check("Numerical artifact validation completed",False,str(error))
    passed=all(item["passed"] for item in checks)
    return {"kind":"independent-cubesat-artifact-validation","verifiedAtUtc":datetime.now(timezone.utc).isoformat(),"status":"PASS_ARTIFACT_CHECKS" if passed else "FAIL","passed":passed,"projectFingerprintSha256":project_fingerprint,"projectCodeExecutedByValidator":False,"executionReceiptIndependentlyAuthenticated":False,"remoteModelProjectAcceptance":"UNVERIFIED: establish model provenance and independently observed execution separately; fixed reference output is not model-generated success.","declaredProvenance":manifest.get("provenance",manifest.get("producer","UNVERIFIED")),"scope":"Actual files, state CSV calculations and receipt consistency. Static prose checks are heuristics. Does not certify flight, scientific source quality or arbitrary agent capability.","checks":checks,"measuredFromCsv":measured,"reference":independent_reference()}


def execute_reviewed(project, expected_fingerprint, numerics_only=False):
    if not re.fullmatch(r"[a-fA-F0-9]{64}",expected_fingerprint or "") or fingerprint(project)!=expected_fingerprint.lower():
        raise ValueError("Execution refused: exact reviewed project fingerprint is missing or changed")
    manifest=json.loads(project_file(project,"cubesat-manifest.json").read_text(encoding="utf8"))
    entrypoint=manifest.get("execution",{}).get("entrypoint",manifest.get("entrypoint"))
    source_entrypoint=project_file(project,entrypoint)
    if source_entrypoint.suffix.lower()!=".py":
        raise ValueError("Reviewed entrypoint must be a Python source file")
    arguments=manifest.get("execution",{}).get("arguments",[])
    if not isinstance(arguments,list) or not all(isinstance(value,str) and "\0" not in value and not Path(value).is_absolute() and not re.match(r"^[A-Za-z]:",value) and ".." not in value.replace("\\","/").split("/") for value in arguments):
        raise ValueError("Reviewed arguments must be strings without absolute/outside paths")
    stage=ROOT/"dist/cubesat-reviewed-qa"/uuid.uuid4().hex
    target=stage/"project"
    stage.mkdir(parents=True)
    target.mkdir()
    for name,path in inventory(project).items():
        destination=target/name
        destination.parent.mkdir(parents=True,exist_ok=True)
        shutil.copyfile(path,destination)
    if fingerprint(target)!=expected_fingerprint.lower():
        raise ValueError("Execution refused: staged files differ from the reviewed fingerprint")
    # Previously supplied output cannot masquerade as output produced by this
    # observed run. Delete only mapped generated files in this new QA copy.
    artifacts=manifest.get("artifacts",{})
    output_names=[artifacts[key] for key in ("trajectory","trajectoryHalfStep","metrics","testResults") if key in artifacts]
    output_names+=artifacts.get("plots",[])
    for name in output_names:
        path=target/name
        if path.exists():
            project_file(target,name).unlink()
    venv.EnvBuilder(with_pip=False).create(stage/"venv")
    python=stage/"venv"/("Scripts/python.exe" if os.name=="nt" else "bin/python")
    environment={key:os.environ[key] for key in ("SystemRoot","WINDIR","COMSPEC") if key in os.environ}
    environment.update({"TEMP":str(stage),"TMP":str(stage),"PATH":str(python.parent)})
    # Restore script-directory imports only for the manually reviewed staged
    # source tree. Standard runner modules are imported before adding it.
    bootstrap="import pathlib,runpy,sys; entry=pathlib.Path(sys.argv[1]); root=pathlib.Path(sys.argv[2]); sys.argv=[str(entry)]+sys.argv[3:]; sys.path[:0]=[str(entry.parent),str(root)]; runpy.run_path(str(entry),run_name='__main__')"
    result=subprocess.run([str(python),"-I","-S","-B","-X","utf8","-c",bootstrap,str(target/entrypoint),str(target),*arguments],cwd=target,env=environment,capture_output=True,text=True,timeout=120)
    execution={"kind":"manually-reviewed-project-execution","reviewedFingerprintSha256":expected_fingerprint.lower(),"exitCode":result.returncode,"stdout":result.stdout,"stderr":result.stderr,"stage":stage.relative_to(ROOT).as_posix(),"pythonIsolation":"-I -S -B -X utf8","packagesInstalledByRunner":False,"providerCredentialsInherited":False,"networkIsolation":False,"scope":"Reviewed code runs with account permissions; directory/venv is not a security sandbox. Model provenance must be established separately."}
    (stage/"execution.json").write_text(json.dumps(execution,indent=2,ensure_ascii=False)+"\n",encoding="utf8")
    try:
        report=validate_numerics(target) if numerics_only else validate(target)
    except (ValueError,KeyError,TypeError,SyntaxError,OSError) as error:
        report={"kind":"independent-cubesat-artifact-validation","passed":False,"status":"FAIL","validationError":str(error),"remoteModelProjectAcceptance":"UNVERIFIED"}
    report["reviewedProjectExecution"]=execution
    report["passed"]=bool(report.get("passed")) and result.returncode==0
    if not report["passed"]:
        report["status"]="FAIL"
    report["projectCodeExecutedByValidator"]=True
    report["executionReceiptIndependentlyAuthenticated"]=result.returncode==0
    report["executionScope"]="Fresh QA venv and copy of exact manually reviewed project fingerprint. No dependency installation or network isolation; not a security sandbox."
    report["executionExitCode"]=result.returncode
    return report


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project",type=Path)
    parser.add_argument("--reference-only",action="store_true")
    parser.add_argument("--reviewed-project",action="store_true",help="Execute only after manual review, bound to --fingerprint")
    parser.add_argument("--fingerprint",help="Exact SHA256 project fingerprint approved after manual review")
    parser.add_argument("--fingerprint-only",action="store_true",help="Calculate the complete review fingerprint without executing code")
    parser.add_argument("--numerics-only",action="store_true",help="Validate actual CSV math without requiring the full deliverable/receipt schema")
    parser.add_argument("--report",type=Path)
    args=parser.parse_args()
    try:
        if args.reference_only:
            report=independent_reference()
        elif args.project:
            project=args.project.resolve(strict=True)
            if args.fingerprint_only:
                report={"kind":"project-review-fingerprint","projectFingerprintSha256":fingerprint(project),"projectCodeExecutedByValidator":False}
            else:
                report=execute_reviewed(project,args.fingerprint,args.numerics_only) if args.reviewed_project else validate_numerics(project) if args.numerics_only else validate(project)
        else:
            parser.error("--project or --reference-only is required")
        if args.report:
            destination=args.report.resolve()
            if not (destination.is_relative_to(ROOT/"dist") or destination.is_relative_to(ROOT/"docs/benchmarks")):
                raise ValueError("Reports may only be written below workspace dist or docs/benchmarks")
            destination.parent.mkdir(parents=True,exist_ok=True)
            destination.write_text(json.dumps(report,indent=2,ensure_ascii=False,allow_nan=False)+"\n",encoding="utf8")
        print(json.dumps({key:value for key,value in report.items() if key not in {"checks","reference","measuredFromCsv"}},indent=2,ensure_ascii=False,allow_nan=False))
        return 0 if report.get("passed",True) else 1
    except (ValueError,KeyError,TypeError,SyntaxError,OSError,RuntimeError,subprocess.SubprocessError) as error:
        print(json.dumps({"status":"FAIL","error":str(error),"projectCodeExecutedByValidator":False}),file=sys.stderr)
        return 1


if __name__=="__main__":
    raise SystemExit(main())
