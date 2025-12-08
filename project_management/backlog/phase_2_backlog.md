# Backlog Fazy 2: Neural Network Architecture

**Ostatnia aktualizacja:** 2025-12-08
**Status Fazy:** IN PROGRESS (60% ukończone)
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
| **Status** | COMPLETED ✅ |
| **Agent** | tzarbot-agent-ai-senior |
| **Zaleznosci** | F1.T1 (Project Setup) |

**Kryteria akceptacji:**
- [x] NetworkGenome zawiera konfiguracje warstw konwolucyjnych (zamrozone)
- [x] NetworkGenome zawiera liste ewoluowanych warstw ukrytych
- [x] Wagi jako plaski wektor float[]
- [x] Serializacja/deserializacja MessagePack dziala
- [x] Test round-trip serializacji przechodzi

**Zaimplementowane pliki:**
- `src/TzarBot.NeuralNetwork/Models/NetworkGenome.cs`
- `src/TzarBot.NeuralNetwork/Models/NetworkConfig.cs`
- `src/TzarBot.NeuralNetwork/Models/ConvLayerConfig.cs`
- `src/TzarBot.NeuralNetwork/Models/DenseLayerConfig.cs`
- `src/TzarBot.NeuralNetwork/Models/ActivationType.cs`
- `src/TzarBot.NeuralNetwork/GenomeSerializer.cs`
- `tests/TzarBot.Tests/NeuralNetwork/NetworkGenomeTests.cs`

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
| **Status** | COMPLETED ✅ |
| **Agent** | tzarbot-agent-dotnet-senior |
| **Zaleznosci** | F1.T2 (Screen Capture), F2.T1 |

**Kryteria akceptacji:**
- [x] Crop do obszaru gry
- [x] Downscale (1920x1080 -> 240x135)
- [x] Konwersja do grayscale (opcjonalna)
- [x] Normalizacja [0-255] -> [0.0-1.0]
- [x] Stack 4 ostatnich klatek (temporal context)
- [x] Output: 4x240x135 float tensor

**Zaimplementowane pliki:**
- `src/TzarBot.NeuralNetwork/Preprocessing/ImagePreprocessor.cs`
- `src/TzarBot.NeuralNetwork/Preprocessing/FrameBuffer.cs`
- `src/TzarBot.NeuralNetwork/Preprocessing/PreprocessorConfig.cs`
- `tests/TzarBot.Tests/NeuralNetwork/ImagePreprocessorTests.cs`

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
| **Status** | COMPLETED ✅ |
| **Agent** | tzarbot-agent-ai-senior |
| **Zaleznosci** | F2.T1 |

**Kryteria akceptacji:**
- [x] Budowanie warstw konwolucyjnych (32, 64, 64 filtry)
- [x] Budowanie dynamicznych warstw ukrytych
- [x] Output heads: Mouse Position (2 neurony) + Action Type (N neuronow)
- [x] Eksport do formatu ONNX
- [x] Model laduje sie w ONNX Runtime

**Zaimplementowane pliki:**
- `src/TzarBot.NeuralNetwork/Onnx/OnnxNetworkBuilder.cs`
- `src/TzarBot.NeuralNetwork/Onnx/OnnxGraphBuilder.cs`
- `src/TzarBot.NeuralNetwork/Onnx/OnnxModelExporter.cs`
- `tests/TzarBot.Tests/NeuralNetwork/OnnxNetworkBuilderTests.cs`

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
| **Status** | COMPLETED ✅ |
| **Agent** | tzarbot-agent-ai-senior |
| **Zaleznosci** | F2.T2, F2.T3 |

**Kryteria akceptacji:**
- [x] Ladowanie modelu ONNX
- [x] Inference time < 10ms na GPU, < 50ms na CPU
- [x] Model miesci sie w < 50MB pamieci
- [x] Mapowanie output na GameAction
- [x] Obsluga GPU i CPU fallback

**Zaimplementowane pliki:**
- `src/TzarBot.NeuralNetwork/Inference/IInferenceEngine.cs`
- `src/TzarBot.NeuralNetwork/Inference/OnnxInferenceEngine.cs`
- `src/TzarBot.NeuralNetwork/Inference/ActionDecoder.cs`
- `tests/TzarBot.Tests/NeuralNetwork/InferenceEngineTests.cs`

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
| Ukonczonych | 4 |
| W trakcie | 1 |
| Zablokowanych | 0 |
| Oczekujacych | 0 |
| Postep | 80% |

---

## Zaleznosci

- **Wymaga:** Faza 1 (Game Interface) - COMPLETED
- **Blokuje:** Faza 3 (Genetic Algorithm)

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

- Architektura bazowa: CNN (3 warstwy) + Dense (2+ warstw ewoluowanych)
- Warstwy konwolucyjne sa zamrozone (nie ewoluowane przez GA)
- Warstwy ukryte sa dynamiczne (64-1024 neuronow, 1-5 warstw)
