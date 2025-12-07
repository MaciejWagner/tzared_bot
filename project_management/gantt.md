# TzarBot - Wykres Gantta

**Ostatnia aktualizacja:** 2025-12-07
**Aktualny status:** Faza 1 UKONCZONA, Fazy 0 i 2-6 oczekujace

---

## Diagram Gantta

```mermaid
gantt
    title TzarBot - Harmonogram Projektu
    dateFormat  YYYY-MM-DD
    excludes    weekends

    section Faza 0 Prerequisites
    F0.T1 Host Machine Setup       :f0t1, 2025-12-09, 1d
    F0.T2 Development VM Setup     :f0t2, after f0t1, 2d
    F0.T3 Tzar Game Installation   :f0t3, after f0t2, 1d
    F0.T4 Environment Verification :f0t4, after f0t3, 1d
    Milestone: F0 Complete         :milestone, m0, after f0t4, 0d

    section Faza 1 Game Interface
    F1.T1 Project Setup            :done, f1t1, 2025-12-07, 1d
    F1.T2 Screen Capture           :done, f1t2, 2025-12-07, 1d
    F1.T3 Input Injection          :done, f1t3, 2025-12-07, 1d
    F1.T4 IPC Named Pipes          :done, f1t4, 2025-12-07, 1d
    F1.T5 Window Detection         :done, f1t5, 2025-12-07, 1d
    F1.T6 Integration Tests        :done, f1t6, 2025-12-07, 1d
    Milestone: F1 Complete         :milestone, done, m1, 2025-12-07, 0d

    section Faza 2 Neural Network
    F2.T1 NetworkGenome            :f2t1, after m0, 2d
    F2.T2 Image Preprocessor       :f2t2, after f2t1, 2d
    F2.T3 ONNX Network Builder     :f2t3, after f2t1, 3d
    F2.T4 Inference Engine         :f2t4, after f2t2 f2t3, 2d
    F2.T5 Integration Tests F2     :f2t5, after f2t4, 1d
    Milestone: F2 Complete         :milestone, m2, after f2t5, 0d

    section Faza 3 Genetic Algorithm
    F3.T1 GA Engine Core           :f3t1, after m2, 2d
    F3.T2 Mutation Operators       :f3t2, after f3t1, 2d
    F3.T3 Crossover Operators      :f3t3, after f3t1, 2d
    F3.T4 Selection & Elitism      :f3t4, after f3t1, 1d
    F3.T5 Fitness & Persistence    :f3t5, after f3t2 f3t3 f3t4, 2d
    Milestone: F3 Complete         :milestone, m3, after f3t5, 0d

    section Faza 4 Hyper-V Infrastructure
    F4.T1 Template VM (MANUAL)     :crit, f4t1, after m0, 5d
    F4.T2 VM Cloning Scripts       :f4t2, after f4t1, 2d
    F4.T3 VM Manager               :f4t3, after f4t2, 3d
    F4.T4 Orchestrator Service     :f4t4, after f4t3, 3d
    F4.T5 Communication Protocol   :f4t5, after f4t3, 2d
    F4.T6 Multi-VM Integration     :f4t6, after f4t4 f4t5, 2d
    Milestone: F4 Complete         :milestone, m4, after f4t6, 0d

    section Faza 5 Game State Detection
    F5.T1 Template Capture Tool    :f5t1, after m0, 1d
    F5.T2 GameStateDetector        :f5t2, after f5t1, 3d
    F5.T3 GameMonitor              :f5t3, after f5t2, 2d
    F5.T4 Stats Extraction OCR     :f5t4, after f5t2, 2d
    Milestone: F5 Complete         :milestone, m5, after f5t3 f5t4, 0d

    section Faza 6 Training Pipeline
    F6.T1 Training Loop Core       :f6t1, after m3 m4 m5, 3d
    F6.T2 Curriculum Manager       :f6t2, after f6t1, 2d
    F6.T3 Checkpoint Manager       :f6t3, after f6t1, 2d
    F6.T4 Tournament System        :f6t4, after f6t2, 2d
    F6.T5 Blazor Dashboard         :f6t5, after f6t1, 5d
    F6.T6 Full Integration         :f6t6, after f6t2 f6t3 f6t4 f6t5, 3d
    Milestone: F6 Complete         :milestone, m6, after f6t6, 0d

    section Training
    First Training Run             :train, after m6, 14d
    Bot vs Easy AI                 :milestone, m7, after train, 0d
```

---

## Sciezka krytyczna

```
F0.T1 -> F0.T2 -> F0.T3 -> F0.T4 -> F4.T1 -> F4.T2 -> F4.T3 -> F4.T4 -> F4.T6 -> F6.T1 -> F6.T6
```

**Uwaga:** Faza 4 (Hyper-V Infrastructure) jest na sciezce krytycznej ze wzgledu na:
1. F4.T1 (Template VM) - zadanie manualne, czasochlonne
2. F4.T4 (Orchestrator) - zlozonosc integracji

---

## Postep wedlug faz

| Faza | Nazwa | Taski | Ukonczonych | Postep | Status |
|------|-------|-------|-------------|--------|--------|
| 0 | Prerequisites | 4 | 0 | 0% | PENDING |
| 1 | Game Interface | 6 | 6 | 100% | COMPLETED |
| 2 | Neural Network | 5 | 0 | 0% | PENDING |
| 3 | Genetic Algorithm | 5 | 0 | 0% | PENDING |
| 4 | Hyper-V Infrastructure | 6 | 0 | 0% | PENDING |
| 5 | Game State Detection | 4 | 0 | 0% | PENDING |
| 6 | Training Pipeline | 6 | 0 | 0% | PENDING |
| **TOTAL** | | **36** | **6** | **17%** | IN PROGRESS |

---

## Mozliwosc pracy rownoleglej

Po ukonczeniu Fazy 0 (Prerequisites), nastepujace fazy moga byc realizowane rownolegle:
- **Faza 2** (Neural Network)
- **Faza 4** (Hyper-V Infrastructure)
- **Faza 5** (Game State Detection)

```
              ┌── Faza 2 ──┐
              │            │
Faza 0 ──────┼── Faza 4 ──┼── Faza 3 ── Faza 6
              │            │
              └── Faza 5 ──┘
```

---

## Szacowany czas realizacji

| Element | Czas (dni roboczych) |
|---------|---------------------|
| Faza 0 | 5 dni |
| Faza 1 | 1 dzien (UKONCZONE) |
| Faza 2 | 8 dni |
| Faza 3 | 7 dni |
| Faza 4 | 15 dni (sciezka krytyczna) |
| Faza 5 | 6 dni |
| Faza 6 | 12 dni |
| Pierwszy trening | 14 dni |
| **TOTAL** | ~50 dni roboczych |

**Uwaga:** Szacunki zakladaja prace jednego developera. Przy pracy rownoleglej na Fazach 2, 4, 5 czas moze byc skrocony.

---

## Historia aktualizacji

| Data | Zmiana |
|------|--------|
| 2025-12-07 | Utworzenie wykresu, Faza 1 ukonczona |
