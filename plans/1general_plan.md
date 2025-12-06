# Plan projektu: Tzar Bot - AI oparte na algorytmie genetycznym

## Spis treści

1. [Wprowadzenie](#wprowadzenie)
2. [Diagram zależności między fazami](#diagram-zależności-między-fazami)
3. [Faza 1: Interfejs bota (Game Interface Layer)](#faza-1-interfejs-bota)
4. [Faza 2: Architektura sieci neuronowej](#faza-2-architektura-sieci-neuronowej)
5. [Faza 3: Algorytm genetyczny](#faza-3-algorytm-genetyczny)
6. [Faza 4: Infrastruktura Hyper-V](#faza-4-infrastruktura-hyper-v)
7. [Faza 5: Detekcja wyniku gry](#faza-5-detekcja-wyniku-gry)
8. [Faza 6: Protokół uczenia (Training Pipeline)](#faza-6-protokół-uczenia)
9. [Podsumowanie technologii](#podsumowanie-technologii)
10. [Następne kroki](#następne-kroki)

---

## Wprowadzenie

### Cel projektu
Budowa bota AI do gry strategicznej Tzar (https://tza.red/) wykorzystującego algorytm genetyczny do ewolucji sieci neuronowych. Bot będzie uczył się gry poprzez:
- Przechwytywanie obrazu z gry
- Podejmowanie decyzji przez sieć neuronową
- Ewolucję sieci poprzez selekcję najlepszych graczy

### Założenia projektowe
- **Język główny**: C# (.NET 8) dla interfejsu i logiki
- **ML Framework**: ONNX Runtime + własna implementacja GA (bez TensorFlow/PyTorch dla prostoty)
- **Infrastruktura**: Hyper-V + PowerShell/Terraform
- **Komunikacja**: Named Pipes lub gRPC między procesami

---

## Diagram zależności między fazami

```
┌─────────────────────────────────────────────────────────────────────┐
│                        DIAGRAM ZALEŻNOŚCI                           │
└─────────────────────────────────────────────────────────────────────┘

                    ┌──────────────┐
                    │   FAZA 1     │
                    │  Interfejs   │
                    │    Bota      │
                    └──────┬───────┘
                           │
              ┌────────────┼────────────┐
              │            │            │
              ▼            ▼            ▼
       ┌──────────┐ ┌──────────┐ ┌──────────┐
       │  FAZA 2  │ │  FAZA 5  │ │  FAZA 4  │
       │  Sieć    │ │ Detekcja │ │ Hyper-V  │
       │Neuronowa │ │  Wyniku  │ │ Infra    │
       └────┬─────┘ └────┬─────┘ └────┬─────┘
            │            │            │
            └──────┬─────┴────────────┘
                   │
                   ▼
            ┌──────────┐
            │  FAZA 3  │
            │Algorytm  │
            │Genetyczny│
            └────┬─────┘
                 │
                 ▼
            ┌──────────┐
            │  FAZA 6  │
            │ Protokół │
            │ Uczenia  │
            └──────────┘

LEGENDA:
─────────────────────────────────────────
[Można robić równolegle]: Faza 2, 4, 5 (po ukończeniu Fazy 1)
[Sekwencyjne]: Faza 1 → (2,4,5) → Faza 3 → Faza 6
```

---

## Faza 1: Interfejs bota

### Opis implementacji

Interfejs bota to program działający w tle, który służy jako warstwa pośrednia między siecią neuronową a grą Tzar. Składa się z trzech głównych komponentów:

#### 1.1 Moduł przechwytywania ekranu (Screen Capture)

```
┌─────────────────────────────────────────────────────────────┐
│                    ARCHITEKTURA CAPTURE                      │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌─────────────┐    ┌──────────────┐    ┌──────────────┐   │
│  │   Windows   │───▶│ DXGI Desktop │───▶│  GPU Buffer  │   │
│  │   Desktop   │    │  Duplication │    │   (BGRA32)   │   │
│  └─────────────┘    └──────────────┘    └──────┬───────┘   │
│                                                 │           │
│                                                 ▼           │
│  ┌─────────────┐    ┌──────────────┐    ┌──────────────┐   │
│  │   Neural    │◀───│   Resize +   │◀───│  Crop Game   │   │
│  │   Network   │    │   Normalize  │    │   Window     │   │
│  └─────────────┘    └──────────────┘    └──────────────┘   │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

**Szczegóły implementacji:**
- Użycie DXGI Desktop Duplication API dla minimalnego narzutu CPU
- Przechwytywanie z częstotliwością 10-15 FPS (wystarczające dla RTS)
- Rozdzielczość wejściowa: 1920x1080 → przeskalowana do 480x270 (4x downscale)
- Format: Grayscale 8-bit lub RGB 24-bit (do ustalenia w testach)
- Bufor kołowy ostatnich 4 klatek dla kontekstu czasowego

#### 1.2 Moduł wysyłania akcji (Input Injection)

```csharp
// Przestrzeń akcji - dyskretyzacja
public enum ActionType
{
    // Ruchy myszy (względne)
    MouseMoveUp, MouseMoveDown, MouseMoveLeft, MouseMoveRight,
    MouseMoveDiagonal_NE, MouseMoveDiagonal_NW,
    MouseMoveDiagonal_SE, MouseMoveDiagonal_SW,

    // Kliknięcia
    LeftClick, RightClick, DoubleClick,

    // Drag (zaznaczanie jednostek)
    DragStart, DragEnd,

    // Skróty klawiszowe
    Hotkey_1, Hotkey_2, Hotkey_3, ..., Hotkey_0,
    Hotkey_Ctrl1, Hotkey_Ctrl2, ...,

    // Akcje specjalne
    ScrollUp, ScrollDown,
    Escape, Enter
}
```

**Implementacja:**
- SendInput API dla wysyłania zdarzeń myszy/klawiatury
- Dyskretyzacja ruchów myszy na 8 kierunków + 3 prędkości
- Cooldown między akcjami: 50-100ms (zapobiega spamowaniu)
- Walidacja pozycji myszy w obrębie okna gry

#### 1.3 Moduł komunikacji (IPC)

```
┌─────────────────────────────────────────────────────────────┐
│                    PROTOKÓŁ KOMUNIKACJI                      │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌───────────────┐         Named Pipe          ┌──────────┐│
│  │   Game        │◀────────────────────────────│  Neural  ││
│  │   Interface   │   Binary Protocol:          │  Network ││
│  │   (C#)        │   [FrameID:4][Action:1]     │  Process ││
│  │               │────────────────────────────▶│  (C#)    ││
│  └───────────────┘   [FrameID:4][ScreenData]   └──────────┘│
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

### Sugerowane technologie

| Komponent | Technologia | Uzasadnienie |
|-----------|-------------|--------------|
| Screen Capture | SharpDX / Vortice.Windows | Niskopoziomowy dostęp do DXGI |
| Input Injection | Windows.Forms + P/Invoke | Natywne SendInput API |
| IPC | System.IO.Pipes | Szybkie, niskopoziomowe, wbudowane w .NET |
| Window Detection | Win32 API (FindWindow) | Standardowe podejście |

### Komendy Claude Code

```bash
# Inicjalizacja projektu
claude "Utwórz nowy projekt C# .NET 8: TzarBot.GameInterface z bibliotekami SharpDX"

# Implementacja screen capture
claude "Zaimplementuj klasę ScreenCapture używającą DXGI Desktop Duplication"

# Implementacja input injection
claude "Zaimplementuj klasę InputInjector z metodami SendMouseMove, SendClick, SendKey"

# Implementacja IPC
claude "Zaimplementuj NamedPipeServer do komunikacji binarnej z procesem sieci neuronowej"
```

### Szacowany nakład pracy: **M** (Medium)

### Zależności
- Brak zależności od innych faz
- Wymaga Windows 10/11 z obsługą DXGI 1.2+

### Metryki sukcesu
- [ ] Przechwytywanie ekranu z ≥10 FPS bez spadku wydajności gry
- [ ] Latencja akcji (od decyzji do wykonania) < 50ms
- [ ] Stabilność: 0 crashów przez 1h ciągłej pracy

### Potencjalne ryzyka i mitygacja

| Ryzyko | Prawdopodobieństwo | Wpływ | Mitygacja |
|--------|-------------------|-------|-----------|
| Gra blokuje input injection | Średnie | Wysoki | Użycie SendInput zamiast PostMessage |
| Wysoki narzut CPU | Niskie | Średni | DXGI Hardware acceleration |
| Antycheat wykrywa bota | Niskie | Wysoki | Gra nie ma aktywnego antycheata |

### MVP Fazy 1
1. Prosty program wykrywający okno gry
2. Screenshot co 100ms zapisywany do pliku
3. Prosty test wysyłania kliknięcia w środek okna

---

## Faza 2: Architektura sieci neuronowej

### Opis implementacji

Sieć neuronowa przetwarza obraz z gry i zwraca akcję do wykonania. Architektura musi być:
- Wystarczająco prosta do ewolucji przez GA
- Wystarczająco ekspresywna do rozpoznawania obiektów w grze

#### 2.1 Preprocessing obrazu

```
┌─────────────────────────────────────────────────────────────┐
│                    PIPELINE PREPROCESSING                    │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  Input: 1920x1080 RGB                                       │
│           │                                                  │
│           ▼                                                  │
│  ┌─────────────────┐                                        │
│  │  Crop to game   │  (usuń paski jeśli są)                │
│  │  area           │                                        │
│  └────────┬────────┘                                        │
│           ▼                                                  │
│  ┌─────────────────┐                                        │
│  │  Downscale      │  1920x1080 → 240x135 (8x)             │
│  │  bilinear       │                                        │
│  └────────┬────────┘                                        │
│           ▼                                                  │
│  ┌─────────────────┐                                        │
│  │  Convert to     │  RGB → Grayscale (opcjonalnie)        │
│  │  grayscale      │                                        │
│  └────────┬────────┘                                        │
│           ▼                                                  │
│  ┌─────────────────┐                                        │
│  │  Normalize      │  [0-255] → [0.0-1.0]                  │
│  │  to float       │                                        │
│  └────────┬────────┘                                        │
│           ▼                                                  │
│  ┌─────────────────┐                                        │
│  │  Stack frames   │  4 ostatnie klatki (motion detection) │
│  │  (temporal)     │                                        │
│  └────────┬────────┘                                        │
│           ▼                                                  │
│  Output: 4x240x135 = 129,600 float values                   │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

#### 2.2 Architektura bazowa sieci

```
┌─────────────────────────────────────────────────────────────┐
│                 ARCHITEKTURA SIECI NEURONOWEJ                │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  WARSTWA WEJŚCIOWA                                          │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  Input: 4 × 240 × 135 (stacked frames, grayscale)   │   │
│  │  = 129,600 neurons                                   │   │
│  └─────────────────────────────────────────────────────┘   │
│                          │                                   │
│                          ▼                                   │
│  WARSTWY KONWOLUCYJNE (feature extraction)                  │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  Conv2D: 32 filters, 8x8, stride 4 → 60x34x32       │   │
│  │  ReLU                                                │   │
│  │  Conv2D: 64 filters, 4x4, stride 2 → 29x16x64       │   │
│  │  ReLU                                                │   │
│  │  Conv2D: 64 filters, 3x3, stride 1 → 27x14x64       │   │
│  │  ReLU                                                │   │
│  │  Flatten → 24,192 neurons                            │   │
│  └─────────────────────────────────────────────────────┘   │
│                          │                                   │
│                          ▼                                   │
│  WARSTWY UKRYTE (decision making) - EWOLUOWANE             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  [Dynamiczna struktura - kontrolowana przez GA]     │   │
│  │                                                      │   │
│  │  Bazowo:                                            │   │
│  │  Dense: 512 neurons, ReLU                           │   │
│  │  Dense: 256 neurons, ReLU                           │   │
│  │                                                      │   │
│  │  GA może dodać/usunąć warstwy 128-1024 neurons     │   │
│  └─────────────────────────────────────────────────────┘   │
│                          │                                   │
│                          ▼                                   │
│  WARSTWA WYJŚCIOWA                                          │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  Output Head 1: Mouse Position (2 neurons, tanh)    │   │
│  │    → dx, dy ∈ [-1, 1] (relative movement)           │   │
│  │                                                      │   │
│  │  Output Head 2: Action Type (N neurons, softmax)    │   │
│  │    → click_left, click_right, drag, hotkey_1, ...   │   │
│  │    → łącznie ~30 możliwych akcji                    │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

#### 2.3 Reprezentacja genomu sieci

```csharp
public class NetworkGenome
{
    // Stałe warstwy (nie ewoluowane)
    public ConvLayerConfig[] ConvLayers { get; } // Zamrożone

    // Ewoluowane warstwy ukryte
    public List<DenseLayerConfig> HiddenLayers { get; set; }

    // Wagi - płaski wektor float
    public float[] Weights { get; set; }

    // Metadane
    public Guid Id { get; set; }
    public int Generation { get; set; }
    public float Fitness { get; set; }
}

public class DenseLayerConfig
{
    public int NeuronCount { get; set; }  // 64-1024
    public ActivationType Activation { get; set; }  // ReLU, Tanh, LeakyReLU
    public float DropoutRate { get; set; }  // 0.0-0.5
}
```

### Sugerowane technologie

| Komponent | Technologia | Uzasadnienie |
|-----------|-------------|--------------|
| Inference | ONNX Runtime | Szybkie, C# native, GPU support |
| Model Building | Własna implementacja | Pełna kontrola nad strukturą genomu |
| Serialization | MessagePack | Szybkie, kompaktowe |
| GPU Acceleration | CUDA (opcjonalnie) | Przyspiesza inference |

### Komendy Claude Code

```bash
# Implementacja genomu
claude "Zaimplementuj klasę NetworkGenome z serializacją MessagePack"

# Implementacja sieci
claude "Zaimplementuj klasę NeuralNetwork używającą ONNX Runtime dla inference"

# Preprocessing
claude "Zaimplementuj ImagePreprocessor: resize, grayscale, normalize, stack frames"

# Eksport do ONNX
claude "Zaimplementuj NetworkBuilder który konwertuje NetworkGenome do ONNX model"
```

### Szacowany nakład pracy: **L** (Large)

### Zależności
- Faza 1 (format danych wejściowych)
- Nie blokuje: może być rozwijana równolegle z interfejsem

### Metryki sukcesu
- [ ] Inference time < 10ms na GPU, < 50ms na CPU
- [ ] Model mieści się w < 50MB pamięci
- [ ] Poprawna serializacja/deserializacja genomu (test round-trip)

### Potencjalne ryzyka i mitygacja

| Ryzyko | Prawdopodobieństwo | Wpływ | Mitygacja |
|--------|-------------------|-------|-----------|
| Zbyt duży model dla GPU | Średnie | Wysoki | Dynamiczne skalowanie rozmiaru |
| ONNX Runtime problemy | Niskie | Średni | Fallback na własną implementację |
| Vanishing gradients | Średnie | Niski | Batch normalization, residual connections |

### MVP Fazy 2
1. Prosta sieć Dense-only (bez konwolucji) jako proof of concept
2. Losowe wagi, sprawdzenie że inference działa
3. Test serializacji/deserializacji

---

## Faza 3: Algorytm genetyczny

### Opis implementacji

Algorytm genetyczny ewoluuje populację sieci neuronowych poprzez selekcję, krzyżowanie i mutację.

#### 3.1 Struktura algorytmu

```
┌─────────────────────────────────────────────────────────────┐
│                    CYKL ALGORYTMU GENETYCZNEGO               │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌─────────────────┐                                        │
│  │  Inicjalizacja  │  Losowa populacja N sieci              │
│  │  Populacji      │  (N = 50-200)                          │
│  └────────┬────────┘                                        │
│           │                                                  │
│           ▼                                                  │
│  ┌─────────────────┐                                        │
│  │  Ewaluacja      │  Każda sieć gra X gier                │
│  │  Fitness        │  Fitness = f(wynik, czas, akcje)       │
│  └────────┬────────┘                                        │
│           │                                                  │
│           ▼                                                  │
│  ┌─────────────────┐                                        │
│  │  Selekcja       │  Tournament selection (k=3)            │
│  │  Rodziców       │  Elityzm: top 5% przechodzi           │
│  └────────┬────────┘                                        │
│           │                                                  │
│           ▼                                                  │
│  ┌─────────────────┐                                        │
│  │  Krzyżowanie    │  Crossover wag + struktur              │
│  │  (Crossover)    │  Prawdopodobieństwo: 70%               │
│  └────────┬────────┘                                        │
│           │                                                  │
│           ▼                                                  │
│  ┌─────────────────┐                                        │
│  │  Mutacja        │  Wagi: Gaussian noise                  │
│  │                 │  Struktura: add/remove layer           │
│  │                 │  Prawdopodobieństwo: 20%               │
│  └────────┬────────┘                                        │
│           │                                                  │
│           ▼                                                  │
│  ┌─────────────────┐                                        │
│  │  Nowa           │  Powtórz od Ewaluacji                  │
│  │  Generacja      │                                        │
│  └─────────────────┘                                        │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

#### 3.2 Operatory genetyczne

##### Mutacja wag
```csharp
public void MutateWeights(NetworkGenome genome, float mutationRate, float mutationStrength)
{
    for (int i = 0; i < genome.Weights.Length; i++)
    {
        if (Random.NextFloat() < mutationRate)
        {
            // Gaussian mutation
            genome.Weights[i] += Random.NextGaussian() * mutationStrength;

            // Clamp to prevent explosion
            genome.Weights[i] = Math.Clamp(genome.Weights[i], -10f, 10f);
        }
    }
}
```

##### Mutacja struktury
```csharp
public void MutateStructure(NetworkGenome genome)
{
    float roll = Random.NextFloat();

    if (roll < 0.1f && genome.HiddenLayers.Count < 5)
    {
        // Dodaj nową warstwę
        int position = Random.Next(genome.HiddenLayers.Count + 1);
        int neurons = Random.Next(64, 512);
        genome.HiddenLayers.Insert(position, new DenseLayerConfig
        {
            NeuronCount = neurons,
            Activation = ActivationType.ReLU
        });
        // Zainicjuj nowe wagi (Xavier initialization)
        ReinitializeWeights(genome);
    }
    else if (roll < 0.2f && genome.HiddenLayers.Count > 1)
    {
        // Usuń losową warstwę
        int position = Random.Next(genome.HiddenLayers.Count);
        genome.HiddenLayers.RemoveAt(position);
        ReinitializeWeights(genome);
    }
    else if (roll < 0.4f)
    {
        // Zmień liczbę neuronów w losowej warstwie
        int layerIdx = Random.Next(genome.HiddenLayers.Count);
        int delta = Random.Next(-64, 65);
        genome.HiddenLayers[layerIdx].NeuronCount =
            Math.Clamp(genome.HiddenLayers[layerIdx].NeuronCount + delta, 32, 1024);
        ReinitializeWeights(genome);
    }
}
```

##### Krzyżowanie (Crossover)
```csharp
public NetworkGenome Crossover(NetworkGenome parent1, NetworkGenome parent2)
{
    var child = new NetworkGenome();

    // Struktura: bierzemy od lepszego rodzica lub mieszamy
    if (parent1.Fitness > parent2.Fitness)
    {
        child.HiddenLayers = CloneLayers(parent1.HiddenLayers);
    }
    else if (Random.NextFloat() < 0.5f)
    {
        // Uniform crossover struktury
        child.HiddenLayers = new List<DenseLayerConfig>();
        int maxLayers = Math.Max(parent1.HiddenLayers.Count, parent2.HiddenLayers.Count);
        for (int i = 0; i < maxLayers; i++)
        {
            var source = Random.NextFloat() < 0.5f ? parent1 : parent2;
            if (i < source.HiddenLayers.Count)
            {
                child.HiddenLayers.Add(Clone(source.HiddenLayers[i]));
            }
        }
    }

    // Wagi: arithmetic crossover
    child.Weights = new float[CalculateWeightCount(child)];
    for (int i = 0; i < child.Weights.Length; i++)
    {
        float alpha = Random.NextFloat();
        float w1 = i < parent1.Weights.Length ? parent1.Weights[i] : 0;
        float w2 = i < parent2.Weights.Length ? parent2.Weights[i] : 0;
        child.Weights[i] = alpha * w1 + (1 - alpha) * w2;
    }

    return child;
}
```

#### 3.3 Funkcja fitness

```csharp
public float CalculateFitness(GameResult result)
{
    float fitness = 0;

    // Główny komponent: wynik gry
    if (result.Won)
    {
        fitness += 1000;
        fitness += 500 * (1.0f / result.GameDurationMinutes); // Bonus za szybką wygraną
    }
    else if (result.Lost)
    {
        fitness += 100 * result.SurvivalTimeMinutes; // Nagroda za przetrwanie
    }
    else // Timeout/crash
    {
        fitness += 50 * result.SurvivalTimeMinutes;
    }

    // Komponenty pomocnicze (kształtują zachowanie)
    fitness += 10 * result.UnitsBuilt;
    fitness += 5 * result.BuildingsBuilt;
    fitness += 20 * result.EnemyUnitsKilled;
    fitness += 50 * result.EnemyBuildingsDestroyed;

    // Kary
    fitness -= 1 * result.IdleTimeSeconds; // Kara za bezczynność
    fitness -= 0.5f * result.InvalidActionsCount; // Kara za błędne akcje

    return Math.Max(0, fitness);
}
```

### Sugerowane technologie

| Komponent | Technologia | Uzasadnienie |
|-----------|-------------|--------------|
| GA Engine | Własna implementacja C# | Pełna kontrola, specyficzne dla problemu |
| Parallelization | System.Threading.Tasks | Równoległa ewaluacja |
| Persistence | SQLite + MessagePack | Lokalna baza danych genomów |
| Visualization | Blazor WebApp | Dashboard postępów |

### Komendy Claude Code

```bash
# Implementacja GA engine
claude "Zaimplementuj GeneticAlgorithm z Tournament Selection i Elityzm"

# Operatory genetyczne
claude "Zaimplementuj mutację wag z Gaussian noise i mutację struktury sieci"

# Crossover
claude "Zaimplementuj uniform crossover dla NetworkGenome"

# Fitness function
claude "Zaimplementuj FitnessCalculator z konfigurowalnymi wagami"

# Persistence
claude "Zaimplementuj GenomeRepository używający SQLite i MessagePack"
```

### Szacowany nakład pracy: **M** (Medium)

### Zależności
- Faza 2 (struktura NetworkGenome)
- Faza 5 (wyniki gier do fitness)

### Metryki sukcesu
- [ ] Populacja 100 sieci ewoluuje bez memory leaks
- [ ] Średni fitness rośnie przez co najmniej 50 generacji
- [ ] Checkpoint populacji działa (save/restore)

### Potencjalne ryzyka i mitygacja

| Ryzyko | Prawdopodobieństwo | Wpływ | Mitygacja |
|--------|-------------------|-------|-----------|
| Premature convergence | Wysokie | Wysoki | Diversity preservation, speciation |
| Exploding weights | Średnie | Średni | Weight clamping, gradient clipping |
| Invalid network structures | Niskie | Średni | Walidacja przed ewaluacją |

### MVP Fazy 3
1. Prosty GA z mutacją wag tylko (bez mutacji struktury)
2. Populacja 20 sieci
3. Funkcja fitness = losowa liczba (test pipeline)

---

## Faza 4: Infrastruktura Hyper-V

### Opis implementacji

Infrastruktura do równoległego treningu na wielu maszynach wirtualnych z grą Tzar.

#### 4.1 Architektura systemu

```
┌─────────────────────────────────────────────────────────────────────────┐
│                      ARCHITEKTURA INFRASTRUKTURY                         │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ┌────────────────────────────────────────────────────────────────┐    │
│  │                        HOST MACHINE                             │    │
│  │  ┌──────────────────────────────────────────────────────────┐  │    │
│  │  │                   ORCHESTRATOR SERVICE                    │  │    │
│  │  │  - Zarządza pulą VM                                      │  │    │
│  │  │  - Dystrybuuje genomy do ewaluacji                       │  │    │
│  │  │  - Zbiera wyniki                                          │  │    │
│  │  │  - Uruchamia GA między generacjami                       │  │    │
│  │  └──────────────────────────────────────────────────────────┘  │    │
│  │                              │                                   │    │
│  │              ┌───────────────┼───────────────┐                  │    │
│  │              │               │               │                  │    │
│  │              ▼               ▼               ▼                  │    │
│  │  ┌────────────────┐ ┌────────────────┐ ┌────────────────┐      │    │
│  │  │     VM #1      │ │     VM #2      │ │     VM #N      │      │    │
│  │  │  ┌──────────┐  │ │  ┌──────────┐  │ │  ┌──────────┐  │      │    │
│  │  │  │  Tzar    │  │ │  │  Tzar    │  │ │  │  Tzar    │  │      │    │
│  │  │  │  Game    │  │ │  │  Game    │  │ │  │  Game    │  │      │    │
│  │  │  └──────────┘  │ │  └──────────┘  │ │  └──────────┘  │      │    │
│  │  │  ┌──────────┐  │ │  ┌──────────┐  │ │  ┌──────────┐  │      │    │
│  │  │  │  Bot     │  │ │  │  Bot     │  │ │  │  Bot     │  │      │    │
│  │  │  │ Interface│  │ │  │ Interface│  │ │  │ Interface│  │      │    │
│  │  │  └──────────┘  │ │  └──────────┘  │ │  └──────────┘  │      │    │
│  │  │  ┌──────────┐  │ │  ┌──────────┐  │ │  ┌──────────┐  │      │    │
│  │  │  │  Neural  │  │ │  │  Neural  │  │ │  │  Neural  │  │      │    │
│  │  │  │  Network │  │ │  │  Network │  │ │  │  Network │  │      │    │
│  │  │  └──────────┘  │ │  └──────────┘  │ │  └──────────┘  │      │    │
│  │  └────────────────┘ └────────────────┘ └────────────────┘      │    │
│  │       Genome A           Genome B           Genome N            │    │
│  └────────────────────────────────────────────────────────────────┘    │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

#### 4.2 Konfiguracja VM

```powershell
# Template VM - przygotowany ręcznie:
# - Windows 10 LTSC (minimal footprint)
# - Tzar zainstalowany i skonfigurowany
# - Bot Interface zainstalowany jako Windows Service
# - Auto-login do konta lokalnego
# - Startup script uruchamiający grę

$VMConfig = @{
    Name = "TzarBot-Template"
    MemoryGB = 4
    ProcessorCount = 2
    VHDPath = "C:\VMs\TzarBot-Template.vhdx"
    Generation = 2
    SwitchName = "TzarBotSwitch"  # Internal switch
}
```

#### 4.3 Skrypty Terraform/PowerShell

```hcl
# terraform/main.tf

variable "vm_count" {
  default = 8
}

variable "template_vhd" {
  default = "C:/VMs/TzarBot-Template.vhdx"
}

resource "hyperv_machine" "tzar_worker" {
  count             = var.vm_count
  name              = "TzarBot-Worker-${count.index}"
  generation        = 2
  memory_startup    = 4096
  processor_count   = 2

  network_adaptors {
    name        = "Network"
    switch_name = "TzarBotSwitch"
  }

  hard_disk_drives {
    path = "C:/VMs/Workers/TzarBot-Worker-${count.index}.vhdx"
    # Differencing disk from template
  }
}
```

```powershell
# scripts/New-TzarWorkerVM.ps1

param(
    [int]$VMCount = 8,
    [string]$TemplatePath = "C:\VMs\TzarBot-Template.vhdx",
    [string]$WorkersPath = "C:\VMs\Workers"
)

# Utwórz internal switch jeśli nie istnieje
$switchName = "TzarBotSwitch"
if (-not (Get-VMSwitch -Name $switchName -ErrorAction SilentlyContinue)) {
    New-VMSwitch -Name $switchName -SwitchType Internal
    # Skonfiguruj NAT dla dostępu do internetu (opcjonalnie)
}

# Utwórz differencing VHDs i VMs
for ($i = 0; $i -lt $VMCount; $i++) {
    $vmName = "TzarBot-Worker-$i"
    $vhdPath = "$WorkersPath\$vmName.vhdx"

    # Utwórz differencing disk
    New-VHD -Path $vhdPath -ParentPath $TemplatePath -Differencing

    # Utwórz VM
    New-VM -Name $vmName -Generation 2 -MemoryStartupBytes 4GB `
           -VHDPath $vhdPath -SwitchName $switchName

    Set-VM -Name $vmName -ProcessorCount 2 -AutomaticStartAction Start

    # Włącz Enhanced Session Mode dla RDP
    Set-VM -Name $vmName -EnhancedSessionTransportType HvSocket
}
```

#### 4.4 Orchestrator Service

```csharp
public class TrainingOrchestrator
{
    private readonly IVMManager _vmManager;
    private readonly IGenomeRepository _genomeRepo;
    private readonly IResultCollector _resultCollector;

    public async Task RunGenerationAsync(int generation)
    {
        var genomes = await _genomeRepo.GetGenomesForGenerationAsync(generation);
        var vms = await _vmManager.GetAvailableVMsAsync();

        // Rozdziel genomy na dostępne VM
        var batches = DistributeGenomes(genomes, vms.Count);

        // Uruchom równolegle na wszystkich VM
        var tasks = new List<Task<GameResult[]>>();
        for (int i = 0; i < vms.Count; i++)
        {
            tasks.Add(RunBatchOnVMAsync(vms[i], batches[i]));
        }

        var allResults = await Task.WhenAll(tasks);

        // Zbierz wyniki i zapisz
        foreach (var results in allResults.SelectMany(r => r))
        {
            await _resultCollector.SaveResultAsync(results);
        }
    }

    private async Task<GameResult[]> RunBatchOnVMAsync(VM vm, NetworkGenome[] genomes)
    {
        var results = new List<GameResult>();

        foreach (var genome in genomes)
        {
            // Wyślij genom do VM
            await _vmManager.SendGenomeAsync(vm, genome);

            // Uruchom grę
            await _vmManager.StartGameAsync(vm);

            // Czekaj na wynik (z timeoutem)
            var result = await _vmManager.WaitForGameResultAsync(vm, TimeSpan.FromMinutes(30));

            results.Add(result);

            // Restart gry dla następnego genomu
            await _vmManager.ResetGameAsync(vm);
        }

        return results.ToArray();
    }
}
```

### Sugerowane technologie

| Komponent | Technologia | Uzasadnienie |
|-----------|-------------|--------------|
| VM Management | Hyper-V PowerShell Module | Natywne dla Windows |
| IaC (opcjonalnie) | Terraform + hyperv provider | Reproducible infrastructure |
| Orchestration | C# Windows Service | Integracja z resztą projektu |
| Communication | WinRM / PowerShell Remoting | Standardowe dla Windows |
| Monitoring | Prometheus + Grafana (opcjonalnie) | Industry standard |

### Komendy Claude Code

```bash
# Skrypt tworzenia VM
claude "Napisz skrypt PowerShell New-TzarWorkerVM.ps1 tworzący N maszyn wirtualnych z differencing disks"

# Orchestrator service
claude "Zaimplementuj TrainingOrchestrator jako Windows Service z komunikacją WinRM"

# VM Manager
claude "Zaimplementuj IVMManager używający Hyper-V PowerShell cmdlets"

# Monitoring
claude "Dodaj Prometheus metrics do Orchestratora: liczba aktywnych VM, postęp treningu"
```

### Szacowany nakład pracy: **XL** (Extra Large)

### Zależności
- Faza 1 (Bot Interface musi działać w VM)
- Licencja Windows dla VM
- Odpowiedni hardware (RAM, CPU cores)

### Metryki sukcesu
- [ ] Automatyczne tworzenie 8+ VM z template
- [ ] Stabilność: VM działają 24h bez interwencji
- [ ] Czas setupu nowej generacji < 5 minut

### Potencjalne ryzyka i mitygacja

| Ryzyko | Prawdopodobieństwo | Wpływ | Mitygacja |
|--------|-------------------|-------|-----------|
| Niewystarczający RAM na hoście | Średnie | Wysoki | Dynamic Memory, mniejsze VM |
| VM crash podczas gry | Wysokie | Średni | Auto-restart, checkpoint |
| Problemy z licencjami Windows | Niskie | Wysoki | Windows 10 Enterprise ma prawa do wirtualizacji |
| Sieć między VM a hostem | Średnie | Średni | Internal switch, przetestować przed produkcją |

### MVP Fazy 4
1. Ręcznie utworzony template VM z zainstalowaną grą
2. Skrypt tworzący 2 klony VM
3. Prosty test uruchomienia gry na obu VM jednocześnie

---

## Faza 5: Detekcja wyniku gry

### Opis implementacji

Moduł rozpoznający stan gry (wygrana/przegrana/w toku) na podstawie analizy obrazu.

#### 5.1 Ekrany końcowe w Tzar

```
┌─────────────────────────────────────────────────────────────┐
│                    EKRANY DO ROZPOZNANIA                     │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  1. EKRAN WYGRANEJ (Victory Screen)                         │
│     ┌─────────────────────────────────────┐                 │
│     │                                     │                 │
│     │         🏆 VICTORY! 🏆              │                 │
│     │                                     │                 │
│     │     [Statistics Panel]              │                 │
│     │                                     │                 │
│     │           [OK Button]               │                 │
│     │                                     │                 │
│     └─────────────────────────────────────┘                 │
│     Signature: Zielone/złote kolory, tekst "VICTORY"        │
│                                                              │
│  2. EKRAN PRZEGRANEJ (Defeat Screen)                        │
│     ┌─────────────────────────────────────┐                 │
│     │                                     │                 │
│     │         💀 DEFEAT 💀                │                 │
│     │                                     │                 │
│     │     [Statistics Panel]              │                 │
│     │                                     │                 │
│     │           [OK Button]               │                 │
│     │                                     │                 │
│     └─────────────────────────────────────┘                 │
│     Signature: Czerwone kolory, tekst "DEFEAT"              │
│                                                              │
│  3. EKRAN GRY (In-Game)                                     │
│     ┌─────────────────────────────────────┐                 │
│     │  [Minimap]              [Resources] │                 │
│     │                                     │                 │
│     │         [Game World]                │                 │
│     │                                     │                 │
│     │  [Unit Panel]     [Command Buttons] │                 │
│     └─────────────────────────────────────┘                 │
│     Signature: Obecność minimapy, paska zasobów             │
│                                                              │
│  4. EKRAN MENU (Main Menu / Pause)                          │
│     Signature: Charakterystyczne przyciski menu             │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

#### 5.2 Metody detekcji

```csharp
public enum GameState
{
    Unknown,
    InGame,
    Victory,
    Defeat,
    MainMenu,
    Loading,
    Crashed
}

public class GameStateDetector
{
    // Metoda 1: Template Matching (szybka, dokładna dla stałych UI)
    private readonly Dictionary<GameState, Mat> _templates;

    // Metoda 2: Color Histogram Analysis
    // Victory screen ma charakterystyczny profil kolorów

    // Metoda 3: OCR dla tekstu (backup)
    private readonly TesseractEngine _ocr;

    public GameState DetectState(Mat screenshot)
    {
        // Sprawdź najpierw proste heurystyki

        // 1. Czy okno gry jest aktywne?
        if (IsWindowMinimized() || IsWindowNotResponding())
        {
            return GameState.Crashed;
        }

        // 2. Template matching dla ekranów końcowych
        var victoryMatch = TemplateMatch(screenshot, _templates[GameState.Victory]);
        if (victoryMatch > 0.8f)
        {
            return GameState.Victory;
        }

        var defeatMatch = TemplateMatch(screenshot, _templates[GameState.Defeat]);
        if (defeatMatch > 0.8f)
        {
            return GameState.Defeat;
        }

        // 3. Sprawdź czy widoczna jest minimapa (= in-game)
        var minimapRegion = ExtractMinimapRegion(screenshot);
        if (IsValidMinimap(minimapRegion))
        {
            return GameState.InGame;
        }

        // 4. Sprawdź menu główne
        var menuMatch = TemplateMatch(screenshot, _templates[GameState.MainMenu]);
        if (menuMatch > 0.7f)
        {
            return GameState.MainMenu;
        }

        return GameState.Unknown;
    }

    private float TemplateMatch(Mat image, Mat template)
    {
        using var result = new Mat();
        Cv2.MatchTemplate(image, template, result, TemplateMatchModes.CCoeffNormed);
        Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out _);
        return (float)maxVal;
    }
}
```

#### 5.3 Zbieranie statystyk z ekranu końcowego

```csharp
public class GameStatsExtractor
{
    public GameStats ExtractStats(Mat victoryOrDefeatScreen)
    {
        // OCR na region ze statystykami
        var statsRegion = CropStatsRegion(victoryOrDefeatScreen);
        var text = _ocr.Recognize(statsRegion);

        // Parse tekstu
        return new GameStats
        {
            UnitsBuilt = ParseStat(text, "Units Built"),
            UnitsLost = ParseStat(text, "Units Lost"),
            UnitsKilled = ParseStat(text, "Units Killed"),
            BuildingsBuilt = ParseStat(text, "Buildings Built"),
            ResourcesGathered = ParseStat(text, "Resources Gathered"),
            GameDuration = ParseDuration(text)
        };
    }
}
```

#### 5.4 Obsługa edge cases

```csharp
public class GameMonitor
{
    private DateTime _lastActivityTime;
    private GameState _lastKnownState;
    private int _consecutiveUnknownFrames;

    public async Task<GameResult> MonitorGameAsync(CancellationToken ct)
    {
        var startTime = DateTime.UtcNow;
        var maxDuration = TimeSpan.FromMinutes(30);

        while (!ct.IsCancellationRequested)
        {
            var screenshot = await CaptureScreenAsync();
            var state = _detector.DetectState(screenshot);

            // Timeout check
            if (DateTime.UtcNow - startTime > maxDuration)
            {
                return new GameResult
                {
                    Outcome = GameOutcome.Timeout,
                    Duration = maxDuration
                };
            }

            // Crash detection
            if (state == GameState.Unknown)
            {
                _consecutiveUnknownFrames++;
                if (_consecutiveUnknownFrames > 30) // ~3 sekundy
                {
                    // Sprawdź czy proces gry żyje
                    if (!IsGameProcessRunning())
                    {
                        return new GameResult
                        {
                            Outcome = GameOutcome.Crashed,
                            Duration = DateTime.UtcNow - startTime
                        };
                    }
                }
            }
            else
            {
                _consecutiveUnknownFrames = 0;
            }

            // Win/Loss detection
            if (state == GameState.Victory)
            {
                var stats = _statsExtractor.ExtractStats(screenshot);
                return new GameResult
                {
                    Outcome = GameOutcome.Victory,
                    Duration = DateTime.UtcNow - startTime,
                    Stats = stats
                };
            }

            if (state == GameState.Defeat)
            {
                var stats = _statsExtractor.ExtractStats(screenshot);
                return new GameResult
                {
                    Outcome = GameOutcome.Defeat,
                    Duration = DateTime.UtcNow - startTime,
                    Stats = stats
                };
            }

            // Idle detection (bot nie robi nic)
            if (state == GameState.InGame)
            {
                if (HasScreenChanged(screenshot))
                {
                    _lastActivityTime = DateTime.UtcNow;
                }
                else if (DateTime.UtcNow - _lastActivityTime > TimeSpan.FromMinutes(2))
                {
                    // Bot jest zablokowany
                    return new GameResult
                    {
                        Outcome = GameOutcome.Stuck,
                        Duration = DateTime.UtcNow - startTime
                    };
                }
            }

            await Task.Delay(100, ct);
        }

        return new GameResult { Outcome = GameOutcome.Cancelled };
    }
}
```

### Sugerowane technologie

| Komponent | Technologia | Uzasadnienie |
|-----------|-------------|--------------|
| Image Processing | OpenCvSharp4 | Dojrzała biblioteka, C# bindings |
| Template Matching | OpenCV MatchTemplate | Szybkie, dokładne dla stałych UI |
| OCR (opcjonalnie) | Tesseract.NET | Open source, dobre dla prostego tekstu |
| Window Monitoring | Win32 API | Natywne API Windows |

### Komendy Claude Code

```bash
# GameStateDetector
claude "Zaimplementuj GameStateDetector używający OpenCvSharp4 template matching"

# Template capture tool
claude "Napisz narzędzie do przechwytywania template'ów z gry (screenshot regions)"

# Game monitor
claude "Zaimplementuj GameMonitor z obsługą timeout, crash, stuck detection"

# Stats extractor
claude "Zaimplementuj GameStatsExtractor z OCR dla statystyk końcowych"
```

### Szacowany nakład pracy: **M** (Medium)

### Zależności
- Faza 1 (screen capture)
- Wymaga ręcznego przechwycenia template'ów z gry

### Metryki sukcesu
- [ ] Dokładność detekcji win/loss: >99%
- [ ] Dokładność detekcji in-game: >95%
- [ ] False positive rate dla crash: <1%

### Potencjalne ryzyka i mitygacja

| Ryzyko | Prawdopodobieństwo | Wpływ | Mitygacja |
|--------|-------------------|-------|-----------|
| UI gry się zmieni po aktualizacji | Niskie | Wysoki | Versioned templates, łatwa aktualizacja |
| Różne rozdzielczości | Średnie | Średni | Scale-invariant matching lub normalizacja |
| OCR błędnie odczytuje statystyki | Średnie | Niski | Statystyki są nice-to-have, nie krytyczne |

### MVP Fazy 5
1. Detekcja tylko Victory/Defeat (bez szczegółowych stanów)
2. Hardcoded template'y dla jednej rozdzielczości
3. Proste timeout detection

---

## Faza 6: Protokół uczenia (Training Pipeline)

### Opis implementacji

Pełny pipeline uczenia od początkowych losowych sieci do zaawansowanych graczy.

#### 6.1 Etapy ewolucji

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        ETAPY PROTOKOŁU UCZENIA                           │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ETAP 0: BOOTSTRAP (generacje 1-10)                                     │
│  ┌────────────────────────────────────────────────────────────────┐    │
│  │  Cel: Nauczyć podstawowej interakcji z grą                      │    │
│  │                                                                  │    │
│  │  • Sieci uczą się wykonywać jakiekolwiek sensowne akcje        │    │
│  │  • Fitness: liczba wykonanych akcji + czas przetrwania          │    │
│  │  • Środowisko: Skirmish vs 1 Passive AI                         │    │
│  │  • Kryterium awansu: sieć przetrwa 2 minuty                     │    │
│  └────────────────────────────────────────────────────────────────┘    │
│                              │                                           │
│                              ▼                                           │
│  ETAP 1: PODSTAWOWY (generacje 11-50)                                   │
│  ┌────────────────────────────────────────────────────────────────┐    │
│  │  Cel: Nauczyć budowania bazy i jednostek                        │    │
│  │                                                                  │    │
│  │  • Sieci uczą się scrollować mapę, zaznaczać, budować          │    │
│  │  • Fitness: budynki + jednostki + zasoby                        │    │
│  │  • Środowisko: Skirmish vs Easy AI                              │    │
│  │  • Kryterium awansu: zbudowanie 5+ budynków                     │    │
│  └────────────────────────────────────────────────────────────────┘    │
│                              │                                           │
│                              ▼                                           │
│  ETAP 2: WALKA Z AI (generacje 51-200)                                  │
│  ┌────────────────────────────────────────────────────────────────┐    │
│  │  Cel: Nauczyć wygrywać z wbudowanym AI                          │    │
│  │                                                                  │    │
│  │  Poziom 2a: Easy AI (gen 51-80)                                 │    │
│  │    • Kryterium awansu: 10/20 wygranych                          │    │
│  │                                                                  │    │
│  │  Poziom 2b: Normal AI (gen 81-120)                              │    │
│  │    • Kryterium awansu: 10/20 wygranych                          │    │
│  │                                                                  │    │
│  │  Poziom 2c: Hard AI (gen 121-200)                               │    │
│  │    • Kryterium awansu: 10/20 wygranych                          │    │
│  │                                                                  │    │
│  │  Fitness: win_rate + efficiency_bonus                           │    │
│  └────────────────────────────────────────────────────────────────┘    │
│                              │                                           │
│                              ▼                                           │
│  ETAP 3: TURNIEJOWY (generacje 200+)                                    │
│  ┌────────────────────────────────────────────────────────────────┐    │
│  │  Cel: Ciągłe doskonalenie przez konkurencję                     │    │
│  │                                                                  │    │
│  │  • Sieci walczą między sobą (round-robin lub Swiss)            │    │
│  │  • Top 20% przechodzi do następnej generacji                    │    │
│  │  • Co 10 generacji: re-test vs Hard AI (sanity check)          │    │
│  │  • Fitness: ELO rating obliczane z wyników turniejowych        │    │
│  └────────────────────────────────────────────────────────────────┘    │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

#### 6.2 Curriculum Learning - szczegóły

```csharp
public class CurriculumManager
{
    public CurriculumStage DetermineStage(int generation, PopulationStats stats)
    {
        // Etap 0: Bootstrap
        if (generation <= 10)
        {
            return new CurriculumStage
            {
                Name = "Bootstrap",
                Opponent = OpponentType.PassiveAI,
                GamesPerGenome = 3,
                FitnessFunction = FitnessFunctions.SurvivalBased,
                PromotionCriteria = g => g.AverageSurvivalTime > TimeSpan.FromMinutes(2)
            };
        }

        // Etap 1: Podstawowy
        if (generation <= 50 || stats.AverageBuildingCount < 5)
        {
            return new CurriculumStage
            {
                Name = "Basic",
                Opponent = OpponentType.EasyAI,
                GamesPerGenome = 5,
                FitnessFunction = FitnessFunctions.EconomyBased,
                PromotionCriteria = g => g.AverageBuildingCount >= 5
            };
        }

        // Etap 2a: Easy AI
        if (generation <= 80 || stats.WinRateVsEasyAI < 0.5f)
        {
            return new CurriculumStage
            {
                Name = "Combat-Easy",
                Opponent = OpponentType.EasyAI,
                GamesPerGenome = 10,
                FitnessFunction = FitnessFunctions.WinRateBased,
                PromotionCriteria = g => g.WinRate >= 0.5f
            };
        }

        // ... kontynuacja dla Normal, Hard AI

        // Etap 3: Turniejowy
        return new CurriculumStage
        {
            Name = "Tournament",
            Opponent = OpponentType.SelfPlay,
            GamesPerGenome = 20,
            FitnessFunction = FitnessFunctions.ELOBased,
            PromotionCriteria = null // Ciągła ewolucja
        };
    }
}
```

#### 6.3 Self-play Tournament

```csharp
public class TournamentManager
{
    public async Task<Dictionary<Guid, float>> RunTournamentAsync(
        IEnumerable<NetworkGenome> population,
        int gamesPerPair = 2)
    {
        var ratings = new Dictionary<Guid, float>();
        var participants = population.ToList();

        // Inicjalizacja ELO
        foreach (var genome in participants)
        {
            ratings[genome.Id] = genome.PreviousElo ?? 1200f;
        }

        // Swiss-system pairing (lub round-robin dla małych populacji)
        var rounds = CalculateSwissRounds(participants.Count);

        for (int round = 0; round < rounds; round++)
        {
            var pairings = GenerateSwissPairings(participants, ratings, round);

            // Rozegraj wszystkie mecze równolegle
            var matches = pairings.Select(p => PlayMatchAsync(p.genome1, p.genome2));
            var results = await Task.WhenAll(matches);

            // Aktualizuj ELO
            foreach (var result in results)
            {
                UpdateElo(ratings, result);
            }
        }

        return ratings;
    }

    private void UpdateElo(Dictionary<Guid, float> ratings, MatchResult result)
    {
        const float K = 32f;

        float r1 = ratings[result.Genome1Id];
        float r2 = ratings[result.Genome2Id];

        float expected1 = 1f / (1f + MathF.Pow(10f, (r2 - r1) / 400f));
        float expected2 = 1f - expected1;

        float score1 = result.Winner == result.Genome1Id ? 1f :
                       result.Winner == result.Genome2Id ? 0f : 0.5f;
        float score2 = 1f - score1;

        ratings[result.Genome1Id] = r1 + K * (score1 - expected1);
        ratings[result.Genome2Id] = r2 + K * (score2 - expected2);
    }
}
```

#### 6.4 Checkpointing i recovery

```csharp
public class CheckpointManager
{
    private readonly string _checkpointDir;

    public async Task SaveCheckpointAsync(TrainingState state)
    {
        var checkpoint = new Checkpoint
        {
            Generation = state.Generation,
            Stage = state.CurrentStage,
            Population = state.Population.Select(g => g.Serialize()).ToList(),
            BestGenome = state.BestGenome.Serialize(),
            Stats = state.HistoricalStats,
            Timestamp = DateTime.UtcNow
        };

        var path = Path.Combine(_checkpointDir, $"checkpoint_gen{state.Generation}.bin");
        await File.WriteAllBytesAsync(path, MessagePackSerializer.Serialize(checkpoint));

        // Zachowaj ostatnie 10 checkpointów
        CleanupOldCheckpoints(keep: 10);

        // Dodatkowo: backup najlepszego genomu
        var bestPath = Path.Combine(_checkpointDir, "best_genome.bin");
        await File.WriteAllBytesAsync(bestPath, state.BestGenome.Serialize());
    }

    public async Task<TrainingState> LoadLatestCheckpointAsync()
    {
        var latestCheckpoint = Directory.GetFiles(_checkpointDir, "checkpoint_*.bin")
            .OrderByDescending(f => f)
            .FirstOrDefault();

        if (latestCheckpoint == null)
            return null;

        var data = await File.ReadAllBytesAsync(latestCheckpoint);
        var checkpoint = MessagePackSerializer.Deserialize<Checkpoint>(data);

        return new TrainingState
        {
            Generation = checkpoint.Generation,
            CurrentStage = checkpoint.Stage,
            Population = checkpoint.Population.Select(NetworkGenome.Deserialize).ToList(),
            BestGenome = NetworkGenome.Deserialize(checkpoint.BestGenome),
            HistoricalStats = checkpoint.Stats
        };
    }
}
```

#### 6.5 Monitoring i wizualizacja

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        DASHBOARD MONITORINGU                             │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │  Generation: 127 | Stage: Combat-Normal | Population: 100       │   │
│  │  Best Fitness: 2847 | Avg Fitness: 1293 | Win Rate: 43%        │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                          │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │  FITNESS OVER GENERATIONS                                        │   │
│  │                                                                   │   │
│  │  3000 ┤                                            ╭─────       │   │
│  │  2500 ┤                                    ╭───────╯             │   │
│  │  2000 ┤                        ╭───────────╯                     │   │
│  │  1500 ┤            ╭───────────╯                                 │   │
│  │  1000 ┤    ╭───────╯                                             │   │
│  │   500 ┤────╯                                                     │   │
│  │     0 ┼────┬────┬────┬────┬────┬────┬────┬────┬────┬────        │   │
│  │       0   20   40   60   80  100  120                            │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                          │
│  ┌──────────────────────┐  ┌──────────────────────┐                    │
│  │  ACTIVE VMs: 8/8     │  │  GAMES COMPLETED     │                    │
│  │  ████████████        │  │  Today: 2,847        │                    │
│  │  CPU: 78%            │  │  Total: 127,493      │                    │
│  │  RAM: 24.3/32 GB     │  │  Avg time: 8m 23s    │                    │
│  └──────────────────────┘  └──────────────────────┘                    │
│                                                                          │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │  RECENT GAMES (live feed)                                        │   │
│  │  ┌──────┬────────┬─────────┬────────────┬──────────────────┐    │   │
│  │  │ VM   │ Genome │ Result  │ Duration   │ Notes            │    │   │
│  │  ├──────┼────────┼─────────┼────────────┼──────────────────┤    │   │
│  │  │ VM-3 │ G-847  │ WIN     │ 12m 34s    │ Fast rush       │    │   │
│  │  │ VM-1 │ G-823  │ LOSS    │ 18m 02s    │ Economy fail    │    │   │
│  │  │ VM-5 │ G-891  │ WIN     │ 22m 17s    │ Long game       │    │   │
│  │  │ VM-2 │ G-856  │ TIMEOUT │ 30m 00s    │ Stuck on scroll │    │   │
│  │  └──────┴────────┴─────────┴────────────┴──────────────────┘    │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

```csharp
// Blazor WebApp dla dashboard
public class TrainingDashboard
{
    [Inject] private ITrainingStateService _stateService { get; set; }

    protected override async Task OnInitializedAsync()
    {
        // Subscribe to real-time updates via SignalR
        _stateService.OnGenerationComplete += HandleGenerationComplete;
        _stateService.OnGameComplete += HandleGameComplete;
    }

    private void HandleGenerationComplete(GenerationStats stats)
    {
        // Update charts
        FitnessHistory.Add(new DataPoint(stats.Generation, stats.BestFitness));
        InvokeAsync(StateHasChanged);
    }
}
```

### Sugerowane technologie

| Komponent | Technologia | Uzasadnienie |
|-----------|-------------|--------------|
| Training Loop | C# Background Service | Integracja z ekosystemem |
| Dashboard | Blazor Server | Real-time, C# |
| Charts | Chart.js lub Plotly | Interaktywne wykresy |
| Persistence | SQLite + MessagePack | Lokalne, szybkie |
| Real-time | SignalR | Wbudowane w .NET |

### Komendy Claude Code

```bash
# Training loop
claude "Zaimplementuj TrainingPipeline z CurriculumManager i CheckpointManager"

# Tournament system
claude "Zaimplementuj TournamentManager z ELO rating i Swiss-system pairing"

# Dashboard
claude "Utwórz Blazor Server App dla monitoringu treningu z SignalR updates"

# Visualizations
claude "Dodaj Chart.js wykresy fitness over generations, win rate, population diversity"
```

### Szacowany nakład pracy: **L** (Large)

### Zależności
- Faza 3 (Algorytm genetyczny)
- Faza 4 (Infrastruktura VM)
- Faza 5 (Detekcja wyniku)

### Metryki sukcesu
- [ ] Pipeline działa nieprzerwanie przez 24h
- [ ] Checkpoint/restore działa poprawnie
- [ ] Dashboard pokazuje real-time status
- [ ] Fitness rośnie przez pierwsze 100 generacji

### Potencjalne ryzyka i mitygacja

| Ryzyko | Prawdopodobieństwo | Wpływ | Mitygacja |
|--------|-------------------|-------|-----------|
| Sieci nigdy nie nauczą się podstaw | Średnie | Krytyczny | Reward shaping, curriculum design |
| Self-play prowadzi do "meta" | Wysokie | Średni | Regularny test vs AI, diversity bonus |
| Długi czas treningu | Wysokie | Średni | Więcej VM, optymalizacja inference |

### MVP Fazy 6
1. Prosty loop: generacja → ewaluacja → selekcja → nowa generacja
2. Bez curriculum (stały przeciwnik Easy AI)
3. Checkpoint co 10 generacji
4. Konsolowy output zamiast dashboard

---

## Podsumowanie technologii

### Tabela głównych technologii

| Obszar | Technologia | Wersja | Licencja |
|--------|-------------|--------|----------|
| **Język główny** | C# / .NET | 8.0 | MIT |
| **Screen Capture** | SharpDX / Vortice.Windows | Latest | MIT |
| **Image Processing** | OpenCvSharp4 | 4.8.x | Apache 2.0 |
| **ML Inference** | ONNX Runtime | 1.16.x | MIT |
| **Serialization** | MessagePack-CSharp | 2.x | MIT |
| **Database** | SQLite | 3.x | Public Domain |
| **IPC** | System.IO.Pipes | Built-in | MIT |
| **Virtualization** | Hyper-V | Windows Pro | Windows License |
| **IaC** | PowerShell / Terraform | Latest | MIT |
| **Dashboard** | Blazor Server | 8.0 | MIT |
| **Charts** | Chart.js | 4.x | MIT |
| **Real-time** | SignalR | 8.0 | MIT |

### Alternatywne technologie (jeśli potrzebne)

| Podstawowa | Alternatywa | Kiedy użyć alternatywy |
|------------|-------------|------------------------|
| ONNX Runtime | TensorFlow.NET | Jeśli potrzebny GPU training |
| SharpDX | Vortice.Windows | Lepsze wsparcie dla .NET 8 |
| SQLite | LiteDB | Jeśli potrzebny document store |
| Blazor Server | React + ASP.NET API | Zespół zna React |
| Hyper-V | Docker + Wine | Cross-platform development |

### Diagram komponentów

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         ARCHITEKTURA SYSTEMU                             │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ┌────────────────────────────────────────────────────────────────┐    │
│  │                        HOST MACHINE                             │    │
│  │                                                                  │    │
│  │  ┌─────────────────────────────────────────────────────────┐   │    │
│  │  │                 ORCHESTRATOR SERVICE                     │   │    │
│  │  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐     │   │    │
│  │  │  │   Training  │  │  Checkpoint │  │  Curriculum │     │   │    │
│  │  │  │   Pipeline  │  │   Manager   │  │   Manager   │     │   │    │
│  │  │  └─────────────┘  └─────────────┘  └─────────────┘     │   │    │
│  │  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐     │   │    │
│  │  │  │  Genetic    │  │  Tournament │  │    VM       │     │   │    │
│  │  │  │  Algorithm  │  │   Manager   │  │   Manager   │     │   │    │
│  │  │  └─────────────┘  └─────────────┘  └─────────────┘     │   │    │
│  │  └─────────────────────────────────────────────────────────┘   │    │
│  │                              │                                   │    │
│  │                      ┌───────┴───────┐                          │    │
│  │                      │    SQLite     │                          │    │
│  │                      │   Database    │                          │    │
│  │                      └───────────────┘                          │    │
│  │                              │                                   │    │
│  │              ┌───────────────┼───────────────┐                  │    │
│  │              │               │               │                  │    │
│  │              ▼               ▼               ▼                  │    │
│  │  ┌────────────────────────────────────────────────────────┐    │    │
│  │  │                   HYPER-V LAYER                         │    │    │
│  │  │                                                          │    │    │
│  │  │  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐    │    │    │
│  │  │  │    VM #1     │ │    VM #2     │ │    VM #N     │    │    │    │
│  │  │  │              │ │              │ │              │    │    │    │
│  │  │  │ ┌──────────┐ │ │ ┌──────────┐ │ │ ┌──────────┐ │    │    │    │
│  │  │  │ │  TZAR    │ │ │ │  TZAR    │ │ │ │  TZAR    │ │    │    │    │
│  │  │  │ │  GAME    │ │ │ │  GAME    │ │ │ │  GAME    │ │    │    │    │
│  │  │  │ └────┬─────┘ │ │ └────┬─────┘ │ │ └────┬─────┘ │    │    │    │
│  │  │  │      │       │ │      │       │ │      │       │    │    │    │
│  │  │  │ ┌────▼─────┐ │ │ ┌────▼─────┐ │ │ ┌────▼─────┐ │    │    │    │
│  │  │  │ │   BOT    │ │ │ │   BOT    │ │ │ │   BOT    │ │    │    │    │
│  │  │  │ │INTERFACE │ │ │ │INTERFACE │ │ │ │INTERFACE │ │    │    │    │
│  │  │  │ └────┬─────┘ │ │ └────┬─────┘ │ │ └────┬─────┘ │    │    │    │
│  │  │  │      │       │ │      │       │ │      │       │    │    │    │
│  │  │  │ ┌────▼─────┐ │ │ ┌────▼─────┐ │ │ ┌────▼─────┐ │    │    │    │
│  │  │  │ │ NEURAL   │ │ │ │ NEURAL   │ │ │ │ NEURAL   │ │    │    │    │
│  │  │  │ │ NETWORK  │ │ │ │ NETWORK  │ │ │ │ NETWORK  │ │    │    │    │
│  │  │  │ │(Genome A)│ │ │ │(Genome B)│ │ │ │(Genome N)│ │    │    │    │
│  │  │  │ └──────────┘ │ │ └──────────┘ │ │ └──────────┘ │    │    │    │
│  │  │  │              │ │              │ │              │    │    │    │
│  │  │  │ ┌──────────┐ │ │ ┌──────────┐ │ │ ┌──────────┐ │    │    │    │
│  │  │  │ │  STATE   │ │ │ │  STATE   │ │ │ │  STATE   │ │    │    │    │
│  │  │  │ │ DETECTOR │ │ │ │ DETECTOR │ │ │ │ DETECTOR │ │    │    │    │
│  │  │  │ └──────────┘ │ │ └──────────┘ │ │ └──────────┘ │    │    │    │
│  │  │  └──────────────┘ └──────────────┘ └──────────────┘    │    │    │
│  │  └────────────────────────────────────────────────────────┘    │    │
│  │                                                                  │    │
│  │  ┌─────────────────────────────────────────────────────────┐   │    │
│  │  │                    MONITORING                            │   │    │
│  │  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐     │   │    │
│  │  │  │   Blazor    │  │   SignalR   │  │   Chart.js  │     │   │    │
│  │  │  │  Dashboard  │  │    Hub      │  │   Graphs    │     │   │    │
│  │  │  └─────────────┘  └─────────────┘  └─────────────┘     │   │    │
│  │  └─────────────────────────────────────────────────────────┘   │    │
│  └────────────────────────────────────────────────────────────────┘    │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Następne kroki

### Checklisty dla każdej fazy

#### Faza 1: Interfejs bota
- [ ] Utworzyć solution .NET 8 z projektami
- [ ] Zaimplementować ScreenCapture z DXGI
- [ ] Zaimplementować InputInjector
- [ ] Zaimplementować Named Pipe server
- [ ] Napisać testy jednostkowe
- [ ] Przetestować z uruchomioną grą Tzar

#### Faza 2: Architektura sieci neuronowej
- [ ] Zdefiniować NetworkGenome class
- [ ] Zaimplementować ImagePreprocessor
- [ ] Zaimplementować NetworkBuilder (genome → ONNX)
- [ ] Zaimplementować inference z ONNX Runtime
- [ ] Przetestować round-trip serializacji

#### Faza 3: Algorytm genetyczny
- [ ] Zaimplementować GeneticAlgorithm class
- [ ] Zaimplementować operatory mutacji
- [ ] Zaimplementować crossover
- [ ] Zaimplementować selekcję turniejową
- [ ] Napisać testy dla GA

#### Faza 4: Infrastruktura Hyper-V
- [ ] Przygotować template VM z grą
- [ ] Napisać skrypty PowerShell do klonowania
- [ ] Zaimplementować VMManager
- [ ] Zaimplementować Orchestrator
- [ ] Przetestować z 2 VM równolegle

#### Faza 5: Detekcja wyniku gry
- [ ] Przechwycić template'y ekranów
- [ ] Zaimplementować GameStateDetector
- [ ] Zaimplementować GameMonitor
- [ ] Przetestować z różnymi scenariuszami

#### Faza 6: Protokół uczenia
- [ ] Zaimplementować TrainingPipeline
- [ ] Zaimplementować CurriculumManager
- [ ] Zaimplementować CheckpointManager
- [ ] Zaimplementować TournamentManager
- [ ] Utworzyć dashboard Blazor
- [ ] Uruchomić pierwszy pełny trening

### Proponowana kolejność implementacji

```
Tydzień 1-2:   Faza 1 (Interfejs) - MVP
               │
               ├─────────────────────────────────────┐
               │                                     │
Tydzień 3-4:   Faza 2 (Sieć neuronowa) ────────── Faza 5 (Detekcja wyniku)
               │                                     │
               │                                     │
Tydzień 5-6:   Faza 3 (GA) ──────────────────────────┤
               │                                     │
               │                                     │
Tydzień 7-10:  Faza 4 (Hyper-V) ─────────────────────┤
               │                                     │
               │                                     │
Tydzień 11-14: Faza 6 (Training Pipeline) ───────────┘
               │
               ▼
              Pierwszy trening na pełnej infrastrukturze
```

### Kamienie milowe

1. **Milestone 1**: Bot wykonuje kliknięcia w grze (koniec Fazy 1)
2. **Milestone 2**: Sieć neuronowa podejmuje decyzje na podstawie screenshota (koniec Fazy 2)
3. **Milestone 3**: Populacja sieci ewoluuje (koniec Fazy 3)
4. **Milestone 4**: Trening działa równolegle na 4+ VM (koniec Fazy 4+5)
5. **Milestone 5**: Bot wygrywa z Easy AI w >50% gier (sukces Fazy 6)
6. **Milestone 6**: Bot wygrywa z Hard AI (pełny sukces projektu)

### Rekomendacje

1. **Zacznij od MVP każdej fazy** - nie buduj pełnej funkcjonalności od razu
2. **Testuj na prawdziwej grze wcześnie** - nie zakładaj, że wszystko zadziała
3. **Loguj wszystko** - debugging rozproszonego systemu jest trudny
4. **Zapisuj checkpointy często** - trening może trwać dni
5. **Rozważ reinforcement learning** - jeśli GA nie przyniesie wyników po 200 generacjach, rozważ przejście na RL (PPO/A3C)

---

*Dokument wygenerowany: 2025-12-06*
*Wersja: 1.0*
