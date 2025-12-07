# Backlog Fazy 2: Neural Network Architecture

**Ostatnia aktualizacja:** 2025-12-07
**Status Fazy:** PENDING
**Priorytet:** MUST (wymagane dla algorytmu genetycznego)

---

## Podsumowanie

Faza 2 obejmuje implementacje architektury sieci neuronowej - przetwarzanie obrazu z gry i zwracanie akcji do wykonania. Siec musi byc wystarczajaco prosta do ewolucji przez GA i wystarczajaco ekspresywna do rozpoznawania obiektow w grze.

---

## Taski

### F2.T1: NetworkGenome & Serialization
| Pole | Wartosc |
|------|---------|
| **ID** | F2.T1 |
| **Tytul** | Definicja genomu sieci i serializacja |
| **Opis** | Implementacja klasy NetworkGenome reprezentujacej strukture i wagi sieci neuronowej |
| **Priorytet** | MUST |
| **Szacowany naklad** | M (Medium) |
| **Status** | PENDING |
| **Agent** | tzarbot-agent-ai-senior |
| **Zaleznosci** | F1.T1 (Project Setup) |

**Kryteria akceptacji:**
- [ ] NetworkGenome zawiera konfiguracje warstw konwolucyjnych (zamrozone)
- [ ] NetworkGenome zawiera liste ewoluowanych warstw ukrytych
- [ ] Wagi jako plaski wektor float[]
- [ ] Serializacja/deserializacja MessagePack dziala
- [ ] Test round-trip serializacji przechodzi

**Powiazane pliki:**
- `plans/phase_2_detailed.md`
- `plans/1general_plan.md` (sekcja 2.3)

---

### F2.T2: Image Preprocessor
| Pole | Wartosc |
|------|---------|
| **ID** | F2.T2 |
| **Tytul** | Preprocessor obrazu |
| **Opis** | Implementacja przetwarzania obrazu z gry do formatu wejsciowego sieci |
| **Priorytet** | MUST |
| **Szacowany naklad** | M (Medium) |
| **Status** | PENDING |
| **Agent** | tzarbot-agent-dotnet-senior |
| **Zaleznosci** | F1.T2 (Screen Capture), F2.T1 |

**Kryteria akceptacji:**
- [ ] Crop do obszaru gry
- [ ] Downscale (1920x1080 -> 240x135)
- [ ] Konwersja do grayscale (opcjonalna)
- [ ] Normalizacja [0-255] -> [0.0-1.0]
- [ ] Stack 4 ostatnich klatek (temporal context)
- [ ] Output: 4x240x135 float tensor

**Powiazane pliki:**
- `plans/1general_plan.md` (sekcja 2.1)

---

### F2.T3: ONNX Network Builder
| Pole | Wartosc |
|------|---------|
| **ID** | F2.T3 |
| **Tytul** | Builder sieci ONNX |
| **Opis** | Konwersja NetworkGenome do modelu ONNX |
| **Priorytet** | MUST |
| **Szacowany naklad** | L (Large) |
| **Status** | PENDING |
| **Agent** | tzarbot-agent-ai-senior |
| **Zaleznosci** | F2.T1 |

**Kryteria akceptacji:**
- [ ] Budowanie warstw konwolucyjnych (32, 64, 64 filtry)
- [ ] Budowanie dynamicznych warstw ukrytych
- [ ] Output heads: Mouse Position (2 neurony) + Action Type (N neuronow)
- [ ] Eksport do formatu ONNX
- [ ] Model laduje sie w ONNX Runtime

**Powiazane pliki:**
- `plans/1general_plan.md` (sekcja 2.2)

---

### F2.T4: Inference Engine
| Pole | Wartosc |
|------|---------|
| **ID** | F2.T4 |
| **Tytul** | Silnik inferencji |
| **Opis** | Wrapper ONNX Runtime do wykonywania inferencji sieci |
| **Priorytet** | MUST |
| **Szacowany naklad** | M (Medium) |
| **Status** | PENDING |
| **Agent** | tzarbot-agent-ai-senior |
| **Zaleznosci** | F2.T2, F2.T3 |

**Kryteria akceptacji:**
- [ ] Ladowanie modelu ONNX
- [ ] Inference time < 10ms na GPU, < 50ms na CPU
- [ ] Model miesci sie w < 50MB pamieci
- [ ] Mapowanie output na GameAction
- [ ] Obsluga GPU i CPU fallback

**Powiazane pliki:**
- `plans/phase_2_detailed.md`

---

### F2.T5: Integration Tests
| Pole | Wartosc |
|------|---------|
| **ID** | F2.T5 |
| **Tytul** | Testy integracyjne Phase 2 |
| **Opis** | Testy weryfikujace caly pipeline: obraz -> tensor -> siec -> akcja |
| **Priorytet** | MUST |
| **Szacowany naklad** | M (Medium) |
| **Status** | PENDING |
| **Agent** | QA_INTEGRATION |
| **Zaleznosci** | F2.T1, F2.T2, F2.T3, F2.T4 |

**Kryteria akceptacji:**
- [ ] Test pelnego pipeline'u
- [ ] Test serializacji genomu
- [ ] Test wydajnosci inferencji
- [ ] Test roznych konfiguracji sieci
- [ ] Wszystkie testy przechodza

**Powiazane pliki:**
- `tests/TzarBot.Tests/Phase2/`

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

- **Wymaga:** Faza 1 (Game Interface) - COMPLETED
- **Blokuje:** Faza 3 (Genetic Algorithm)

---

## Notatki

- Architektura bazowa: CNN (3 warstwy) + Dense (2+ warstw ewoluowanych)
- Warstwy konwolucyjne sa zamrozone (nie ewoluowane przez GA)
- Warstwy ukryte sa dynamiczne (64-1024 neuronow, 1-5 warstw)
