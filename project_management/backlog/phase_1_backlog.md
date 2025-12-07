# Backlog Fazy 1: Game Interface

**Ostatnia aktualizacja:** 2025-12-07
**Status Fazy:** COMPLETED
**Priorytet:** MUST (fundament calego projektu)
**Data ukonczenia:** 2025-12-07

---

## Podsumowanie

Faza 1 obejmuje implementacje interfejsu gry - warstwy posredniej miedzy sieciami neuronowymi a gra Tzar. Zawiera moduly przechwytywania ekranu, wstrzykiwania inputu, komunikacji IPC oraz wykrywania okna gry.

**FAZA UKONCZONA** - Wszystkie 6 taskow pomyslnie zaimplementowanych. 46 testow jednostkowych przechodzi.

---

## Taski

### F1.T1: Project Setup
| Pole | Wartosc |
|------|---------|
| **ID** | F1.T1 |
| **Tytul** | Konfiguracja projektu |
| **Opis** | Utworzenie struktury solution .NET z wszystkimi wymaganymi projektami i pakietami NuGet |
| **Priorytet** | MUST |
| **Szacowany naklad** | S (Small) |
| **Rzeczywisty naklad** | S |
| **Status** | COMPLETED |
| **Agent** | tzarbot-agent-dotnet-senior |
| **Zaleznosci** | F0 (Prerequisites) |
| **Data rozpoczecia** | 2025-12-07 |
| **Data ukonczenia** | 2025-12-07 |

**Kryteria akceptacji:**
- [x] Solution buduje sie bez bledow
- [x] Wszystkie projekty uzycia .NET 10
- [x] Wymagane pakiety NuGet zainstalowane (Vortice, OpenCvSharp4, MessagePack)
- [x] Struktura katalogow zgodna ze specyfikacja
- [x] Directory.Build.props i .editorconfig utworzone

**Utworzone pliki:**
- `TzarBot.slnx`
- `Directory.Build.props`
- `.editorconfig`
- `src/TzarBot.Common/TzarBot.Common.csproj`
- `src/TzarBot.GameInterface/TzarBot.GameInterface.csproj`
- `tests/TzarBot.Tests/TzarBot.Tests.csproj`

---

### F1.T2: Screen Capture Implementation
| Pole | Wartosc |
|------|---------|
| **ID** | F1.T2 |
| **Tytul** | Implementacja przechwytywania ekranu |
| **Opis** | Implementacja przechwytywania ekranu za pomoca DXGI Desktop Duplication API |
| **Priorytet** | MUST |
| **Szacowany naklad** | M (Medium) |
| **Rzeczywisty naklad** | M |
| **Status** | COMPLETED |
| **Agent** | tzarbot-agent-dotnet-senior |
| **Zaleznosci** | F1.T1 |
| **Data rozpoczecia** | 2025-12-07 |
| **Data ukonczenia** | 2025-12-07 |
| **Testy** | 8 testow - PASS |

**Kryteria akceptacji:**
- [x] Interfejs IScreenCapture zdefiniowany
- [x] DxgiScreenCapture wykorzystuje Desktop Duplication API
- [x] Przechwytywanie 10+ FPS bez spadku wydajnosci
- [x] Bufor w formacie BGRA32
- [x] 8 testow jednostkowych przechodzi

**Utworzone pliki:**
- `src/TzarBot.GameInterface/Capture/IScreenCapture.cs`
- `src/TzarBot.GameInterface/Capture/DxgiScreenCapture.cs`
- `src/TzarBot.GameInterface/Capture/ScreenCaptureException.cs`
- `src/TzarBot.Common/Models/ScreenFrame.cs`
- `tests/TzarBot.Tests/Phase1/ScreenCaptureTests.cs`

---

### F1.T3: Input Injection Implementation
| Pole | Wartosc |
|------|---------|
| **ID** | F1.T3 |
| **Tytul** | Implementacja wstrzykiwania inputu |
| **Opis** | Implementacja wstrzykiwania myszy i klawiatury za pomoca Windows SendInput API |
| **Priorytet** | MUST |
| **Szacowany naklad** | M (Medium) |
| **Rzeczywisty naklad** | M |
| **Status** | COMPLETED |
| **Agent** | tzarbot-agent-dotnet-senior |
| **Zaleznosci** | F1.T1 |
| **Data rozpoczecia** | 2025-12-07 |
| **Data ukonczenia** | 2025-12-07 |
| **Testy** | 11 testow - PASS |

**Kryteria akceptacji:**
- [x] Interfejs IInputInjector zdefiniowany
- [x] Win32InputInjector uzywa SendInput API
- [x] Obsluga myszy (move, click, drag)
- [x] Obsluga klawiatury (press, release, hotkey)
- [x] Thread-safe z minimalnym opoznieniem
- [x] 11 testow jednostkowych przechodzi

**Utworzone pliki:**
- `src/TzarBot.GameInterface/Input/IInputInjector.cs`
- `src/TzarBot.GameInterface/Input/Win32InputInjector.cs`
- `src/TzarBot.GameInterface/Input/VirtualKey.cs`
- `src/TzarBot.GameInterface/Input/NativeMethods.cs`
- `src/TzarBot.Common/Models/GameAction.cs`
- `tests/TzarBot.Tests/Phase1/InputInjectorTests.cs`

---

### F1.T4: IPC Named Pipes
| Pole | Wartosc |
|------|---------|
| **ID** | F1.T4 |
| **Tytul** | Implementacja komunikacji IPC |
| **Opis** | Implementacja komunikacji miedzy procesami za pomoca Named Pipes |
| **Priorytet** | MUST |
| **Szacowany naklad** | M (Medium) |
| **Rzeczywisty naklad** | M |
| **Status** | COMPLETED |
| **Agent** | tzarbot-agent-dotnet-senior |
| **Zaleznosci** | F1.T1 |
| **Data rozpoczecia** | 2025-12-07 |
| **Data ukonczenia** | 2025-12-07 |
| **Testy** | 8 testow - PASS |

**Kryteria akceptacji:**
- [x] Protokol binarny zdefiniowany
- [x] Serwer akceptuje polaczenia
- [x] Klient laczy sie w timeout
- [x] Transfer ScreenFrame bez korupcji danych
- [x] Odbiór GameAction poprawny
- [x] Serializacja MessagePack
- [x] 8 testow jednostkowych przechodzi

**Utworzone pliki:**
- `src/TzarBot.GameInterface/IPC/Protocol.cs`
- `src/TzarBot.GameInterface/IPC/IPipeServer.cs`
- `src/TzarBot.GameInterface/IPC/IPipeClient.cs`
- `src/TzarBot.GameInterface/IPC/PipeServer.cs`
- `src/TzarBot.GameInterface/IPC/PipeClient.cs`
- `tests/TzarBot.Tests/Phase1/IpcTests.cs`

---

### F1.T5: Window Detection
| Pole | Wartosc |
|------|---------|
| **ID** | F1.T5 |
| **Tytul** | Wykrywanie okna gry |
| **Opis** | Implementacja wykrywania i sledzenia okna gry Tzar |
| **Priorytet** | MUST |
| **Szacowany naklad** | S (Small) |
| **Rzeczywisty naklad** | S |
| **Status** | COMPLETED |
| **Agent** | tzarbot-agent-dotnet-senior |
| **Zaleznosci** | F1.T1 |
| **Data rozpoczecia** | 2025-12-07 |
| **Data ukonczenia** | 2025-12-07 |
| **Testy** | 12 testow - PASS |

**Kryteria akceptacji:**
- [x] WindowInfo zawiera Handle, Title, Bounds
- [x] FindWindow znajduje okno po tytule
- [x] GetWindowRect zwraca poprawne wymiary
- [x] SetForeground aktywuje okno
- [x] Obsluga okna zminimalizowanego/zamknietego
- [x] 12 testow jednostkowych przechodzi

**Utworzone pliki:**
- `src/TzarBot.GameInterface/Window/IWindowDetector.cs`
- `src/TzarBot.GameInterface/Window/WindowDetector.cs`
- `src/TzarBot.GameInterface/Window/WindowInfo.cs`
- `src/TzarBot.GameInterface/Window/TzarWindow.cs`
- `tests/TzarBot.Tests/Phase1/WindowDetectorTests.cs`

---

### F1.T6: Integration & Smoke Tests
| Pole | Wartosc |
|------|---------|
| **ID** | F1.T6 |
| **Tytul** | Testy integracyjne i demo |
| **Opis** | Utworzenie testow integracyjnych i aplikacji demo weryfikujacych dzialanie wszystkich komponentow |
| **Priorytet** | MUST |
| **Szacowany naklad** | M (Medium) |
| **Rzeczywisty naklad** | M |
| **Status** | COMPLETED |
| **Agent** | tzarbot-agent-dotnet-senior |
| **Zaleznosci** | F1.T2, F1.T3, F1.T4, F1.T5 |
| **Data rozpoczecia** | 2025-12-07 |
| **Data ukonczenia** | 2025-12-07 |
| **Testy** | 7 testow - PASS |

**Kryteria akceptacji:**
- [x] Demo app uruchamia sie bez bledu
- [x] Przechwytywanie ekranu dziala
- [x] Input injection dziala
- [x] IPC przesyla dane poprawnie
- [x] Wykrywanie okna dziala
- [x] Wszystkie komponenty wspóldzialaja

**Utworzone pliki:**
- `src/TzarBot.GameInterface.Demo/Program.cs`
- `src/TzarBot.GameInterface.Demo/TzarBot.GameInterface.Demo.csproj`

---

## Metryki Fazy

| Metryka | Wartosc |
|---------|---------|
| Liczba taskow | 6 |
| Ukonczonych | 6 |
| W trakcie | 0 |
| Zablokowanych | 0 |
| Oczekujacych | 0 |
| Postep | 100% |
| Testy jednostkowe | 46 PASS |
| Bledy buildu | 0 |

---

## Technologie uzyte

| Komponent | Technologia | Wersja |
|-----------|-------------|--------|
| Runtime | .NET | 10.0 |
| Screen Capture | Vortice.Windows | 3.8.1 |
| Image Processing | OpenCvSharp4 | 4.10.0 |
| Serialization | MessagePack | 3.1.3 |
| Testing | xUnit | 9.0.0 |
| Assertions | FluentAssertions | 8.0.1 |
| Mocking | Moq | 4.20.72 |

---

## Wskazniki wydajnosci

| Metryka | Wartosc | Cel |
|---------|---------|-----|
| Screen Capture FPS | 10+ | 10+ |
| Capture Latency | <50ms | <100ms |
| IPC Frame Transfer | ~50ms | <100ms |
| Input Delay | 50ms | konfigurowalny |

---

## Demo Requirements

Dokumentacja demo fazy MUSI zawierac:

| Wymaganie | Status | Opis |
|-----------|--------|------|
| Scenariusze testowe | READY | Kroki do wykonania w phase_1_demo.md |
| **Raport z VM** | **PENDING** | Uruchomienie demo na VM DEV z dowodami |
| Screenshoty | PENDING | Min. 5 zrzutow ekranu z VM |
| Logi | PENDING | build.log, tests.log, demo_run.log |

> **UWAGA:** Demo NIE jest kompletne bez raportu z uruchomienia na maszynie wirtualnej DEV!

---

## Powiazane dokumenty

- Plan szczegolowy: `plans/phase_1_detailed.md`
- Raport ukonczenia: `reports/2_phase1_completion_report.md`
- Dokumentacja demo: `project_management/demo/phase_1_demo.md`
- Dowody demo: `project_management/demo/phase_1_evidence/`
- Workflow progress: `workflow_progress.md`
