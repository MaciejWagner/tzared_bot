# TzarBot Evolution Log

**Utworzono:** 2025-12-16
**Cel:** Ewolucja sieci neuronowych do gry Tzar

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

**Struktura populacji:**
- 4 mutacje najlepszej sieci (sigma=0.25)
- 10 forced crossovers (best x top10)
- 20 normal crossovers (top10 x top10)
- 6 random networks (nowe architektury)

---

## Parametry treningu

| Parametr | Wartość |
|----------|---------|
| Duration | 40s |
| TrialsPerNetwork | 5 |
| ParallelSessions | 3 |
| StaggerDelaySeconds | 4 |

---

## Kryterium sukcesu

- **Victory:** Fitness = 1.0
- **Timeout:** Fitness = 0.3
- **Defeat:** Fitness = 0.0
- **Formuła:** `(victories * 1.0 + timeouts * 0.3) / trials`
- **Próg zmiany mapy:** 80% sieci z VICTORY -> trudniejsza mapa

---

## Historia generacji

### Generation 10
| Parametr | Wartość |
|----------|---------|
| Data | 2025-12-15 |
| Mapa | training-0b.tzared |
| Czas treningu | 46m 46s |
| Victory rate | 32.5% (13/40) |

**Top 5:**
| Network | V | D | T | Fitness | AvgDur | AvgAct |
|---------|---|---|---|---------|--------|--------|
| network_10 | 5 | 0 | 0 | 1.0 | 13.3s | 23 |
| network_16 | 5 | 0 | 0 | 1.0 | 13.6s | 24 |
| network_07 | 5 | 0 | 0 | 1.0 | 14.2s | 25 |
| network_28 | 5 | 0 | 0 | 1.0 | 13.6s | 24 |
| network_30 | 5 | 0 | 0 | 1.0 | 13.5s | 24 |

**Ewolucja -> Gen11:**
- Lider: network_37 (Fitness 1.0, 42 actions)
- Crossovers: częściowo niekompatybilne struktury (3 vs 4 layers)

---

### Generation 11
| Parametr | Wartość |
|----------|---------|
| Data | 2025-12-16 |
| Mapa | training-0b.tzared |
| Victory rate | 40% (16/40) |

**Top 5:**
| Network | V | D | T | Fitness | AvgDur | AvgAct |
|---------|---|---|---|---------|--------|--------|
| network_18 | 5 | 0 | 0 | 1.0 | 13.4s | 24 |
| network_19 | 5 | 0 | 0 | 1.0 | 13.4s | 24 |
| network_25 | 5 | 0 | 0 | 1.0 | 13.4s | 24 |
| network_26 | 5 | 0 | 0 | 1.0 | 13.4s | 24 |
| network_14 | 5 | 0 | 0 | 1.0 | 13.5s | 24 |

**Ewolucja -> Gen12:**
- Lider: network_03 (Fitness 1.0, 42 actions)
- Top 10: network_03, 15, 16, 04, 10, 33, 17, 24, 14, 18
- Crossovers: głównie 3-layer networks

---

### Generation 12
| Parametr | Wartość |
|----------|---------|
| Data | 2025-12-16 |
| Mapa | training-0b.tzared |
| Status | OCZEKUJE NA TRENING |

**Skład populacji:**
- 4 mutacje network_03
- 10 forced crossovers (network_03 x top10)
- 20 normal crossovers
- 6 random (w tym większe architektury: 1024-512-256-128)

---

## Podsumowanie postępu

| Gen | Victory Rate | Trend | Najszybszy czas |
|-----|--------------|-------|-----------------|
| 10 | 32.5% | - | 13.3s |
| 11 | 40.0% | +7.5% | 13.4s |
| 12 | ? | ? | ? |

---

*Ostatnia aktualizacja: 2025-12-16*
