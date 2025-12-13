# Generation 0 Training Analysis - COMPLETE

**Date:** 2025-12-13
**Networks Tested:** 20 (network_00 to network_19)
**Test Duration:** 60 seconds per network
**Map:** training-0.tzared (Basic Survival)
**Status:** COMPLETED

## Summary Results - All Networks

| Network ID | Outcome | Duration (s) | Actions | APS | Avg Inference (ms) |
|------------|---------|--------------|---------|-----|-------------------|
| 00 | TIMEOUT | 60.66 | 32 | 0.53 | 19.7 |
| 01 | TIMEOUT | 61.41 | 0 | 0.00 | 0.0 |
| 02 | TIMEOUT | 60.90 | 3 | 0.05 | 96.1 |
| 03 | TIMEOUT | 60.92 | 9 | 0.15 | 36.0 |
| 04 | TIMEOUT | 60.63 | 15 | 0.25 | 61.2 |
| 05 | TIMEOUT | 60.86 | 36 | 0.59 | 13.8 |
| 06 | TIMEOUT | 60.45 | 38 | 0.63 | 16.6 |
| 07 | TIMEOUT | 62.82 | 15 | 0.24 | 50.8 |
| 08 | TIMEOUT | 60.09 | 15 | 0.25 | 30.7 |
| 09 | TIMEOUT | 60.35 | 33 | 0.55 | 12.5 |
| 10 | TIMEOUT | 60.17 | 36 | 0.60 | 13.5 |
| 11 | TIMEOUT | 60.24 | 36 | 0.60 | 18.8 |
| 12 | TIMEOUT | 60.06 | 41 | 0.68 | 11.0 |
| 13 | TIMEOUT | 60.40 | 42 | 0.70 | 13.2 |
| 14 | TIMEOUT | 60.63 | 42 | 0.69 | 18.2 |
| 15 | TIMEOUT | 60.49 | 42 | 0.69 | 10.7 |
| 16 | TIMEOUT | 60.60 | 43 | 0.71 | 13.8 |
| 17 | TIMEOUT | 60.69 | 42 | 0.69 | 14.8 |
| 18 | TIMEOUT | 60.73 | 44 | 0.72 | 13.2 |
| 19 | TIMEOUT | 60.68 | 42 | 0.69 | 11.3 |

## Fitness Ranking (by Actions)

| Rank | Network | Actions | APS | Inference (ms) | Selection |
|------|---------|---------|-----|----------------|-----------|
| 1 | **18** | 44 | 0.72 | 13.2 | TOP 10 |
| 2 | **16** | 43 | 0.71 | 13.8 | TOP 10 |
| 3 | **13** | 42 | 0.70 | 13.2 | TOP 10 |
| 4 | **14** | 42 | 0.69 | 18.2 | TOP 10 |
| 5 | **15** | 42 | 0.69 | 10.7 | TOP 10 |
| 6 | **17** | 42 | 0.69 | 14.8 | TOP 10 |
| 7 | **19** | 42 | 0.69 | 11.3 | TOP 10 |
| 8 | **12** | 41 | 0.68 | 11.0 | TOP 10 |
| 9 | **06** | 38 | 0.63 | 16.6 | TOP 10 |
| 10 | **05** | 36 | 0.59 | 13.8 | TOP 10 |
| 11 | 10 | 36 | 0.60 | 13.5 | ELIMINATED |
| 12 | 11 | 36 | 0.60 | 18.8 | ELIMINATED |
| 13 | 09 | 33 | 0.55 | 12.5 | ELIMINATED |
| 14 | 00 | 32 | 0.53 | 19.7 | ELIMINATED |
| 15 | 04 | 15 | 0.25 | 61.2 | ELIMINATED |
| 16 | 07 | 15 | 0.24 | 50.8 | ELIMINATED |
| 17 | 08 | 15 | 0.25 | 30.7 | ELIMINATED |
| 18 | 03 | 9 | 0.15 | 36.0 | ELIMINATED |
| 19 | 02 | 3 | 0.05 | 96.1 | ELIMINATED |
| 20 | 01 | 0 | 0.00 | 0.0 | ELIMINATED |

## Statistics

| Metric | Value |
|--------|-------|
| Best Network | #18 (44 actions) |
| Worst Network | #01 (0 actions) |
| Average Actions | 28.0 |
| Average APS | 0.46 |
| Average Inference | 27.8 ms |
| Min Inference | 10.7 ms (#15) |
| Max Inference | 96.1 ms (#02) |

## Key Findings

### 1. Architecture Performance

Networks with lower inference times performed better:
- **Best performers (10-15ms inference)**: networks 12, 15, 18, 19
- **Worst performers (>30ms inference)**: networks 01, 02, 03, 04, 07, 08

### 2. Clear Performance Clusters

**Group A - Efficient (40+ actions):**
- Networks: 12, 13, 14, 15, 16, 17, 18, 19
- Avg inference: ~13ms
- APS: 0.68-0.72

**Group B - Standard (32-38 actions):**
- Networks: 00, 05, 06, 09, 10, 11
- Avg inference: ~15ms
- APS: 0.53-0.63

**Group C - Slow (0-15 actions):**
- Networks: 01, 02, 03, 04, 07, 08
- Avg inference: >30ms
- APS: 0.00-0.25

### 3. Inference Time Correlation

Strong negative correlation between inference time and actions:
- Networks with <20ms inference: avg 40 actions
- Networks with >30ms inference: avg 10 actions

## Selection for Generation 1

**Top 10 selected networks (50% selection rate):**

1. network_18 (44 actions)
2. network_16 (43 actions)
3. network_13 (42 actions)
4. network_14 (42 actions)
5. network_15 (42 actions)
6. network_17 (42 actions)
7. network_19 (42 actions)
8. network_12 (41 actions)
9. network_06 (38 actions)
10. network_05 (36 actions)

## Recommendations for Generation 1

1. **Crossover pairs:**
   - 18 × 16 (best performers)
   - 13 × 15 (fast inference)
   - 12 × 19 (consistent performance)
   - 06 × 05 (diversity)

2. **Mutation rates:**
   - Keep low mutation (5-10%) for top performers
   - Higher mutation (20-30%) for diversity

3. **Elitism:**
   - Keep network_18 unchanged (best performer)
   - Keep network_15 unchanged (fastest inference)

## Interactive Session Test (Edge Browser)

**Date:** 2025-12-13
**Browser:** Microsoft Edge (msedge) - visible window
**Session:** Interactive (Session 1)

### Results Comparison

| NetworkId | Session 0 Actions | Interactive Actions | Session 0 Inference | Interactive Inference |
|-----------|-------------------|---------------------|---------------------|----------------------|
| 12 | 41 | 3 | 11.0 ms | 346.0 ms |
| 13 | 42 | 5 | 13.2 ms | 223.9 ms |
| 14 | 42 | 4 | 18.2 ms | 255.5 ms |
| 15 | 42 | 4 | 10.7 ms | 175.5 ms |
| 16 | 43 | 5 | 13.8 ms | 233.6 ms |
| 17 | 42 | 3 | 14.8 ms | 434.5 ms |
| 18 | 44 | 3 | 13.2 ms | 312.4 ms |
| 19 | 42 | 1 | 11.3 ms | 494.2 ms |

### Key Finding

**Performance degradation in interactive session:**
- Session 0 (headless) average inference: **13 ms**
- Interactive session average inference: **310 ms**
- **~24x slowdown** when browser is visible

**Cause:** Visible browser window causes additional GPU/CPU overhead for rendering.

**Recommendation:** Use Session 0 (PowerShell Direct) for training, interactive session only for debugging/observation.

## Next Steps

1. ✅ All 20 networks evaluated
2. ✅ Top 10 selected
3. ✅ Interactive session test completed (Edge works, but slower)
4. ⏳ Generate Generation 1 (crossover + mutation)
5. ⏳ Run Generation 1 training
6. ⏳ Compare performance across generations
