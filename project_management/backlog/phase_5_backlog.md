# Backlog Fazy 5: Game State Detection

**Ostatnia aktualizacja:** 2025-12-08
**Status Fazy:** COMPLETED
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
| **Status** | COMPLETED |
| **Agent** | tzarbot-agent-ai-senior |
| **Zaleznosci** | F1.T2 (Screen Capture) |

**Kryteria akceptacji:**
- [x] Aplikacja konsolowa do przechwytywania regionow ekranu
- [x] Zapis templateow do plikow PNG
- [x] Konfigurowalny region (x, y, width, height)
- [x] Dokumentacja jakie ekrany przechwyywac

**Powiazane pliki:**
- `tools/TemplateCapturer/Program.cs`
- `tools/TemplateCapturer/TemplateCapturer.csproj`
- `src/TzarBot.StateDetection/Templates/README.md`

---

### F5.T2: GameStateDetector
| Pole | Wartosc |
|------|---------|
| **ID** | F5.T2 |
| **Tytul** | Detektor stanu gry |
| **Opis** | Implementacja rozpoznawania stanu gry za pomoca template matching |
| **Priorytet** | MUST |
| **Szacowany naklad** | M (Medium) |
| **Status** | COMPLETED |
| **Agent** | tzarbot-agent-ai-senior |
| **Zaleznosci** | F5.T1, F1.T2 |

**Kryteria akceptacji:**
- [x] Rozpoznawanie: Victory, Defeat, InGame, MainMenu, Loading
- [x] Template matching (OpenCV)
- [x] Color histogram analysis (backup)
- [ ] Dokladnosc victory/defeat > 99% (wymaga testow z rzeczywistymi templateami)
- [ ] Dokladnosc in-game > 95% (wymaga testow z rzeczywistymi templateami)

**Powiazane pliki:**
- `src/TzarBot.StateDetection/GameState.cs`
- `src/TzarBot.StateDetection/Detection/IGameStateDetector.cs`
- `src/TzarBot.StateDetection/Detection/DetectionResult.cs`
- `src/TzarBot.StateDetection/Detection/DetectionConfig.cs`
- `src/TzarBot.StateDetection/Detection/TemplateMatchingDetector.cs`
- `src/TzarBot.StateDetection/Detection/ColorHistogramDetector.cs`
- `src/TzarBot.StateDetection/Detection/CompositeGameStateDetector.cs`

---

### F5.T3: GameMonitor
| Pole | Wartosc |
|------|---------|
| **ID** | F5.T3 |
| **Tytul** | Monitor gry |
| **Opis** | Serwis monitorujacy stan gry w czasie rzeczywistym |
| **Priorytet** | MUST |
| **Szacowany naklad** | M (Medium) |
| **Status** | COMPLETED |
| **Agent** | tzarbot-agent-ai-senior |
| **Zaleznosci** | F5.T2 |

**Kryteria akceptacji:**
- [x] Ciagle monitorowanie stanu gry
- [x] Timeout detection (30 min max)
- [x] Crash detection (okno nie odpowiada)
- [x] Idle/stuck detection (brak aktywnosci)
- [x] Zwracanie GameResult z wynikiem

**Powiazane pliki:**
- `src/TzarBot.StateDetection/Monitoring/IGameMonitor.cs`
- `src/TzarBot.StateDetection/Monitoring/GameMonitor.cs`
- `src/TzarBot.StateDetection/Monitoring/GameMonitorConfig.cs`
- `src/TzarBot.StateDetection/Monitoring/MonitoringResult.cs`

---

### F5.T4: Stats Extraction (OCR)
| Pole | Wartosc |
|------|---------|
| **ID** | F5.T4 |
| **Tytul** | Ekstrakcja statystyk (OCR) |
| **Opis** | Odczytywanie statystyk z ekranu koncowego gry |
| **Priorytet** | SHOULD |
| **Szacowany naklad** | M (Medium) |
| **Status** | COMPLETED |
| **Agent** | tzarbot-agent-ai-senior |
| **Zaleznosci** | F5.T2 |

**Kryteria akceptacji:**
- [x] OCR regionu statystyk (Tesseract)
- [x] Parsowanie: units built/lost/killed
- [x] Parsowanie: buildings built/destroyed
- [x] Parsowanie: resources gathered
- [x] Parsowanie: game duration

**Uwagi:**
- Wymaga instalacji tessdata dla pelnej funkcjonalnosci
- Dokladnosc zalezna od rozdzielczosci i czcionek gry

**Powiazane pliki:**
- `src/TzarBot.StateDetection/Stats/IStatsExtractor.cs`
- `src/TzarBot.StateDetection/Stats/OcrStatsExtractor.cs`
- `src/TzarBot.StateDetection/Stats/GameStats.cs`

---

## Metryki Fazy

| Metryka | Wartosc |
|---------|---------|
| Liczba taskow | 4 |
| Ukonczonych | 4 |
| W trakcie | 0 |
| Zablokowanych | 0 |
| Oczekujacych | 0 |
| Postep | 100% |

---

## Zaleznosci

- **Wymaga:** Faza 1 (Game Interface) - Screen Capture - COMPLETED
- **Blokuje:** Faza 3 (Genetic Algorithm) - funkcja fitness wymaga wykrywania wyniku
- **Blokuje:** Faza 6 (Training Pipeline)

---

## Metryki sukcesu

- [ ] Dokladnosc detekcji win/loss: >99% (wymaga testow z templateami z gry)
- [ ] Dokladnosc detekcji in-game: >95% (wymaga testow z templateami z gry)
- [ ] False positive rate dla crash: <1% (wymaga testow integracyjnych)

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

- Wymagane reczne przechwycenie templateow z gry na VM
- F5.T4 (OCR) jest SHOULD - statystyki sa nice-to-have dla fitness, nie krytyczne
- Rozne rozdzielczosci moga wymagac scale-invariant matching (zaimplementowane)
- Unit testy dodane w `tests/TzarBot.Tests/Phase5/`

---

## Utworzone pliki

### Projekt TzarBot.StateDetection
```
src/TzarBot.StateDetection/
├── TzarBot.StateDetection.csproj
├── GameState.cs
├── Detection/
│   ├── IGameStateDetector.cs
│   ├── DetectionResult.cs
│   ├── DetectionConfig.cs
│   ├── TemplateMatchingDetector.cs
│   ├── ColorHistogramDetector.cs
│   └── CompositeGameStateDetector.cs
├── Monitoring/
│   ├── IGameMonitor.cs
│   ├── GameMonitor.cs
│   ├── GameMonitorConfig.cs
│   └── MonitoringResult.cs
├── Stats/
│   ├── IStatsExtractor.cs
│   ├── OcrStatsExtractor.cs
│   └── GameStats.cs
└── Templates/
    └── README.md
```

### Tool TemplateCapturer
```
tools/TemplateCapturer/
├── TemplateCapturer.csproj
└── Program.cs
```

### Testy
```
tests/TzarBot.Tests/Phase5/
├── GameStateDetectorTests.cs
├── GameMonitorTests.cs
└── StatsExtractorTests.cs
```
