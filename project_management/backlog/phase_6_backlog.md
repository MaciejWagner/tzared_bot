# Backlog Fazy 6: Training Pipeline

**Ostatnia aktualizacja:** 2025-12-07
**Status Fazy:** PENDING
**Priorytet:** MUST (finalna integracja systemu)

---

## Podsumowanie

Faza 6 obejmuje pelny pipeline uczenia od poczatkowych losowych sieci do zaawansowanych graczy. Integruje wszystkie poprzednie fazy w dzialajacy system treningu.

---

## Taski

### F6.T1: Training Loop Core
| Pole | Wartosc |
|------|---------|
| **ID** | F6.T1 |
| **Tytul** | Glowna petla treningowa |
| **Opis** | Implementacja glownej petli: generacja -> ewaluacja na VM -> selekcja -> nowa generacja |
| **Priorytet** | MUST |
| **Szacowany naklad** | L (Large) |
| **Status** | PENDING |
| **Agent** | tzarbot-agent-ai-senior |
| **Zaleznosci** | F3 (GA), F4 (Hyper-V), F5 (State Detection) |

**Kryteria akceptacji:**
- [ ] Integracja GA z Orchestratorem
- [ ] Ewaluacja genomow na VM
- [ ] Zbieranie wynikow i obliczanie fitness
- [ ] Generowanie nowej populacji
- [ ] Logging i metryki

**Powiazane pliki:**
- `plans/phase_6_detailed.md`
- `src/TzarBot.Training/`

---

### F6.T2: Curriculum Manager
| Pole | Wartosc |
|------|---------|
| **ID** | F6.T2 |
| **Tytul** | Zarzadzanie curriculum |
| **Opis** | Implementacja etapow ewolucji (Bootstrap -> Basic -> Combat -> Tournament) |
| **Priorytet** | MUST |
| **Szacowany naklad** | M (Medium) |
| **Status** | PENDING |
| **Agent** | tzarbot-agent-ai-senior |
| **Zaleznosci** | F6.T1 |

**Kryteria akceptacji:**
- [ ] Etap 0: Bootstrap (Passive AI, survival time)
- [ ] Etap 1: Basic (Easy AI, economy based)
- [ ] Etap 2: Combat (Easy -> Normal -> Hard AI)
- [ ] Etap 3: Tournament (self-play)
- [ ] Automatyczne przechodzenie miedzy etapami

**Powiazane pliki:**
- `plans/1general_plan.md` (sekcja 6.1, 6.2)

---

### F6.T3: Checkpoint Manager
| Pole | Wartosc |
|------|---------|
| **ID** | F6.T3 |
| **Tytul** | Zarzadzanie checkpointami |
| **Opis** | Zapisywanie i odtwarzanie stanu treningu |
| **Priorytet** | MUST |
| **Szacowany naklad** | M (Medium) |
| **Status** | PENDING |
| **Agent** | QA_INTEGRATION |
| **Zaleznosci** | F6.T1 |

**Kryteria akceptacji:**
- [ ] Zapis stanu treningu (generacja, populacja, statystyki)
- [ ] Odtwarzanie z checkpointu
- [ ] Auto-checkpoint co N generacji
- [ ] Zachowanie ostatnich 10 checkpointow
- [ ] Backup najlepszego genomu
- [ ] **env_settings.md zaktualizowany** (checkpoint paths, backup locations)

**Powiazane pliki:**
- `plans/1general_plan.md` (sekcja 6.4)
- `env_settings.md`

---

### F6.T4: Tournament System
| Pole | Wartosc |
|------|---------|
| **ID** | F6.T4 |
| **Tytul** | System turniejowy |
| **Opis** | Self-play tournament z ELO rating |
| **Priorytet** | SHOULD |
| **Szacowany naklad** | M (Medium) |
| **Status** | PENDING |
| **Agent** | tzarbot-agent-ai-senior |
| **Zaleznosci** | F6.T1, F6.T2 |

**Kryteria akceptacji:**
- [ ] Swiss-system pairing
- [ ] ELO rating calculation
- [ ] Round-robin dla malych populacji
- [ ] Fitness = ELO w etapie turniejowym

**Powiazane pliki:**
- `plans/1general_plan.md` (sekcja 6.3)

---

### F6.T5: Blazor Dashboard
| Pole | Wartosc |
|------|---------|
| **ID** | F6.T5 |
| **Tytul** | Dashboard monitoringu |
| **Opis** | Blazor Server app do monitorowania treningu w czasie rzeczywistym |
| **Priorytet** | SHOULD |
| **Szacowany naklad** | L (Large) |
| **Status** | PENDING |
| **Agent** | tzarbot-agent-fullstack-blazor |
| **Zaleznosci** | F6.T1, F6.T3 |

**Kryteria akceptacji:**
- [ ] Real-time status (generacja, etap, populacja)
- [ ] Wykres fitness over generations
- [ ] Status VM (aktywne, CPU, RAM)
- [ ] Live feed ostatnich gier
- [ ] SignalR updates
- [ ] **env_settings.md zaktualizowany** (dashboard port, SignalR endpoint)

**Powiazane pliki:**
- `plans/1general_plan.md` (sekcja 6.5)
- `src/TzarBot.Dashboard/`
- `env_settings.md`

---

### F6.T6: Full Integration Test
| Pole | Wartosc |
|------|---------|
| **ID** | F6.T6 |
| **Tytul** | Pelny test integracyjny |
| **Opis** | Weryfikacja dzialania calego systemu end-to-end |
| **Priorytet** | MUST |
| **Szacowany naklad** | L (Large) |
| **Status** | PENDING |
| **Agent** | QA_INTEGRATION |
| **Zaleznosci** | F6.T1, F6.T2, F6.T3, F6.T4, F6.T5 |

**Kryteria akceptacji:**
- [ ] Pelny cykl: losowa populacja -> 10 generacji -> checkpoint
- [ ] Wszystkie komponenty wspóldzialaja
- [ ] Dashboard pokazuje real-time status
- [ ] Checkpoint/restore dziala
- [ ] Fitness rosnie (chociaz minimalnie)

**Powiazane pliki:**
- `tests/TzarBot.Tests/Phase6/`

---

## Metryki Fazy

| Metryka | Wartosc |
|---------|---------|
| Liczba taskow | 6 |
| Ukonczonych | 0 |
| W trakcie | 0 |
| Zablokowanych | 0 |
| Oczekujacych | 6 |
| Postep | 0% |

---

## Zaleznosci

- **Wymaga:** Faza 3 (Genetic Algorithm)
- **Wymaga:** Faza 4 (Hyper-V Infrastructure)
- **Wymaga:** Faza 5 (Game State Detection)

---

## Metryki sukcesu

- [ ] Pipeline dziala nieprzerwanie przez 24h
- [ ] Checkpoint/restore dziala poprawnie
- [ ] Dashboard pokazuje real-time status
- [ ] Fitness rosnie przez pierwsze 100 generacji

---

## Kamienie milowe

| Milestone | Opis | Status |
|-----------|------|--------|
| M5 | Bot wygrywa z Easy AI w >50% gier | PENDING |
| M6 | Bot wygrywa z Hard AI | PENDING |

---

## Demo Requirements

Dokumentacja demo fazy MUSI zawierac:

| Wymaganie | Opis |
|-----------|------|
| Scenariusze testowe | Kroki do wykonania demo |
| **Raport z VM** | Uruchomienie demo na VM DEV z dowodami |
| Screenshoty | Min. 3-5 zrzutow ekranu z VM |
| Logi | Pelny output z konsoli (.log files) |

> **UWAGA:** Demo NIE jest kompletne bez raportu z uruchomienia na maszynie wirtualnej!

---

## Notatki

- F6.T4 (Tournament) i F6.T5 (Dashboard) sa SHOULD - mozna uruchomic trening bez nich
- Rekomendacja: jezeli GA nie przyniesie wynikow po 200 generacjach, rozwazyc RL (PPO/A3C)
- Czas treningu moze byc bardzo dlugi - planowac tygodnie, nie dni
