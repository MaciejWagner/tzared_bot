# Browser Testing Results for tza.red

**Data testów:** 2025-12-14
**Środowisko:** Hyper-V VM (DEV) z GPU-PV (NVIDIA)

## Problem

Gra tza.red wymaga WebGPU do renderowania. Hyper-V VM nie ma natywnego wsparcia WebGPU nawet z włączonym GPU Partitioning (GPU-PV).

## Wyniki testów przeglądarek

| Przeglądarka | Flagi | Ładowanie mapy | Screenshoty | Gra działa | Status |
|--------------|-------|----------------|-------------|------------|--------|
| Chromium (default) | brak | ❌ Nie ładuje | ~0.8s | ❌ | NIE DZIAŁA |
| Firefox | brak | ❌ Stuck na "Loading" | ~0.8s | ❌ | NIE DZIAŁA |
| Edge (default) | brak | ✅ | 3-5s | ❌ Błąd WebGPU | NIE DZIAŁA |
| Edge + ANGLE D3D11 | `--use-angle=d3d11` | ✅ | - | ❌ Błąd WebGPU | NIE DZIAŁA |
| Edge + ANGLE D3D11on12 | `--use-angle=d3d11on12` | ✅ | - | ❌ Błąd WebGPU | NIE DZIAŁA |
| Edge + SwiftShader | `--use-gl=swiftshader` | ✅ | 3-5s | ✅ | **DZIAŁA** |

## Błędy WebGPU

Bez SwiftShader, gra pada z błędem:
```
[Browser warning] No available adapters.
[Browser PageError] TypeError: Cannot read properties of null (reading 'getError')
    at bk (https://tza.red/nehhon/netral.js?v=649:384:132)
```

## Konfiguracja działająca

```csharp
_browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
{
    Headless = false,
    Channel = "msedge",
    Args = new[]
    {
        "--start-maximized",
        "--disable-infobars",
        "--no-sandbox",
        "--use-gl=swiftshader",           // Software GL - jedyna działająca opcja
        "--enable-unsafe-swiftshader"     // Wymagane dla Edge
    }
});

var context = await _browser.NewContextAsync(new BrowserNewContextOptions
{
    ViewportSize = new ViewportSize { Width = 1280, Height = 720 },  // Mniejszy = szybszy
    IgnoreHTTPSErrors = true
});
```

## Wydajność SwiftShader

| Metryka | Wartość |
|---------|---------|
| Czas screenshota | 3-5 sekund |
| Actions per second | ~0.2 APS |
| Stosunek czasu gry do realnego | ~1:30 |
| Czas na 20s w grze | ~10 minut |

### Przykładowe wyniki

```json
{
  "Outcome": "TIMEOUT",
  "ActualDurationSeconds": 121.86,
  "TotalFrames": 29,
  "TotalActions": 26,
  "AverageInferenceMs": 252.6,
  "ActionsPerSecond": 0.21
}
```

Timer w grze po 120s realnych: 0:04 (4 sekundy w grze)

## Wnioski

1. **SwiftShader jest jedyną działającą opcją** dla tza.red w Hyper-V VM
2. **GPU-PV nie pomaga** - tza.red wymaga WebGPU, które nie jest wspierane przez GPU-PV
3. **Trening będzie bardzo wolny** - każdy trial wymaga ~10 minut dla 20s gry
4. **Alternatywy:**
   - Uruchomienie na fizycznej maszynie z GPU
   - Użycie WSL2 z GPU passthrough
   - Modyfikacja mapy treningowej (krótszy czas do DEFEAT)

## Rekomendacje

1. **Krótkoterminowo:** Zaakceptować wolne tempo, uruchomić trening z dłuższymi sesjami
2. **Długoterminowo:** Rozważyć:
   - Trening na fizycznej maszynie
   - Użycie headless Chrome z GPU na Linux
   - Stworzenie prostszej mapy treningowej

## Pliki konfiguracyjne

- `src/TzarBot.BrowserInterface/PlaywrightGameInterface.cs` - konfiguracja przeglądarki
- `tools/TrainingRunner/Program.cs` - runner treningu
