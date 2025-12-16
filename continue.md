# TzarBot Workflow Continuation Report

**Ostatnia aktualizacja:** 2025-12-16
**Status:** OCZEKUJE NA TRENING (Generation 12)

---

## Aktualny stan

| Parametr | Wartość |
|----------|---------|
| Aktualna generacja | 12 |
| Status | Gotowa do treningu |
| Mapa | training-0b.tzared |
| Populacja | 40 sieci |

---

## Komenda do uruchomienia treningu

```powershell
powershell.exe -ExecutionPolicy Bypass -File "C:\Users\maciek\ai_experiments\tzar_bot\scripts\train_generation_staggered.ps1" -GenerationPath "training/generation_12" -MapPaths "training_maps/training-0b.tzared" -Duration 40 -TrialsPerNetwork 5 -ParallelSessions 3 -StaggerDelaySeconds 4
```

---

## Historia ostatnich generacji

| Gen | Victory Rate | Trend | Najszybszy | Lider |
|-----|--------------|-------|------------|-------|
| 10 | 32.5% (13/40) | - | 13.3s | network_37 |
| 11 | 40.0% (16/40) | +7.5% | 13.4s | network_03 |
| 12 | ? | ? | ? | ? |

---

## Po zakończeniu treningu gen12

1. Sprawdź wyniki:
```powershell
Get-Content training/generation_12/results/summary.json | ConvertFrom-Json | Sort-Object -Property Fitness -Descending | Format-Table -AutoSize
```

2. Ewoluuj do gen13:
```powershell
./publish/EvolveGeneration/EvolveGeneration.exe `
    training/generation_12 `
    training/generation_12/results/summary.json `
    training/generation_13 `
    --population 40 `
    --elite 0 `
    --mutated-copies 4 `
    --forced-parent 0 `
    --forced-crossover-count 10 `
    --random-ratio 0.15 `
    --top 10
```

3. Usuń starą generację:
```powershell
Remove-Item -Recurse -Force training/generation_12
```

4. Zaktualizuj `training/evolution_log.md`

---

## Parametry ewolucji

```
--population 40
--elite 0
--mutated-copies 4
--forced-parent 0 (best network)
--forced-crossover-count 10
--random-ratio 0.15
--top 10
```

**Kryterium zmiany mapy:** 80% sieci z VICTORY -> przejście na trudniejszą mapę (training-0.tzared)

---

## Pliki do aktualizacji

- `training/evolution_log.md` - po każdym cyklu
- `continue.md` - po każdej zmianie stanu

---

*Raport zaktualizowany: 2025-12-16*
