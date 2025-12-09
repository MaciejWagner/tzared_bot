# Demo Fazy 6: Training Pipeline

**Wersja:** 1.0
**Data utworzenia:** 2025-12-09
**Ostatnia aktualizacja:** 2025-12-09
**Status:** SCENARIUSZE GOTOWE (wymaga uruchomienia na VM)

---

## Przeglad

Ten dokument zawiera kompletne instrukcje do przeprowadzenia demonstracji funkcjonalnosci Fazy 6 projektu TzarBot. Demo pokazuje dzialanie wszystkich komponentow Training Pipeline:
- Training Loop Core
- Curriculum Manager
- Checkpoint Manager
- Tournament System
- Blazor Dashboard z SignalR

---

## Prerekvizity

### Wymagania sprzetowe
- System Windows 10 (wersja 1803+) lub Windows 11
- Minimum 8 GB RAM (16 GB zalecane dla pelnego demo z VM)
- Hyper-V wlaczony (dla testow z VM)

### Wymagania programowe
- .NET 8.0 SDK zainstalowany
- Git (do sklonowania repozytorium)
- Przegladarka (Chrome/Edge/Firefox) do Dashboard

### Zaleznosci od innych faz
- Phase 1: Game Interface (COMPLETED)
- Phase 2: Neural Network (COMPLETED)
- Phase 3: Genetic Algorithm (COMPLETED)
- Phase 4: Hyper-V Infrastructure (COMPLETED - 5/6)
- Phase 5: Game State Detection (COMPLETED)

### Weryfikacja prerekvizytow

```powershell
# Sprawdz wersje .NET
dotnet --version
# Oczekiwana: 8.0.x lub nowsza

# Sprawdz projekty
cd C:\Users\maciek\ai_experiments\tzar_bot
dotnet build TzarBot.sln --verbosity minimal
# Oczekiwana: Build succeeded
```

---

## Scenariusze demo

### Scenariusz 1: Training Loop Demo (bez VM)

**Cel:** Demonstracja dzialania glownej petli treningowej z mockowanymi danymi.

**Kroki:**

1. **Zbuduj projekt:**
```powershell
cd C:\Users\maciek\ai_experiments\tzar_bot
dotnet build TzarBot.sln
```

2. **Uruchom testy Training Loop:**
```powershell
dotnet test src\TzarBot.Training.Tests --filter "FullyQualifiedName~TrainingLoop" --verbosity normal
```

3. **Oczekiwany wynik:**
```
Passed!  - Failed:     0, Passed:    18, Skipped:     0, Total:    18
```

**Kryteria sukcesu:**
| # | Kryterium | Sposob weryfikacji | Status |
|---|-----------|-------------------|--------|
| 1 | Testy TrainingLoop PASS | Wszystkie 18 testow przechodza | [ ] |
| 2 | Generacje sa tworzone | Log pokazuje "Generation X started" | [ ] |
| 3 | Fitness jest obliczany | Log pokazuje "Fitness calculated" | [ ] |

---

### Scenariusz 2: Curriculum Manager Demo

**Cel:** Demonstracja przechodzenia przez etapy curriculum.

**Kroki:**

1. **Uruchom testy Curriculum Manager:**
```powershell
dotnet test src\TzarBot.Training.Tests --filter "FullyQualifiedName~Curriculum" --verbosity normal
```

2. **Oczekiwany wynik:**
```
Passed!  - Failed:     0, Passed:    12, Skipped:     0, Total:    12
```

3. **Weryfikacja etapow:**
```
Stage 0: Bootstrap (Passive AI) -> min survival 60s
Stage 1: Basic (Easy AI) -> min win rate 30%
Stage 2: Combat (Normal AI) -> min win rate 50%
Stage 3: Tournament (Self-play) -> ELO ranking
```

**Kryteria sukcesu:**
| # | Kryterium | Sposob weryfikacji | Status |
|---|-----------|-------------------|--------|
| 1 | Testy Curriculum PASS | Wszystkie 12 testow przechodza | [ ] |
| 2 | Etapy sa poprawne | 4 etapy zdefiniowane | [ ] |
| 3 | Przechodzenie dziala | Test AdvanceStage przechodzi | [ ] |

---

### Scenariusz 3: Checkpoint Manager Demo

**Cel:** Demonstracja zapisywania i odtwarzania stanu treningu.

**Kroki:**

1. **Uruchom testy Checkpoint Manager:**
```powershell
dotnet test src\TzarBot.Training.Tests --filter "FullyQualifiedName~Checkpoint" --verbosity normal
```

2. **Oczekiwany wynik:**
```
Passed!  - Failed:     0, Passed:    10, Skipped:     0, Total:    10
```

3. **Weryfikacja funkcjonalnosci:**
- Zapis checkpointu do pliku
- Odczyt checkpointu z pliku
- Rotacja starych checkpointow (max 10)
- Backup najlepszego genomu

**Kryteria sukcesu:**
| # | Kryterium | Sposob weryfikacji | Status |
|---|-----------|-------------------|--------|
| 1 | Testy Checkpoint PASS | Wszystkie 10 testow przechodza | [ ] |
| 2 | Zapis dziala | Plik checkpoint_*.bin tworzony | [ ] |
| 3 | Odczyt dziala | Stan poprawnie przywrocony | [ ] |

---

### Scenariusz 4: Tournament System Demo

**Cel:** Demonstracja systemu turniejowego z ELO rating.

**Kroki:**

1. **Uruchom testy Tournament System:**
```powershell
dotnet test src\TzarBot.Training.Tests --filter "FullyQualifiedName~Tournament" --verbosity normal
```

2. **Oczekiwany wynik:**
```
Passed!  - Failed:     0, Passed:    15, Skipped:     0, Total:    15
```

3. **Weryfikacja funkcjonalnosci:**
- Swiss-system pairing
- ELO calculation (K-factor = 32)
- Round-robin dla malych populacji (<8)
- Ranking po rundzie

**Kryteria sukcesu:**
| # | Kryterium | Sposob weryfikacji | Status |
|---|-----------|-------------------|--------|
| 1 | Testy Tournament PASS | Wszystkie 15 testow przechodza | [ ] |
| 2 | ELO calculation | Wygrana zwieksza ELO | [ ] |
| 3 | Pairing dziala | Gracze sparowani poprawnie | [ ] |

---

### Scenariusz 5: Blazor Dashboard Demo

**Cel:** Demonstracja dashboardu monitoringu w czasie rzeczywistym.

**Kroki:**

1. **Uruchom Dashboard:**
```powershell
cd C:\Users\maciek\ai_experiments\tzar_bot
dotnet run --project src\TzarBot.Dashboard
```

2. **Oczekiwany output:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Application started.
```

3. **Otworz przegladarke:**
- Przejdz do: http://localhost:5000
- Dashboard powinien sie zaladowac

4. **Weryfikacja komponentow:**
- Status treningu (generacja, etap)
- Wykres fitness (Chart.js)
- Lista VM (aktywne/nieaktywne)
- Feed ostatnich gier

5. **Test SignalR:**
- Otworz Developer Tools (F12) -> Console
- Powinny byc logi SignalR connection

**Kryteria sukcesu:**
| # | Kryterium | Sposob weryfikacji | Status |
|---|-----------|-------------------|--------|
| 1 | Dashboard laduje sie | Strona wyswietla UI | [ ] |
| 2 | Komponenty widoczne | Status, wykresy, VM list | [ ] |
| 3 | SignalR polaczony | Console pokazuje connected | [ ] |
| 4 | Real-time update | Dane odswieczaja sie automatycznie | [ ] |

---

### Scenariusz 6: Pelna integracja (wymaga VM)

**UWAGA:** Ten scenariusz wymaga uruchomionej maszyny wirtualnej DEV.

**Cel:** Demonstracja pelnego pipeline od startu do checkpointu.

**Prerekvizity:**
- VM DEV uruchomiona i dostepna
- Tzar zainstalowany na VM
- Siec skonfigurowana

**Kroki:**

1. **Sprawdz dostepnosc VM:**
```powershell
Test-Connection -ComputerName 192.168.100.10 -Count 1
```

2. **Uruchom Training na VM:**
```powershell
# Na VM DEV
cd C:\TzarBot
dotnet run --project src\TzarBot.Training -- --population 10 --generations 5
```

3. **Monitoruj w Dashboard:**
- Otworz http://localhost:5000
- Obserwuj postep generacji
- Sprawdz wykresy fitness

4. **Weryfikacja checkpointu:**
```powershell
# Na VM DEV
ls C:\TzarBot\checkpoints\
# Powinny byc pliki checkpoint_gen_*.bin
```

**Kryteria sukcesu:**
| # | Kryterium | Sposob weryfikacji | Status |
|---|-----------|-------------------|--------|
| 1 | VM dostepna | Ping zwraca odpowiedz | [ ] |
| 2 | Training startuje | Log pokazuje "Training started" | [ ] |
| 3 | Generacje przechodza | 5 generacji ukonczonych | [ ] |
| 4 | Checkpoint zapisany | Plik checkpoint istnieje | [ ] |
| 5 | Dashboard pokazuje status | Real-time updates widoczne | [ ] |

---

## Uruchomienie testow jednostkowych (wszystkie)

```powershell
cd C:\Users\maciek\ai_experiments\tzar_bot
dotnet test src\TzarBot.Training.Tests --verbosity normal
```

**Oczekiwany wynik:**
```
Passed!  - Failed:     0, Passed:    55, Skipped:     0, Total:    55
```

```powershell
dotnet test src\TzarBot.Dashboard.Tests --verbosity normal
```

**Oczekiwany wynik:**
```
Passed!  - Failed:     0, Passed:    35, Skipped:     0, Total:    35
```

**Lacznie:** ~90 testow PASS

---

## Zbieranie dowodow

### Zrzuty ekranu do zebrania

1. **Dashboard overview** - glowny widok dashboardu
2. **Fitness chart** - wykres fitness over generations
3. **VM status** - lista VM z statusami
4. **Test results** - wynik `dotnet test`
5. **SignalR connection** - console z logami

### Logi do zebrania

```powershell
# Utworz katalog na dowody
mkdir project_management\demo\phase_6_evidence

# Zapisz output testow
dotnet test src\TzarBot.Training.Tests 2>&1 | Out-File -FilePath project_management\demo\phase_6_evidence\training_tests.log

dotnet test src\TzarBot.Dashboard.Tests 2>&1 | Out-File -FilePath project_management\demo\phase_6_evidence\dashboard_tests.log
```

---

## Rozwiazywanie problemow

### Problem: Dashboard nie startuje

**Objawy:**
```
Unable to bind to http://localhost:5000
```

**Rozwiazanie:**
- Sprawdz czy port 5000 nie jest zajety: `netstat -ano | findstr :5000`
- Zmien port w `appsettings.json`

### Problem: SignalR nie laczy sie

**Objawy:**
```
WebSocket connection failed
```

**Rozwiazanie:**
- Sprawdz firewall (port 5000)
- Uzyj Edge/Chrome (Firefox moze miec problemy z WebSocket)

### Problem: Testy failuja z timeout

**Objawy:**
```
Test exceeded timeout of 30000ms
```

**Rozwiazanie:**
- Zwieksz timeout w testach
- Sprawdz obciazenie CPU

---

## Raport z uruchomienia na VM

> **UWAGA:** Ta sekcja zostanie wypelniona po uruchomieniu demo na VM DEV.

### Informacje o srodowisku

| Pole | Wartosc |
|------|---------|
| VM Name | [TBD] |
| VM IP | [TBD] |
| RAM | [TBD] |
| Data uruchomienia | [TBD] |
| Wykonawca | [TBD] |

### Status: PENDING

### Screenshoty

| # | Opis | Plik | Status |
|---|------|------|--------|
| 1 | Dashboard overview | `phase_6_evidence/01_dashboard.png` | PENDING |
| 2 | Fitness chart | `phase_6_evidence/02_fitness.png` | PENDING |
| 3 | VM status | `phase_6_evidence/03_vm_status.png` | PENDING |
| 4 | Test results | `phase_6_evidence/04_tests.png` | PENDING |
| 5 | Training logs | `phase_6_evidence/05_training.png` | PENDING |

### Logi

| Log | Opis | Plik | Status |
|-----|------|------|--------|
| Training Tests | Output z testow Training | `phase_6_evidence/training_tests.log` | PENDING |
| Dashboard Tests | Output z testow Dashboard | `phase_6_evidence/dashboard_tests.log` | PENDING |
| Dashboard Run | Output z uruchomienia Dashboard | `phase_6_evidence/dashboard_run.log` | PENDING |

### Wyniki

| # | Kryterium | Oczekiwany wynik | Rzeczywisty wynik | Status |
|---|-----------|------------------|-------------------|--------|
| 1 | Training Tests | 55/55 PASS | [TBD] | PENDING |
| 2 | Dashboard Tests | 35/35 PASS | [TBD] | PENDING |
| 3 | Dashboard UI | Laduje sie poprawnie | [TBD] | PENDING |
| 4 | SignalR | Polaczony | [TBD] | PENDING |
| 5 | Real-time updates | Dzialaja | [TBD] | PENDING |

---

## Podsumowanie kryteriow sukcesu

### Minimalne (MUST PASS)

| # | Kryterium | Sposob weryfikacji | Status |
|---|-----------|-------------------|--------|
| 1 | Projekt buduje sie | `dotnet build` zwraca 0 bledow | [ ] |
| 2 | Training Tests PASS | 55/55 testow przechodzi | [ ] |
| 3 | Dashboard Tests PASS | 35/35 testow przechodzi | [ ] |
| 4 | Dashboard laduje sie | UI wyswietla sie w przegladarce | [ ] |
| 5 | SignalR dziala | Logi w console potwierdzaja | [ ] |

### Rozszerzone (SHOULD PASS)

| # | Kryterium | Sposob weryfikacji | Status |
|---|-----------|-------------------|--------|
| 6 | Real-time updates | Dashboard odswieza sie | [ ] |
| 7 | Training na VM | Pipeline dziala na VM | [ ] |
| 8 | Checkpoint saved | Plik checkpoint istnieje | [ ] |

---

## Powiazane dokumenty

| Dokument | Sciezka |
|----------|---------|
| Backlog Phase 6 | `project_management/backlog/phase_6_backlog.md` |
| Plan szczegolowy | `plans/phase_6_detailed.md` |
| Kod Training | `src/TzarBot.Training/` |
| Kod Dashboard | `src/TzarBot.Dashboard/` |
| Testy Training | `src/TzarBot.Training.Tests/` |
| Testy Dashboard | `src/TzarBot.Dashboard.Tests/` |

---

## Historia wersji

| Wersja | Data | Autor | Zmiany |
|--------|------|-------|--------|
| 1.0 | 2025-12-09 | Agent PM | Utworzenie dokumentu z scenariuszami demo |
