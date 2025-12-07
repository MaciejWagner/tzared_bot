# Demo Fazy 1: Game Interface

**Wersja:** 1.0
**Data utworzenia:** 2025-12-07
**Status:** READY FOR DEMO

---

## Przeglad

Ten dokument zawiera kompletne instrukcje do przeprowadzenia demonstracji funkcjonalnosci Fazy 1 projektu TzarBot. Demo pokazuje dzialanie wszystkich komponentow Game Interface:
- Przechwytywanie ekranu (DXGI Desktop Duplication)
- Wstrzykiwanie inputu (SendInput API)
- Komunikacja IPC (Named Pipes)
- Wykrywanie okna (Win32 API)

---

## Prerekvizity

### Wymagania sprzetowe
- System Windows 10 (wersja 1803+) lub Windows 11
- Karta graficzna z obsluga DXGI 1.2+
- Minimum 8 GB RAM
- Rozdzielczosc ekranu: 1920x1080 (zalecana)

### Wymagania programowe
- .NET 10 SDK zainstalowany
- Git (do sklonowania repozytorium)
- Visual Studio 2022 lub VS Code z C# extension (opcjonalnie)

### Weryfikacja prerekvizytow

Uruchom ponizsze komendy, aby zweryfikowac srodowisko:

```powershell
# Sprawdz wersje .NET
dotnet --version
# Oczekiwana: 10.0.x

# Sprawdz wersje Windows
[System.Environment]::OSVersion.Version
# Oczekiwana: 10.0.x lub nowsza
```

---

## Instrukcja krok po kroku

### Krok 1: Sklonowanie repozytorium

```powershell
git clone https://github.com/TWOJ_USER/tzar_bot.git
cd tzar_bot
```

Lub jesli repozytorium juz istnieje:

```powershell
cd C:\Users\maciek\ai_experiments\tzar_bot
```

### Krok 2: Zbudowanie projektu

```powershell
dotnet restore TzarBot.slnx
dotnet build TzarBot.slnx --configuration Release
```

**Oczekiwany wynik:**
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### Krok 3: Uruchomienie testow jednostkowych

```powershell
dotnet test TzarBot.slnx --verbosity normal
```

**Oczekiwany wynik:**
```
Passed!  - Failed:     0, Passed:    46, Skipped:     0, Total:    46
```

### Krok 4: Demo przechwytywania ekranu

Uruchom aplikacje demo:

```powershell
dotnet run --project src\TzarBot.GameInterface.Demo
```

**Oczekiwany wynik:**
```
=== TzarBot Game Interface Demo ===

[1] Screen Capture Demo
[2] Input Injection Demo (requires open Notepad)
[3] Window Detection Demo
[4] IPC Communication Demo
[5] Full Integration Demo
[0] Exit

Select option:
```

Wybierz opcje `1` (Screen Capture Demo).

**Oczekiwany wynik:**
```
Starting Screen Capture Demo...
Initializing DXGI Screen Capture...
Screen resolution: 1920x1080
Capturing 10 frames...
Frame 1: 1920x1080, 8294400 bytes, 0ms
Frame 2: 1920x1080, 8294400 bytes, 16ms
...
Frame 10: 1920x1080, 8294400 bytes, 152ms
Average FPS: 65.8
Screenshots saved to: screenshots/
Demo complete.
```

**Weryfikacja:**
- Sprawdz katalog `screenshots/` - powinny byc pliki PNG
- Otworz jeden z plikow - powinien zawierac zrzut ekranu

### Krok 5: Demo wstrzykiwania inputu

1. Najpierw otworz Notatnik (Notepad):
```powershell
notepad.exe
```

2. W aplikacji demo wybierz opcje `2` (Input Injection Demo).

**Oczekiwany wynik:**
```
Starting Input Injection Demo...
Please ensure Notepad is open and focused.
Press Enter to continue...

Finding Notepad window...
Found: Notepad - Untitled (Handle: 0x12345)
Setting foreground...
Waiting 1 second...
Typing test text...
Moving mouse...
Clicking...
Demo complete.
```

**Weryfikacja:**
- W Notatniku powinien pojawic sie tekst: "TzarBot Input Test 123"
- Kursor myszy powinien poruszyc sie

### Krok 6: Demo wykrywania okna

Wybierz opcje `3` (Window Detection Demo).

**Oczekiwany wynik:**
```
Starting Window Detection Demo...
Enumerating visible windows...

Found 25 windows:
1. Program Manager (Handle: 0x10010)
2. Notepad - Untitled (Handle: 0x12345)
3. Windows PowerShell (Handle: 0x23456)
...

Testing FindWindow("Notepad")...
Found: Notepad - Untitled
  Handle: 0x12345
  Bounds: X=100, Y=100, Width=800, Height=600
  Client: X=0, Y=0, Width=784, Height=562
  Visible: True
  Focused: False

Demo complete.
```

### Krok 7: Demo komunikacji IPC

Wybierz opcje `4` (IPC Communication Demo).

**Oczekiwany wynik:**
```
Starting IPC Communication Demo...
Starting Pipe Server...
Server started on pipe: TzarBot

Starting Pipe Client in separate thread...
Client connecting...
Client connected!

Server: Sending test frame (1920x1080, 8MB)...
Client: Received frame (1920x1080, 8294400 bytes)

Client: Sending test action (LeftClick at 500,300)...
Server: Received action: LeftClick at (500,300)

Round-trip time: 48ms
Demo complete.
```

### Krok 8: Pelna integracja (opcjonalne - wymaga gry Tzar)

Jesli masz zainstalowana gre Tzar:

1. Uruchom gre Tzar
2. W aplikacji demo wybierz opcje `5` (Full Integration Demo)

**Oczekiwany wynik:**
```
Starting Full Integration Demo...

Looking for Tzar window...
Found: Tzar - The Burden of the Crown
  Resolution: 1920x1080

Starting capture...
Captured 60 frames in 5 seconds (12 FPS)

Testing input injection...
Moving mouse to center of game window...
Clicking...

Demo complete. Check if game responded to click.
```

---

## Kryteria sukcesu

### Minimalne (MUST PASS)

| # | Kryterium | Sposob weryfikacji | Status |
|---|-----------|-------------------|--------|
| 1 | Projekt buduje sie bez bledow | `dotnet build` zwraca 0 bledow | [ ] |
| 2 | Wszystkie 46 testow przechodzi | `dotnet test` pokazuje 46 PASS | [ ] |
| 3 | Screen Capture dziala | Demo zapisuje poprawne PNG | [ ] |
| 4 | Input Injection dziala | Tekst pojawia sie w Notepad | [ ] |
| 5 | Window Detection dziala | Znajduje Notepad/Tzar | [ ] |
| 6 | IPC dziala | Klient/Serwer komunikuja sie | [ ] |

### Rozszerzone (SHOULD PASS)

| # | Kryterium | Sposob weryfikacji | Status |
|---|-----------|-------------------|--------|
| 7 | Capture FPS >= 10 | Sredni FPS w demo | [ ] |
| 8 | IPC round-trip < 100ms | Czas w demo | [ ] |
| 9 | Dzialanie z gra Tzar | Full Integration Demo | [ ] |

---

## Zbieranie dowodow

### Zrzuty ekranu do zebrania

1. **Build output** - wynik `dotnet build`
2. **Test output** - wynik `dotnet test`
3. **Screen capture** - przykladowy zrzut z katalogu `screenshots/`
4. **Notepad test** - Notatnik z wpisanym tekstem
5. **IPC demo** - output z demo IPC

### Logi do zebrania

```powershell
# Zapisz output buildu
dotnet build TzarBot.slnx 2>&1 | Out-File -FilePath demo_evidence/build.log

# Zapisz output testow
dotnet test TzarBot.slnx 2>&1 | Out-File -FilePath demo_evidence/tests.log

# Zapisz output demo
dotnet run --project src\TzarBot.GameInterface.Demo 2>&1 | Out-File -FilePath demo_evidence/demo.log
```

---

## Rozwiazywanie problemow

### Problem: Build fails z bledem Vortice

**Objawy:**
```
error CS0246: The type or namespace name 'Vortice' could not be found
```

**Rozwiazanie:**
```powershell
dotnet restore TzarBot.slnx --force
```

### Problem: Screen Capture zwraca pusty bufor

**Objawy:**
```
Frame data is all zeros
```

**Mozliwe przyczyny:**
1. Secure desktop jest aktywny (np. UAC dialog)
2. Remote Desktop bez GPU acceleration

**Rozwiazanie:**
- Zamknij wszystkie dialogi UAC
- Uzyj fizycznego dostep do maszyny (nie RDP)

### Problem: Input Injection nie dziala w grze

**Objawy:**
Gra nie reaguje na klikniecia/klawisze

**Mozliwe przyczyny:**
1. Gra nie jest focused
2. Gra wymaga uprawnien administratora

**Rozwiazanie:**
```powershell
# Uruchom demo jako administrator
Start-Process powershell -Verb RunAs -ArgumentList "-Command dotnet run --project src\TzarBot.GameInterface.Demo"
```

### Problem: IPC timeout

**Objawy:**
```
Connection timeout after 5000ms
```

**Rozwiazanie:**
- Upewnij sie, ze zadna inna instancja nie uzywa pipe "TzarBot"
- Sprawdz Windows Firewall

---

## Czyszczenie po demo

```powershell
# Usun tymczasowe pliki
Remove-Item -Path screenshots/* -Force
Remove-Item -Path demo_evidence/* -Force

# Zamknij wszystkie procesy demo
Get-Process -Name "TzarBot*" | Stop-Process -Force
```

---

## Powiazane dokumenty

| Dokument | Sciezka |
|----------|---------|
| Raport ukonczenia Phase 1 | `reports/2_phase1_completion_report.md` |
| Backlog Phase 1 | `project_management/backlog/phase_1_backlog.md` |
| Plan szczegolowy | `plans/phase_1_detailed.md` |
| Kod zrodlowy | `src/TzarBot.GameInterface/` |
| Testy | `tests/TzarBot.Tests/Phase1/` |

---

## Historia wersji

| Wersja | Data | Autor | Zmiany |
|--------|------|-------|--------|
| 1.0 | 2025-12-07 | Agent PM | Utworzenie dokumentu |
