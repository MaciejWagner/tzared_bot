# TzarBot Evolution Log

**Utworzono:** 2025-12-16
**Cel:** Ewolucja sieci neuronowych do gry Tzar

---

## Parametry ewolucji (aktualne od Gen13)

```
--population 50
--elite 10
--mutated-per-elite 2
--random-ratio 0.08
--top 10
```

**Struktura populacji (50):**
- 10 elit (bez zmian)
- 20 mutacji (2 × każda elita)
- 16 crossovers (z puli elity)
- 4 random networks (nowe architektury)

---

## Parametry treningu

**Gen 0-13 (stara konfiguracja):**
| Parametr | Wartość |
|----------|---------|
| Mapa | training-0b.tzared (2 wieśniaków) |
| TrialsPerNetwork | 5 |
| Duration | 40s |

**Gen 14+ (nowa konfiguracja):**
| Parametr | Wartość |
|----------|---------|
| Mapy | training-0-1 do training-0-6 (6 map, 1 wieśniak każda) |
| TrialsPerNetwork | 8 |
| Duration | 40s |

---

## Historia generacji

### Generation 10
| Parametr | Wartość |
|----------|---------|
| Data | 2025-12-15 |
| Mapa | training-0b.tzared |
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

---

### Generation 12
| Parametr | Wartość |
|----------|---------|
| Data | 2025-12-16 |
| Mapa | training-0b.tzared |
| Victory rate | 52% (104/200) |
| Population | 40 |

**Top 5:**
| Network | V | D | T | Fitness | AvgDur | AvgAct |
|---------|---|---|---|---------|--------|--------|
| network_00 | 5 | 0 | 0 | 1 | 17.5s | 31 |
| network_18 | 5 | 0 | 0 | 1 | 23s | 42 |
| network_15 | 5 | 0 | 0 | 1 | 22.7s | 42 |
| network_05 | 5 | 0 | 0 | 1 | 22.7s | 42 |
| network_23 | 5 | 0 | 0 | 1 | 14.1s | 25 |

**Ewolucja -> Gen13:**
- Lider: network_00 (Fitness 1)
- Zmiana: Population 40 -> 50, nowe parametry ewolucji

---

### Generation 13
| Parametr | Wartość |
|----------|---------|
| Data | 2025-12-16 |
| Mapa | training-0b.tzared |
| Victory rate | 58.8% (147/250) |
| Population | 50 |

**Top 5:**
| Network | V | D | T | Fitness | AvgDur | AvgAct |
|---------|---|---|---|---------|--------|--------|
| network_31 | 5 | 0 | 0 | 504.2 | 22.9s | 42 |
| network_08 | 5 | 0 | 0 | 504.2 | 22.6s | 42 |
| network_42 | 5 | 0 | 0 | 504.2 | 22.9s | 42 |
| network_32 | 5 | 0 | 0 | 504.2 | 22.8s | 42 |
| network_34 | 5 | 0 | 0 | 504.2 | 22.7s | 42 |

**Uwagi:**
- 19 sieci z 100% victory rate (5/5)
- 11 sieci z 80% victory rate (4/5)

**Ewolucja -> Gen14:**
- Lider: network_31 (Fitness 504.2)

---

### Generation 14
| Parametr | Wartość |
|----------|---------|
| Data | 2025-12-16 |
| Mapy | training-0-1 do training-0-6 (6 map, 1 wieśniak) |
| Próby/sieć | 8 |
| Population | 50 |
| Status | OCZEKUJE NA TRENING |

**Zmiana konfiguracji:**
- Nowe mapy: 6 wariantów z pojedynczym wieśniakiem (łatwiejsze)
- Zwiększona liczba prób: 8 (było 5)
- Łącznie prób: 400 (50 sieci × 8 prób)

---

## Podsumowanie postępu

| Gen | Mapa | Victory Rate | Trend |
|-----|------|--------------|-------|
| 10 | training-0b | 32.5% | - |
| 11 | training-0b | 40.0% | +7.5% |
| 12 | training-0b | 52.0% | +12% |
| 13 | training-0b | 58.8% | +6.8% |
| 14 | training-0-1..6 | ? | ? |

---

*Ostatnia aktualizacja: 2025-12-16*
