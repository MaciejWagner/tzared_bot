# Training Cycle Configuration

**Utworzono:** 2025-12-15 23:25
**Cel:** 10 cykli ewolucji i treningu

---

## Parametry cyklu

### Ewolucja
```
--population 40
--elite 0
--mutated-copies 4
--forced-parent 0
--forced-crossover-count 10
--random-ratio 0.15
--top 10
```

**Wynik:**
- 4 mutacje lidera
- 10 crossoverów lider × top10
- 20 crossoverów top10 × top10
- 6 random

### Trening
```
-MapPaths 'training_maps/training-0b.tzared'
-Duration 40
-TrialsPerNetwork 5
-ParallelSessions 3
-StaggerDelaySeconds 4
```

---

## Warunki zakończenia treningu

1. Wszystkie sieci przetrenowane (200 triali)
2. **LUB** 80% sieci ma VICTORY (32+ z 40)

---

## Aktualny postęp

| Cykl | Generacja | Status | Leader | Best Fitness | Victories |
|------|-----------|--------|--------|--------------|-----------|
| 0 | 10 | IN PROGRESS | network_00 | - | - |
| 1 | 11 | PENDING | - | - | - |
| 2 | 12 | PENDING | - | - | - |
| 3 | 13 | PENDING | - | - | - |
| 4 | 14 | PENDING | - | - | - |
| 5 | 15 | PENDING | - | - | - |
| 6 | 16 | PENDING | - | - | - |
| 7 | 17 | PENDING | - | - | - |
| 8 | 18 | PENDING | - | - | - |
| 9 | 19 | PENDING | - | - | - |
| 10 | 20 | PENDING | - | - | - |

---

## Komendy do wznowienia

### Sprawdź status treningu
```powershell
ls training/generation_N/results/*.json | Measure-Object
```

### Uruchom ewolucję
```powershell
./publish/EvolveGeneration/EvolveGeneration.exe `
    training/generation_N `
    training/generation_N/results/summary.json `
    training/generation_N+1 `
    --population 40 --elite 0 --mutated-copies 4 `
    --forced-parent 0 --forced-crossover-count 10 `
    --random-ratio 0.15 --top 10
```

### Uruchom trening
```powershell
powershell.exe -ExecutionPolicy Bypass -Command `
    "./scripts/train_generation_staggered.ps1 `
    -GenerationPath 'training/generation_N' `
    -MapPaths 'training_maps/training-0b.tzared' `
    -Duration 40 -TrialsPerNetwork 5 `
    -ParallelSessions 3 -StaggerDelaySeconds 4"
```

### Usuń poprzednią generację
```powershell
rm -r training/generation_N-1
```

---

*Ostatnia aktualizacja: 2025-12-15 23:25*
