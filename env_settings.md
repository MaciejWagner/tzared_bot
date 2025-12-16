# TzarBot Environment Settings

**Ostatnia aktualizacja:** 2025-12-14

> Konfiguracja dla lokalnego treningu z GPU (bez VM).

---

## Host Machine

| Setting | Value | Notes |
|---------|-------|-------|
| OS | Windows 11 | Development + training |
| .NET Version | 8.0 | Required for TzarBot |
| GPU | NVIDIA (wspiera WebGPU) | Wymagane dla tza.red |

---

## Browser Configuration

tza.red wymaga WebGPU. Na hoście z GPU przeglądarka powinna działać natywnie.

| Browser | Status | Notes |
|---------|--------|-------|
| Edge | Preferowany | Najlepsza kompatybilność z tza.red |
| Chrome | OK | Alternatywa |
| Firefox | Nie działa | Stuck na "Loading" |

**Fallback (bez GPU):** SwiftShader (30x wolniejszy)
```csharp
Args = new[] {
    "--use-gl=swiftshader",
    "--enable-unsafe-swiftshader"
}
```

---

## Paths

| Component | Path | Notes |
|-----------|------|-------|
| Project Root | C:\Users\maciek\ai_experiments\tzar_bot | |
| Source Code | src\ | |
| Training Maps | training\maps\ | .tzared files |
| ONNX Models | training\generation_*\ | Network files |
| Training Results | training\generation_*\results\ | JSON results |

---

## Training Configuration

| Setting | Value | Notes |
|---------|-------|-------|
| Population Size | 20 | Networks per generation |
| Trials per Network | 30 | Evaluations for fitness |
| Trial Duration | 20-60s | Game time |
| Map | training-0.tzared | Standard training map |

---

## Ports & Services

| Service | Port | Protocol |
|---------|------|----------|
| Dashboard | 5000 | HTTP |
| SignalR Hub | 5001 | WebSocket |

---

## Version Info

| Component | Version |
|-----------|---------|
| .NET SDK | 8.0.x |
| ONNX Runtime | 1.16.3 |
| OpenCvSharp4 | 4.8.0 |
| Playwright | 1.40.0 |

---

## History

| Date | Change |
|------|--------|
| 2025-12-07 | Initial file created |
| 2025-12-14 | Simplified for local GPU training (removed VM sections) |
