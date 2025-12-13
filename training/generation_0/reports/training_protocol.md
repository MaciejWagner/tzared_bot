# TzarBot Training Protocol - Generation 0

**Created:** 2025-12-13 13:02:21 UTC
**Population Size:** 20

## Protocol Parameters

| Parameter | Value |
|-----------|-------|
| Models | 20 |
| Trials per Model | 10 |
| Total Games | 200 |
| Success Criterion | Win game OR score > threshold |
| Selection | Top 50% pass to next generation |
| Reproduction | Winners produce offspring via crossover + mutation |

## Evaluation Phases

### Phase 1: Basic Survival
- **Map:** training-0.tzared
- **Objective:** Survive as long as possible
- **Duration:** 5 minutes per game
- **Metrics:**
  - Survival time
  - Resources gathered
  - Buildings constructed

### Phase 2: Unit Production
- **Map:** training-1.tzared
- **Objective:** Build and manage units
- **Duration:** 10 minutes per game
- **Metrics:**
  - Units produced
  - Actions per minute (APM)
  - Resource efficiency

### Phase 3: Combat
- **Map:** training-2.tzared
- **Objective:** Defeat AI opponent
- **Duration:** 15 minutes per game
- **Metrics:**
  - Enemy units killed
  - Own units lost
  - Victory/Defeat outcome

## Trial Log Template

Use this template to log each trial:

```markdown
## Trial [TRIAL_NUMBER] - Network [NETWORK_ID]

| Field | Value |
|-------|-------|
| Date/Time | [TIMESTAMP] |
| Network ID | [ID] |
| Phase | [PHASE_NUMBER] |
| Map | [MAP_NAME] |
| Duration | [SECONDS] |
| Outcome | [WIN/LOSS/TIMEOUT] |
| Resources Gathered | [NUMBER] |
| Units Built | [NUMBER] |
| Units Killed | [NUMBER] |
| Units Lost | [NUMBER] |
| APM | [NUMBER] |

### Notes
[Observations about network behavior]
```

## Generation Summary Template

```markdown
## Generation [N] Summary

| Network | Trials | Wins | Avg Score | Status |
|---------|--------|------|-----------|--------|
| Net 00 | 10 | 3 | 45.2 | PASS |
| Net 01 | 10 | 1 | 22.5 | FAIL |
| ... | ... | ... | ... | ... |

### Selection Results
- Passed: [LIST OF IDs]
- Failed: [LIST OF IDs]

### Next Generation
- Parents: [LIST]
- Offspring created: [NUMBER]
- Mutations applied: [DESCRIPTION]
```

## Network-by-Network Breakdown

### Network 00 (`26813982`)

| Trial | Phase | Duration | Outcome | Score | Notes |
|-------|-------|----------|---------|-------|-------|
| 1 | - | - | - | - | - |
| 2 | - | - | - | - | - |
| 3 | - | - | - | - | - |
| 4 | - | - | - | - | - |
| 5 | - | - | - | - | - |
| 6 | - | - | - | - | - |
| 7 | - | - | - | - | - |
| 8 | - | - | - | - | - |
| 9 | - | - | - | - | - |
| 10 | - | - | - | - | - |

**Summary:** 0/10 wins, Avg Score: 0.0, Status: PENDING

### Network 01 (`d1f093a6`)

| Trial | Phase | Duration | Outcome | Score | Notes |
|-------|-------|----------|---------|-------|-------|
| 1 | - | - | - | - | - |
| 2 | - | - | - | - | - |
| 3 | - | - | - | - | - |
| 4 | - | - | - | - | - |
| 5 | - | - | - | - | - |
| 6 | - | - | - | - | - |
| 7 | - | - | - | - | - |
| 8 | - | - | - | - | - |
| 9 | - | - | - | - | - |
| 10 | - | - | - | - | - |

**Summary:** 0/10 wins, Avg Score: 0.0, Status: PENDING

### Network 02 (`5b351554`)

| Trial | Phase | Duration | Outcome | Score | Notes |
|-------|-------|----------|---------|-------|-------|
| 1 | - | - | - | - | - |
| 2 | - | - | - | - | - |
| 3 | - | - | - | - | - |
| 4 | - | - | - | - | - |
| 5 | - | - | - | - | - |
| 6 | - | - | - | - | - |
| 7 | - | - | - | - | - |
| 8 | - | - | - | - | - |
| 9 | - | - | - | - | - |
| 10 | - | - | - | - | - |

**Summary:** 0/10 wins, Avg Score: 0.0, Status: PENDING

### Network 03 (`4f3e6eab`)

| Trial | Phase | Duration | Outcome | Score | Notes |
|-------|-------|----------|---------|-------|-------|
| 1 | - | - | - | - | - |
| 2 | - | - | - | - | - |
| 3 | - | - | - | - | - |
| 4 | - | - | - | - | - |
| 5 | - | - | - | - | - |
| 6 | - | - | - | - | - |
| 7 | - | - | - | - | - |
| 8 | - | - | - | - | - |
| 9 | - | - | - | - | - |
| 10 | - | - | - | - | - |

**Summary:** 0/10 wins, Avg Score: 0.0, Status: PENDING

### Network 04 (`36372fe5`)

| Trial | Phase | Duration | Outcome | Score | Notes |
|-------|-------|----------|---------|-------|-------|
| 1 | - | - | - | - | - |
| 2 | - | - | - | - | - |
| 3 | - | - | - | - | - |
| 4 | - | - | - | - | - |
| 5 | - | - | - | - | - |
| 6 | - | - | - | - | - |
| 7 | - | - | - | - | - |
| 8 | - | - | - | - | - |
| 9 | - | - | - | - | - |
| 10 | - | - | - | - | - |

**Summary:** 0/10 wins, Avg Score: 0.0, Status: PENDING

### Network 05 (`1067b352`)

| Trial | Phase | Duration | Outcome | Score | Notes |
|-------|-------|----------|---------|-------|-------|
| 1 | - | - | - | - | - |
| 2 | - | - | - | - | - |
| 3 | - | - | - | - | - |
| 4 | - | - | - | - | - |
| 5 | - | - | - | - | - |
| 6 | - | - | - | - | - |
| 7 | - | - | - | - | - |
| 8 | - | - | - | - | - |
| 9 | - | - | - | - | - |
| 10 | - | - | - | - | - |

**Summary:** 0/10 wins, Avg Score: 0.0, Status: PENDING

### Network 06 (`0b56404c`)

| Trial | Phase | Duration | Outcome | Score | Notes |
|-------|-------|----------|---------|-------|-------|
| 1 | - | - | - | - | - |
| 2 | - | - | - | - | - |
| 3 | - | - | - | - | - |
| 4 | - | - | - | - | - |
| 5 | - | - | - | - | - |
| 6 | - | - | - | - | - |
| 7 | - | - | - | - | - |
| 8 | - | - | - | - | - |
| 9 | - | - | - | - | - |
| 10 | - | - | - | - | - |

**Summary:** 0/10 wins, Avg Score: 0.0, Status: PENDING

### Network 07 (`33d5175f`)

| Trial | Phase | Duration | Outcome | Score | Notes |
|-------|-------|----------|---------|-------|-------|
| 1 | - | - | - | - | - |
| 2 | - | - | - | - | - |
| 3 | - | - | - | - | - |
| 4 | - | - | - | - | - |
| 5 | - | - | - | - | - |
| 6 | - | - | - | - | - |
| 7 | - | - | - | - | - |
| 8 | - | - | - | - | - |
| 9 | - | - | - | - | - |
| 10 | - | - | - | - | - |

**Summary:** 0/10 wins, Avg Score: 0.0, Status: PENDING

### Network 08 (`54318206`)

| Trial | Phase | Duration | Outcome | Score | Notes |
|-------|-------|----------|---------|-------|-------|
| 1 | - | - | - | - | - |
| 2 | - | - | - | - | - |
| 3 | - | - | - | - | - |
| 4 | - | - | - | - | - |
| 5 | - | - | - | - | - |
| 6 | - | - | - | - | - |
| 7 | - | - | - | - | - |
| 8 | - | - | - | - | - |
| 9 | - | - | - | - | - |
| 10 | - | - | - | - | - |

**Summary:** 0/10 wins, Avg Score: 0.0, Status: PENDING

### Network 09 (`77790ca6`)

| Trial | Phase | Duration | Outcome | Score | Notes |
|-------|-------|----------|---------|-------|-------|
| 1 | - | - | - | - | - |
| 2 | - | - | - | - | - |
| 3 | - | - | - | - | - |
| 4 | - | - | - | - | - |
| 5 | - | - | - | - | - |
| 6 | - | - | - | - | - |
| 7 | - | - | - | - | - |
| 8 | - | - | - | - | - |
| 9 | - | - | - | - | - |
| 10 | - | - | - | - | - |

**Summary:** 0/10 wins, Avg Score: 0.0, Status: PENDING

### Network 10 (`b4365ab5`)

| Trial | Phase | Duration | Outcome | Score | Notes |
|-------|-------|----------|---------|-------|-------|
| 1 | - | - | - | - | - |
| 2 | - | - | - | - | - |
| 3 | - | - | - | - | - |
| 4 | - | - | - | - | - |
| 5 | - | - | - | - | - |
| 6 | - | - | - | - | - |
| 7 | - | - | - | - | - |
| 8 | - | - | - | - | - |
| 9 | - | - | - | - | - |
| 10 | - | - | - | - | - |

**Summary:** 0/10 wins, Avg Score: 0.0, Status: PENDING

### Network 11 (`2128fd63`)

| Trial | Phase | Duration | Outcome | Score | Notes |
|-------|-------|----------|---------|-------|-------|
| 1 | - | - | - | - | - |
| 2 | - | - | - | - | - |
| 3 | - | - | - | - | - |
| 4 | - | - | - | - | - |
| 5 | - | - | - | - | - |
| 6 | - | - | - | - | - |
| 7 | - | - | - | - | - |
| 8 | - | - | - | - | - |
| 9 | - | - | - | - | - |
| 10 | - | - | - | - | - |

**Summary:** 0/10 wins, Avg Score: 0.0, Status: PENDING

### Network 12 (`a7daab45`)

| Trial | Phase | Duration | Outcome | Score | Notes |
|-------|-------|----------|---------|-------|-------|
| 1 | - | - | - | - | - |
| 2 | - | - | - | - | - |
| 3 | - | - | - | - | - |
| 4 | - | - | - | - | - |
| 5 | - | - | - | - | - |
| 6 | - | - | - | - | - |
| 7 | - | - | - | - | - |
| 8 | - | - | - | - | - |
| 9 | - | - | - | - | - |
| 10 | - | - | - | - | - |

**Summary:** 0/10 wins, Avg Score: 0.0, Status: PENDING

### Network 13 (`5c360a8f`)

| Trial | Phase | Duration | Outcome | Score | Notes |
|-------|-------|----------|---------|-------|-------|
| 1 | - | - | - | - | - |
| 2 | - | - | - | - | - |
| 3 | - | - | - | - | - |
| 4 | - | - | - | - | - |
| 5 | - | - | - | - | - |
| 6 | - | - | - | - | - |
| 7 | - | - | - | - | - |
| 8 | - | - | - | - | - |
| 9 | - | - | - | - | - |
| 10 | - | - | - | - | - |

**Summary:** 0/10 wins, Avg Score: 0.0, Status: PENDING

### Network 14 (`1daf16f6`)

| Trial | Phase | Duration | Outcome | Score | Notes |
|-------|-------|----------|---------|-------|-------|
| 1 | - | - | - | - | - |
| 2 | - | - | - | - | - |
| 3 | - | - | - | - | - |
| 4 | - | - | - | - | - |
| 5 | - | - | - | - | - |
| 6 | - | - | - | - | - |
| 7 | - | - | - | - | - |
| 8 | - | - | - | - | - |
| 9 | - | - | - | - | - |
| 10 | - | - | - | - | - |

**Summary:** 0/10 wins, Avg Score: 0.0, Status: PENDING

### Network 15 (`81f0be64`)

| Trial | Phase | Duration | Outcome | Score | Notes |
|-------|-------|----------|---------|-------|-------|
| 1 | - | - | - | - | - |
| 2 | - | - | - | - | - |
| 3 | - | - | - | - | - |
| 4 | - | - | - | - | - |
| 5 | - | - | - | - | - |
| 6 | - | - | - | - | - |
| 7 | - | - | - | - | - |
| 8 | - | - | - | - | - |
| 9 | - | - | - | - | - |
| 10 | - | - | - | - | - |

**Summary:** 0/10 wins, Avg Score: 0.0, Status: PENDING

### Network 16 (`e71cf521`)

| Trial | Phase | Duration | Outcome | Score | Notes |
|-------|-------|----------|---------|-------|-------|
| 1 | - | - | - | - | - |
| 2 | - | - | - | - | - |
| 3 | - | - | - | - | - |
| 4 | - | - | - | - | - |
| 5 | - | - | - | - | - |
| 6 | - | - | - | - | - |
| 7 | - | - | - | - | - |
| 8 | - | - | - | - | - |
| 9 | - | - | - | - | - |
| 10 | - | - | - | - | - |

**Summary:** 0/10 wins, Avg Score: 0.0, Status: PENDING

### Network 17 (`fc5153b0`)

| Trial | Phase | Duration | Outcome | Score | Notes |
|-------|-------|----------|---------|-------|-------|
| 1 | - | - | - | - | - |
| 2 | - | - | - | - | - |
| 3 | - | - | - | - | - |
| 4 | - | - | - | - | - |
| 5 | - | - | - | - | - |
| 6 | - | - | - | - | - |
| 7 | - | - | - | - | - |
| 8 | - | - | - | - | - |
| 9 | - | - | - | - | - |
| 10 | - | - | - | - | - |

**Summary:** 0/10 wins, Avg Score: 0.0, Status: PENDING

### Network 18 (`e94aa4eb`)

| Trial | Phase | Duration | Outcome | Score | Notes |
|-------|-------|----------|---------|-------|-------|
| 1 | - | - | - | - | - |
| 2 | - | - | - | - | - |
| 3 | - | - | - | - | - |
| 4 | - | - | - | - | - |
| 5 | - | - | - | - | - |
| 6 | - | - | - | - | - |
| 7 | - | - | - | - | - |
| 8 | - | - | - | - | - |
| 9 | - | - | - | - | - |
| 10 | - | - | - | - | - |

**Summary:** 0/10 wins, Avg Score: 0.0, Status: PENDING

### Network 19 (`bf25bfcf`)

| Trial | Phase | Duration | Outcome | Score | Notes |
|-------|-------|----------|---------|-------|-------|
| 1 | - | - | - | - | - |
| 2 | - | - | - | - | - |
| 3 | - | - | - | - | - |
| 4 | - | - | - | - | - |
| 5 | - | - | - | - | - |
| 6 | - | - | - | - | - |
| 7 | - | - | - | - | - |
| 8 | - | - | - | - | - |
| 9 | - | - | - | - | - |
| 10 | - | - | - | - | - |

**Summary:** 0/10 wins, Avg Score: 0.0, Status: PENDING

