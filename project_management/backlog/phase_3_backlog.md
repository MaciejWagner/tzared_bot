# Backlog Fazy 3: Genetic Algorithm

**Ostatnia aktualizacja:** 2025-12-07
**Status Fazy:** PENDING
**Priorytet:** MUST (core algorytmu uczenia)

---

## Podsumowanie

Faza 3 obejmuje implementacje algorytmu genetycznego ewoluujacego populacje sieci neuronowych poprzez selekcje, krzyzowanie i mutacje.

---

## Taski

### F3.T1: GA Engine Core
| Pole | Wartosc |
|------|---------|
| **ID** | F3.T1 |
| **Tytul** | Rdzen silnika GA |
| **Opis** | Implementacja glownej petli algorytmu genetycznego |
| **Priorytet** | MUST |
| **Szacowany naklad** | M (Medium) |
| **Status** | PENDING |
| **Agent** | tzarbot-agent-dotnet-senior |
| **Zaleznosci** | F2.T1 (NetworkGenome) |

**Kryteria akceptacji:**
- [ ] Inicjalizacja losowej populacji (N=50-200)
- [ ] Petla generacji: ewaluacja -> selekcja -> krzyzowanie -> mutacja
- [ ] Konfigurowalny rozmiar populacji
- [ ] Parallel evaluation support
- [ ] Logging postepow

**Powiazane pliki:**
- `plans/phase_3_detailed.md`
- `plans/1general_plan.md` (sekcja 3.1)

---

### F3.T2: Mutation Operators
| Pole | Wartosc |
|------|---------|
| **ID** | F3.T2 |
| **Tytul** | Operatory mutacji |
| **Opis** | Implementacja mutacji wag i struktury sieci |
| **Priorytet** | MUST |
| **Szacowany naklad** | M (Medium) |
| **Status** | PENDING |
| **Agent** | tzarbot-agent-dotnet-senior |
| **Zaleznosci** | F3.T1 |

**Kryteria akceptacji:**
- [ ] Mutacja wag (Gaussian noise)
- [ ] Mutacja struktury (add/remove layer)
- [ ] Zmiana liczby neuronow w warstwie
- [ ] Weight clamping (-10, 10)
- [ ] Konfigurowalny mutation rate i strength

**Powiazane pliki:**
- `plans/1general_plan.md` (sekcja 3.2)

---

### F3.T3: Crossover Operators
| Pole | Wartosc |
|------|---------|
| **ID** | F3.T3 |
| **Tytul** | Operatory krzyzowania |
| **Opis** | Implementacja krzyzowania (crossover) genomow |
| **Priorytet** | MUST |
| **Szacowany naklad** | M (Medium) |
| **Status** | PENDING |
| **Agent** | tzarbot-agent-dotnet-senior |
| **Zaleznosci** | F3.T1 |

**Kryteria akceptacji:**
- [ ] Uniform crossover struktury
- [ ] Arithmetic crossover wag
- [ ] Dziedziczenie od lepszego rodzica (opcja)
- [ ] Prawdopodobienstwo crossover (70%)
- [ ] Poprawne tworzenie potomstwa

**Powiazane pliki:**
- `plans/1general_plan.md` (sekcja 3.2)

---

### F3.T4: Selection & Elitism
| Pole | Wartosc |
|------|---------|
| **ID** | F3.T4 |
| **Tytul** | Selekcja i elityzm |
| **Opis** | Implementacja selekcji turniejowej i zachowania najlepszych osobnikow |
| **Priorytet** | MUST |
| **Szacowany naklad** | S (Small) |
| **Status** | PENDING |
| **Agent** | tzarbot-agent-dotnet-senior |
| **Zaleznosci** | F3.T1 |

**Kryteria akceptacji:**
- [ ] Tournament selection (k=3)
- [ ] Elityzm (top 5% przechodzi do nastepnej generacji)
- [ ] Konfigurowalny rozmiar turnieju
- [ ] Konfigurowalny % elityzmu
- [ ] Diversity preservation (opcjonalne)

**Powiazane pliki:**
- `plans/phase_3_detailed.md`

---

### F3.T5: Fitness Calculator & Persistence
| Pole | Wartosc |
|------|---------|
| **ID** | F3.T5 |
| **Tytul** | Kalkulator fitness i persystencja |
| **Opis** | Implementacja funkcji fitness i zapisywania genomow do bazy |
| **Priorytet** | MUST |
| **Szacowany naklad** | M (Medium) |
| **Status** | PENDING |
| **Agent** | tzarbot-agent-ai-senior |
| **Zaleznosci** | F3.T1 |

**Kryteria akceptacji:**
- [ ] Funkcja fitness: wygrana + czas + jednostki + budynki
- [ ] Kary za bezczynnosc i bledne akcje
- [ ] Persystencja SQLite + MessagePack
- [ ] GenomeRepository z CRUD operations
- [ ] Checkpoint populacji dziala (save/restore)

**Powiazane pliki:**
- `plans/1general_plan.md` (sekcja 3.3)

---

## Metryki Fazy

| Metryka | Wartosc |
|---------|---------|
| Liczba taskow | 5 |
| Ukonczonych | 0 |
| W trakcie | 0 |
| Zablokowanych | 0 |
| Oczekujacych | 5 |
| Postep | 0% |

---

## Zaleznosci

- **Wymaga:** Faza 2 (Neural Network) - szczegolnie F2.T1 (NetworkGenome)
- **Wymaga:** Faza 5 (Game State Detection) - dla funkcji fitness
- **Blokuje:** Faza 6 (Training Pipeline)

---

## Metryki sukcesu

- [ ] Populacja 100 sieci ewoluuje bez memory leaks
- [ ] Sredni fitness rosnie przez co najmniej 50 generacji
- [ ] Checkpoint populacji dziala (save/restore)
