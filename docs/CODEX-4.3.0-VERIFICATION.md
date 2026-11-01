# ASTRA 4.3.0 verification

Observed on Windows 11 x64, 2026-10-31 / 2026-11-01. These checks do not certify every possible task, a flight system, or every upstream model.

| Check | Observed result |
| --- | --- |
| Core multi-agent regressions | 184 checks passed; role methods, complete handoffs, checkpoint recovery, artifact validation, fresh Codex profiles and preservation of explicit OpenRouter selection |
| Static regressions | 63 tests passed |
| Native interface | 10 checks passed, including complete objective/source text over 100,000 characters |
| Codex connection | Actual selected-model answer preserved calibration number and source ID |
| Image input | Actual Codex interpretation read the calibration number and identified a blue triangle; Windows OCR read the text |
| Audio | Whisper small ran on CPU; calibration digits survived, but some English words were misrecognized. Review transcripts |
| Speech output | Windows TTS generated a nonempty 131,850-byte WAV |
| Worker recovery | Actual packaged supervisor restarted a deliberately terminated idle worker in an isolated profile |
| CubeSat generation | Five agent handoffs, eight successful model responses, selected `gpt-6.1-sol`, no supplied reference implementation |
| Generated CubeSat code | All 56 pytest cases passed, unchanged generated sources, CPython 3.12.16, NumPy 2.1.3, Matplotlib 3.9.2, pytest 8.3.3 |
| Independent CSV validation | Full orbit/common time, halved step, analytic accuracy, energy and RK4 convergence all passed |

The educational nominal case used a 400 km circular equatorial orbit, Earth radius 6378.137 km and mu 398600.4418 km^3/s^2. Its analytical speed was 7.668558175407055 km/s and period 5553.624271252228 s. Executed output had maximum sampled position error 0.017185304442568383 m and maximum relative energy drift 3.2163573596144863e-11. Halving the step reduced final position error by a factor of 16.499835899480335. Generated plots were visually inspected. J2 is documented as an extension, not implemented or tested. Numerical error against an ideal two-body solution is not real-world orbit-prediction accuracy.

The original raw job contains private vault passages and remains on the test PC. It is deliberately excluded from this repository and release. The public sanitized record reports observations without publishing the vault. Logs are not independent third-party certification. Account switching was not simulated by changing the user's login; ASTRA reads the native Codex login before requests and stores no fixed account credentials. OpenRouter free-model full-project/media equivalence is not claimed. Its current quotas and capacity apply.

Procedural lessons and worker recovery were tested. Model-weight retraining and unrestricted executable self-modification are not implemented. Arbitrary binary formats cannot be interpreted solely because they can be attached; unsupported types are identified rather than fabricated.
