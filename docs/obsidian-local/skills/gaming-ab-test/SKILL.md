---
name: gaming-ab-test
description: Design a repeatable game-settings experiment without inventing measurements or inferring unsupported performance metrics.
---

# Gaming A/B Test

## Method
1. Record game, patch, hardware, driver, resolution, scene, duration and the player's goal.
2. Define two conditions differing in exactly one setting.
3. Specify warm-up, three or more paired repeats, run order and the metrics that can actually be collected.
4. Provide a blank result table, a decision rule chosen before testing and likely confounders.
5. Keep missing values explicitly marked `unmeasured`.

## Output
Return `objective`, `fixed controls`, `changed variable`, `procedure`, `worksheet`, `decision rule`, `confounders` and `limitations`.

## Safety
Never invent FPS, frame time, latency, temperature, win rate or benchmark results. CPU/GPU utilization and RAM do not establish those measurements.
