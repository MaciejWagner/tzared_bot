# Backlog Fazy 5: Game State Detection

**Ostatnia aktualizacja:** 2025-12-07
**Status Fazy:** PENDING
**Priorytet:** MUST (wymagane dla funkcji fitness)

---

## Podsumowanie

Faza 5 obejmuje implementacje modulu rozpoznajacego stan gry (wygrana/przegrana/w toku) na podstawie analizy obrazu. Kluczowy dla ewaluacji fitness sieci.

---

## Taski

### F5.T1: Template Capture Tool
| Pole | Wartosc |
|------|---------|
| **ID** | F5.T1 |
| **Tytul** | Narzedzie do przechwytywania templateow |
| **Opis** | Narzedzie do recznego przechwytywania wzorcow ekranow gry |
| **Priorytet** | MUST |
| **Szacowany naklad** | S (Small) |
| **Status** | PENDING |
| **Agent** | QA_INTEGRATION |
| **Zaleznosci** | F1.T2 (Screen Capture) |

**Kryteria akceptacji:**
- [ ] Aplikacja konsolowa do przechwytywania regionow ekranu
- [ ] Zapis templateow do plikow PNG
- [ ] Konfigurowalny region (x, y, width, height)
- [ ] Dokumentacja jakie ekrany przechwyywac

**Powiazane pliki:**
- `plans/phase_5_detailed.md`
- `tools/TemplateCapturer/`

---

### F5.T2: GameStateDetector
| Pole | Wartosc |
|------|---------|
| **ID** | F5.T2 |
| **Tytul** | Detektor stanu gry |
| **Opis** | Implementacja rozpoznawania stanu gry za pomoca template matching |
| **Priorytet** | MUST |
| **Szacowany naklad** | M (Medium) |
| **Status** | PENDING |
| **Agent** | tzarbot-agent-ai-senior |
| **Zaleznosci** | F5.T1, F1.T2 |

**Kryteria akceptacji:**
- [ ] Rozpoznawanie: Victory, Defeat, InGame, MainMenu, Loading
- [ ] Template matching (OpenCV)
- [ ] Color histogram analysis (backup)
- [ ] Dokladnosc victory/defeat > 99%
- [ ] Dokladnosc in-game > 95%

**Powiazane pliki:**
- `plans/1general_plan.md` (sekcja 5.2)
- `src/TzarBot.StateDetection/`

---

### F5.T3: GameMonitor
| Pole | Wartosc |
|------|---------|
| **ID** | F5.T3 |
| **Tytul** | Monitor gry |
| **Opis** | Serwis monitorujacy stan gry w czasie rzeczywistym |
| **Priorytet** | MUST |
| **Szacowany naklad** | M (Medium) |
| **Status** | PENDING |
| **Agent** | QA_INTEGRATION |
| **Zaleznosci** | F5.T2 |

**Kryteria akceptacji:**
- [ ] Ciagle monitorowanie stanu gry
- [ ] Timeout detection (30 min max)
- [ ] Crash detection (okno nie odpowiada)
- [ ] Idle/stuck detection (brak aktywnosci)
- [ ] Zwracanie GameResult z wynikiem

**Powiazane pliki:**
- `plans/1general_plan.md` (sekcja 5.4)

---

### F5.T4: Stats Extraction (OCR)
| Pole | Wartosc |
|------|---------|
| **ID** | F5.T4 |
| **Tytul** | Ekstrakcja statystyk (OCR) |
| **Opis** | Odczytywanie statystyk z ekranu koncowego gry |
| **Priorytet** | SHOULD |
| **Szacowany naklad** | M (Medium) |
| **Status** | PENDING |
| **Agent** | tzarbot-agent-ai-senior |
| **Zaleznosci** | F5.T2 |

**Kryteria akceptacji:**
- [ ] OCR regionu statystyk (Tesseract)
- [ ] Parsowanie: units built/lost/killed
- [ ] Parsowanie: buildings built/destroyed
- [ ] Parsowanie: resources gathered
- [ ] Parsowanie: game duration

**Powiazane pliki:**
- `plans/1general_plan.md` (sekcja 5.3)

---

## Metryki Fazy

| Metryka | Wartosc |
|---------|---------|
| Liczba taskow | 4 |
| Ukonczonych | 0 |
| W trakcie | 0 |
| Zablokowanych | 0 |
| Oczekujacych | 4 |
| Postep | 0% |

---

## Zaleznosci

- **Wymaga:** Faza 1 (Game Interface) - Screen Capture - COMPLETED
- **Blokuje:** Faza 3 (Genetic Algorithm) - funkcja fitness wymaga wykrywania wyniku
- **Blokuje:** Faza 6 (Training Pipeline)

---

## Metryki sukcesu

- [ ] Dokladnosc detekcji win/loss: >99%
- [ ] Dokladnosc detekcji in-game: >95%
- [ ] False positive rate dla crash: <1%

---

## Notatki

- Wymagane reczne przechwycenie templateow z gry
- F5.T4 (OCR) jest SHOULD - statystyki sa nice-to-have dla fitness, nie krytyczne
- Rozne rozdzielczosci moga wymagac scale-invariant matching
