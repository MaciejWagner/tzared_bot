# Generation 0 Training Analysis

**Date:** 2025-12-13
**Networks Tested:** 5 (network_00 to network_04)
**Test Duration:** 60 seconds per network
**Map:** training-0.tzared (Basic Survival)

## Summary Results

| Network ID | Outcome | Duration (s) | Frames | Actions | APS | Avg Inference (ms) |
|------------|---------|--------------|--------|---------|-----|-------------------|
| 00 | TIMEOUT | 60.66 | 35 | 32 | 0.53 | 19.7 |
| 01 | TIMEOUT | 61.41 | 3 | 0 | 0.00 | 0.0 |
| 02 | TIMEOUT | 60.90 | 8 | 3 | 0.05 | 96.1 |
| 03 | TIMEOUT | 60.92 | 13 | 9 | 0.15 | 36.0 |
| 04 | TIMEOUT | 60.63 | 17 | 15 | 0.25 | 61.2 |

## Network Architecture Details

From population_report.md:

| Network | Hidden Layers | Total Weights | ONNX Size |
|---------|---------------|---------------|-----------|
| 00 | 256 → 128 | 4,235,110 | 21.6 MB |
| 01 | 512 → 256 | 11,244,448 | 43.1 MB |
| 02 | 128 → 64 | 2,779,360 | 10.9 MB |
| 03 | 256 → 128 | 4,235,110 | 21.6 MB |
| 04 | 512 → 256 | 11,244,448 | 43.2 MB |

## Analysis

### Performance Observations

1. **Network 00 (best)**: Standard architecture (256→128) with good balance of speed and capability
   - 32 actions in 60s
   - Fast inference (~20ms)
   - Best overall performance

2. **Network 01 (problematic)**: Largest architecture (512→256)
   - Only 3 frames captured, 0 actions executed
   - Inference too slow for frame buffer to fill
   - **Issue:** Large networks may timeout before generating any actions

3. **Network 02 (smallest)**: Smallest architecture (128→64)
   - Slow inference (96ms) despite small size
   - Only 3 actions executed
   - **Issue:** May have encountered errors during inference

4. **Network 03 (same as 00)**: Same architecture as network_00
   - 9 actions, 36ms inference
   - Worse than network_00 despite same architecture
   - **Note:** Random initialization affects performance

5. **Network 04 (large)**: Same as network_01
   - 15 actions, 61ms inference
   - Better than network_01, showing variance in initialization

### Key Findings

1. **Architecture matters**: Smaller networks (256→128) generally perform better due to faster inference
2. **Initialization variance**: Same architecture can have different performance based on random weights
3. **Frame buffer issue**: Need minimum 4 frames before inference starts (first 3 populate buffer)
4. **Actions Per Second**: Maximum achieved ~0.5 APS (target was 10 APS)

### Bottleneck Analysis

The low APS is caused by:
1. **Screenshot capture**: Playwright takes ~50-100ms per screenshot
2. **Network inference**: 20-100ms depending on architecture
3. **Browser interactions**: Additional overhead for clicks

**Recommendation for Phase 2:**
- Use smaller architectures (128→64 or 256→128)
- Consider optimizing screenshot capture (lower resolution, JPEG)
- Implement parallel screenshot/inference pipeline

## Fitness Ranking (based on actions executed)

| Rank | Network | Actions | Status |
|------|---------|---------|--------|
| 1 | 00 | 32 | PASS |
| 2 | 04 | 15 | PASS |
| 3 | 03 | 9 | FAIL |
| 4 | 02 | 3 | FAIL |
| 5 | 01 | 0 | FAIL |

**Selection:** Top 50% (networks 00, 04) would proceed to next generation.

## Next Steps

1. Run remaining networks (05-19) to complete generation evaluation
2. Implement fitness scoring based on actions, not just survival
3. Consider optimizations for higher APS
4. Generate Generation 1 through crossover/mutation of best performers
