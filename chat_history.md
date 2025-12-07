# Chat History

## Sesja 1 - 2025-12-06

### User:
zaproponuj prompt, który pozwoli jakościowo wykonać poniższe zadanie. WAZNE : NIE wykonuj zadania, a jedynie zaproponuj prompt, który je wykona. po wszystkim zapisz je w prompts/1_planning_prompt Zadanie do spromptowania: chciałbym stworzyć bota do gry strategicznej https://tza.red/ . Zaplanuj stworzenie bota, z podziałem na fazy. Fazy,które mam w głowie na ten moment, to: 1 interfejs dla bota, czyli prosty program uruchamiany obok gry, który przejmuje przekazuje do gry ruchy myszką, kliknięcia, klawiaturę, a pobiera zrzuty ekranu. 2 propozycja wstępnej sieci neuronowej, czyli warstwa wejściowa którą jest screenshot z interfejsu. Warstwy ukryte, rozpoznające co jest na ekranie, sytuację w grze i warstwa wyjściowa, która przekazuje komendy do interfejsu. 3 algorytm genetyczny manipulujący warstwami ukrytymi dodający bądź usuwajacy kolejne warstwy, oraz zmieniający ilość neuronów w warstwach ukrytych 4 algorytm uruchamiajacy konkretne sieci neuronowe na hyper V, z podstawowym windowsem 4 funkcja która zczytuje wygraną bądź przegraną 5 protokół uczenia sieci, czyli na poczatku każda sieć próbuje po prostu manipulowac mapą, te które dokonają manipulacji przechodza dalej, następnie sieci mają walczyć z SI na różnych poziomach trudności, te które na 20 walk mają co najmniej 10 wygrancyh przechodza dalej ostatni punkt, to sieci walczą same ze sobą. Zaprponuj szczegółowy plan stworzenia bota w punktach, zaprponuj jakie komendy cloude użyć w którym punkcie, oraz podejście technologiczne do kolejnych punktów. Moje preferowane technologie, to c#, c, puppet/terraform, ale jestem w stanie się dostosowac, jeżeli wymagany bedzi enp tenserflow. Wygenerowany plan zapisz do pliku plan.md

### Assistant:
Prompt został zapisany w `prompts/1_planning_prompt.md`.

Zawiera on:
- **Pełny kontekst** zadania z opisem wszystkich 6 faz
- **Szczegółowe wymagania** dla każdej fazy (interfejs, sieć neuronowa, algorytm genetyczny, Hyper-V, detekcja wyniku, protokół uczenia)
- **Wytyczne technologiczne** (C#, C, Puppet/Terraform jako preferowane, z otwartością na TensorFlow)
- **Strukturę oczekiwanego planu** (zależności, MVP, metryki sukcesu)
- **Konkretne komendy Claude Code** do użycia w poszczególnych punktach
- **Format wyjściowy** (Markdown z diagramami, tabelami, checklistami)

Aby użyć tego prompta, wystarczy go skopiować i wkleić do nowej sesji Claude Code lub uruchomić z tego pliku.

---

### User:
wykonaj prompt prompts\1_planning_prompt.md

### Assistant:
Wykonano prompt planowania. Utworzono plik `plans/1general_plan.md` zawierający kompleksowy plan budowy bota AI do gry Tzar.

**Struktura planu:**
- Spis treści z linkami do sekcji
- Diagram zależności między 6 fazami (ASCII art)
- Szczegółowy opis każdej fazy z:
  - Architekturą i diagramami
  - Przykładami kodu C#
  - Tabelą sugerowanych technologii
  - Komendami Claude Code
  - Szacowanym nakładem pracy (S/M/L/XL)
  - Zależnościami, ryzykami, metrykami sukcesu
  - MVP dla szybkiego startu

**Kluczowe decyzje technologiczne:**
- C# / .NET 8 jako główny język
- ONNX Runtime dla inference sieci neuronowej
- SharpDX/Vortice dla screen capture (DXGI)
- OpenCvSharp4 dla przetwarzania obrazu
- Hyper-V + PowerShell dla wirtualizacji
- Blazor Server dla dashboardu monitoringu

**6 faz projektu:**
1. Interfejs bota (screen capture + input injection)
2. Architektura sieci neuronowej (CNN + dense layers)
3. Algorytm genetyczny (mutacja wag i struktury)
4. Infrastruktura Hyper-V (VM pool + orchestrator)
5. Detekcja wyniku gry (template matching)
6. Protokół uczenia (curriculum learning + self-play)

**Kamienie milowe:** od "bot klika w grze" do "bot wygrywa z Hard AI"

---

### User:
na podstawie planu plans\1general_plan.md stwórz prompt, który: zaproponuje podejście cloude do łatwej realizacji kolejnych faz, stworzy mniejsze plany dla każdej z faz, oraz jeden duży workflow, albo inne rozwiązanie, które pozwoli łatwo zaimplementować kolejne fazy projektu. Podzieli każda faze na podzadania, możliwe do wykonania w pojedynczej sesji AI, zaproponuje automatyczną metodą testowania wyników zadań. Wygenerowany prompt zapisz w katalogu prompts, zaproponuj nazwę zaczynająca się od 2_

### Assistant:
Utworzono prompt `prompts/2_implementation_workflow_prompt.md` zawierający:

**Struktura prompta:**
- **Część 1: Struktura podzadań** - szablon YAML dla atomowych tasków z inputs/outputs/test_command
- **Część 2: System automatycznego testowania** - katalog `tests/` + skrypt `validate_phase.ps1`
- **Część 3: Workflow Claude Code** - 4 opcje podejścia:
  - A: Sekwencyjne prompty w `prompts/phase_X/`
  - B: Master workflow file z checklistą
  - C: Slash commands w `.claude/commands/`
  - D: Hybrid z hooks do auto-walidacji
- **Część 4: Szczegółowe plany** - osobne pliki `plans/phase_X_detailed.md`
- **Część 5: Metryki i raportowanie** - `progress.json` + raporty

**Kluczowe elementy:**
- Każdy task wykonalny w jednej sesji Claude Code (30-60 min)
- Automatyczne testy dla każdego taska
- Izolacja tasków - kontekst przez pliki, nie pamięć
- Przykład szczegółowego taska (F1.T2 Screen Capture) z pełnym szablonem
- Struktura katalogów dla całego workflow

**Wynikowa struktura plików:**
```
plans/phase_X_detailed.md (6 plików)
prompts/phase_X/*.md (prompty per task)
scripts/validate_phase.ps1
tests/TzarBot.Tests/
reports/progress.json
```

---

### User:
wykonaj prompts\2_implementation_workflow_prompt.md

### Assistant:
Wykonano workflow implementacyjny. Utworzono kompletną strukturę plików:

**Plany szczegółowe dla każdej fazy:**
- `plans/2_implementation_workflow.md` - główny dokument workflow
- `plans/phase_1_detailed.md` - Game Interface (6 tasków: F1.T1-F1.T6)
- `plans/phase_2_detailed.md` - Neural Network (5 tasków: F2.T1-F2.T5)
- `plans/phase_3_detailed.md` - Genetic Algorithm (5 tasków: F3.T1-F3.T5)
- `plans/phase_4_detailed.md` - Hyper-V Infrastructure (6 tasków: F4.T1-F4.T6)
- `plans/phase_5_detailed.md` - Game State Detection (4 tasków: F5.T1-F5.T4)
- `plans/phase_6_detailed.md` - Training Pipeline (6 tasków: F6.T1-F6.T6)

**Skrypty walidacyjne:**
- `scripts/validate_phase.ps1` - walidacja pojedynczej fazy/taska
- `scripts/run_all_tests.ps1` - uruchomienie wszystkich testów
- `scripts/generate_progress_report.ps1` - generowanie raportu postępu

**Pliki promptów dla Phase 1:**
- `prompts/phase_1/F1.T1_project_setup.md`
- `prompts/phase_1/F1.T2_screen_capture.md`
- `prompts/phase_1/F1.T3_input_injection.md`
- `prompts/phase_1/F1.T4_ipc_pipes.md`
- `prompts/phase_1/F1.T5_window_detection.md`
- `prompts/phase_1/F1.T6_integration.md`

**System raportowania:**
- `reports/progress.json` - śledzenie postępu wszystkich 32 tasków

**Każdy task zawiera:**
- YAML z inputs/outputs/test_command/dependencies
- Szczegółowy prompt dla Claude Code
- Kryteria walidacji i test_criteria
- Rollback plan w razie niepowodzenia
- Dokumentację API

**Podejście:** Wybrano Opcję D (Hybrid) - slash commands + hooks + walidacja

**Jak używać:**
1. `.\scripts\generate_progress_report.ps1` - sprawdź następny task
2. Wykonaj prompt z `prompts/phase_X/FX.TY_*.md`
3. `.\scripts\validate_phase.ps1 -Phase X -Task Y` - zwaliduj wynik
4. Kontynuuj do następnego taska

**Łącznie:** 32 taski do wykonania w sekwencji z automatyczną walidacją.

---

### User [2025-12-07 12:30]:
dodaj do logowania w chat history godzinę wykonania requestu

### Assistant:
Zaktualizowano `CLAUDE.md` - dodano format godziny do Chat History Protocol. Nowy format nagłówka: `### User [YYYY-MM-DD HH:MM]:` zamiast samego `### User:`.

---

### User [2025-12-07 12:32]:
dodaj slash command prompt który mówi "stwórz prompt, który zrealizuje poniższy opis. Stworzony prompt zapisz w katalogu prompts z kolejnym numerem i nazwą"

### Assistant:
Utworzono slash command `.claude/commands/prompt.md`. Użycie: `/prompt [opis zadania]` - stworzy prompt realizujący opis i zapisze go w `prompts/` z kolejnym numerem.

---

### User [2025-12-07 12:35]:
zaproponuj subagenta audytu tasków pod kątem możliwości uruchomienia projektu, z możliwością dodania dodatkowego zadania

### Assistant:
Utworzono slash command `.claude/commands/audit.md`. Użycie: `/audit [dodatkowe instrukcje]` - przeprowadza audyt tasków sprawdzając: środowisko testowe, kolejność tasków, luki w workflow, brakujące prerekvizity, krytyczne ścieżki. Generuje raport z oceną gotowości projektu.

---

### User [2025-12-07 16:40]:
/audit zwróć szczególną uwagę na rzeczy dodatkowe, jak np brak gry tzared, chciałbym aby środowisko testowe było na osobnej maszynie na hyper V

### Assistant:
Przeprowadzono kompleksowy audyt workflow. **Ocena: WYMAGA POPRAWEK** - workflow nie może być uruchomiony.

**Problemy krytyczne:**
1. Brak instrukcji pozyskania gry Tzar
2. Brak Development VM (środowisko testowe na Hyper-V)
3. Niekompletne instrukcje Template VM (F4.T1)
4. Brak skryptu `scripts/vm/New-TzarWorkerVM.ps1`

---

### User [2025-12-07 16:50]:
najpierw uzupełnij plany. Instalator gry tzar został dodany do katalogu files.

### Assistant:
Uzupełniono plany zgodnie z audytem. Utworzone pliki:

1. **`plans/phase_0_prerequisites.md`** - prerekvizity projektu (F0.T1-F0.T4)
2. **`plans/phase_4_template_setup.md`** - szczegółowy przewodnik Template VM
3. **`scripts/vm/*.ps1`** - 6 skryptów PowerShell do zarządzania VMs
4. Zaktualizowano `phase_1_detailed.md`, `phase_4_detailed.md`, `2_implementation_workflow.md`

**Instalator gry:** `files/tzared.windows.zip` (86.7 MB)

---

### User [2025-12-07 17:00]:
/agent-optimizer (przerwane) tam gdzie jest to potrzebne dodaj wyczyszczenie contextu, jeśli agent nie posiada tego bez potrzeby. sprawdź, czy umiejętności skills są wypełnione tam gdzie są wymagane

### Assistant:
Uruchomiono audyt zasobów AI. Zmiany:
- Dodano parametr `tools` do agentów `task-audit-workflow.md` i `ai-resource-optimizer.md`
- Naprawiono błędną regułę w `settings.local.json`
- Zachowano model opus dla wszystkich agentów (jakość > koszt)

---

### User [2025-12-07 17:05]:
optymalizacja pod kontem zasobów nie powinna być ponad szczegółowością projektu i jakością wykonania.

### Assistant:
Potwierdzono - zachowano model opus dla wszystkich agentów. Jakość wykonania ma priorytet nad oszczędnością kosztów.

---

### User [2025-12-07 17:10]:
wyniki audytu zasobów oraz sugestie większego wykorzystania narzędzi zapisz potem w podkatalogu reports z prefixem 1_

### Assistant:
Utworzono raport `reports/1_ai_resource_audit.md` zawierający wyniki audytu zasobów AI i sugestie optymalizacji.

---

### User [2025-12-07 17:15]:
(przerwane) wykonaj następne kroki z raportu, tylko przy tworzeniu agentów dodaj prefix tzarbot-agent- Agentom dodaj wymagane skille.

### Assistant:
Utworzono 4 wyspecjalizowanych agentów z skills:
- `.claude/agents/tzarbot-agent-dotnet-senior.md` (skills: csharp, dotnet, windows-api)
- `.claude/agents/tzarbot-agent-ai-senior.md` (skills: machine-learning, neural-networks, genetic-algorithms, onnx)
- `.claude/agents/tzarbot-agent-fullstack-blazor.md` (skills: blazor, signalr, chartjs, css)
- `.claude/agents/tzarbot-agent-hyperv-admin.md` (skills: powershell, hyper-v, windows-server, networking)

Dodatkowo utworzono:
- `reports/1_vision_testing_guide.md`
- `reports/1_cost_monitoring.md`
- Zaktualizowano `CLAUDE.md` z nowymi agentami

---

### User [2025-12-07 18:00]:
uruchom workflow tzarbot

### Assistant:
Rozpoczęto implementację Phase 1: Game Interface.

**F1.T1 - Project Setup:**
- Utworzono solution .NET 10 (użytkownik ma .NET 10, nie .NET 8)
- Projekty: TzarBot.Common, TzarBot.GameInterface, TzarBot.Tests
- Dodano NuGet packages: Vortice, OpenCvSharp4, MessagePack, FluentAssertions, Moq
- Utworzono Directory.Build.props, .editorconfig
- Modele: ScreenFrame.cs, GameAction.cs

**F1.T2 - Screen Capture:**
- IScreenCapture.cs, DxgiScreenCapture.cs, ScreenCaptureException.cs
- ScreenCaptureTests.cs (8 testów)
- Naprawiono API Vortice (EnumOutputs zamiast GetOutput, uint casting)

**F1.T3 - Input Injection:**
- VirtualKey.cs, NativeMethods.cs, IInputInjector.cs, Win32InputInjector.cs
- InputInjectorTests.cs (11 testów)
- Dodano AllowUnsafeBlocks dla P/Invoke

**F1.T4 - IPC Named Pipes:**
- Protocol.cs, IPipeServer.cs, IPipeClient.cs, PipeServer.cs, PipeClient.cs
- IpcTests.cs (8 testów)
- MessagePack serialization z atrybutami [MessagePackObject]

**F1.T5 - Window Detection:**
- WindowInfo.cs, IWindowDetector.cs, WindowDetector.cs, TzarWindow.cs
- WindowDetectorTests.cs (12 testów)
- Naprawiono P/Invoke (partial class, Unicode entry points)

---

### User [2025-12-07 (kontynuacja z poprzedniej sesji)]:
(kontynuacja workflow po przerwaniu kontekstu)

### Assistant:
Kontynuowano implementację F1.T6 - Integration Tests:
- Utworzono TzarBot.GameInterface.Demo (console app)
- Program.cs z demo wszystkich komponentów
- Naprawiono P/Invoke dla WindowDetector (GetWindowTextLengthW, GetWindowTextW, GetClassNameW)
- Usunięto StringBuilder, zastąpiono char[] bufferami

**Build:** SUCCESS
**Tests:** 46/46 PASS

**Phase 1: Game Interface - COMPLETED!**

Zaktualizowano workflow_progress.md - wszystkie taski F1.T1-F1.T6 ukończone.

---

### User [2025-12-07]:
zapisz raport w katalogu reports i dodaj brakujące wiadomości do chat history

### Assistant:
Utworzono `reports/2_phase1_completion_report.md` - kompletny raport z ukończonej Phase 1 zawierający:
- Summary wszystkich 6 tasków
- Strukturę projektu
- Technology stack
- Performance metrics
- API overview
- Lista 30 utworzonych plików

Zaktualizowano `chat_history.md` z brakującymi wiadomościami od ostatniej aktualizacji.

---

### User [2025-12-07 18:30]:
Wykonaj pełny prompt agent-project-manager.
1. Faza 1 projektu TzarBot została już UKOŃCZONA
2. Przy każdej fazie muszą być zapisane dane projektowe
3. Uzupełnij brakujące materiały dla Fazy 1

### Assistant:
Wykonano pełny setup Project Management dla projektu TzarBot. Utworzono kompletną strukturę dokumentacji w katalogu `project_management/`:

**Backlogi faz (7 plików):**
- `project_management/backlog/phase_0_backlog.md` - Prerequisites (4 taski, PENDING)
- `project_management/backlog/phase_1_backlog.md` - Game Interface (6 tasków, COMPLETED)
- `project_management/backlog/phase_2_backlog.md` - Neural Network (5 tasków, PENDING)
- `project_management/backlog/phase_3_backlog.md` - Genetic Algorithm (5 tasków, PENDING)
- `project_management/backlog/phase_4_backlog.md` - Hyper-V Infrastructure (6 tasków, PENDING)
- `project_management/backlog/phase_5_backlog.md` - Game State Detection (4 taski, PENDING)
- `project_management/backlog/phase_6_backlog.md` - Training Pipeline (6 tasków, PENDING)

**Dokumenty zarzadzania:**
- `project_management/gantt.md` - Wykres Gantta w Mermaid z harmonogramem i ścieżką krytyczną
- `project_management/timetracking.md` - Metryki czasowe, velocity, burndown chart
- `project_management/progress_dashboard.md` - Dashboard z aktualnym statusem (17% ukończonych)
- `project_management/project_overview.md` - Przegląd całego projektu

**Dokumentacja demo:**
- `project_management/demo/phase_1_demo.md` - Kompletna instrukcja demo dla Fazy 1 z krokami, kryteriami sukcesu i troubleshootingiem

**Podsumowanie:**
- 36 tasków w 7 fazach zdefiniowanych
- 6 tasków ukończonych (Faza 1: 100%)
- 46 testów jednostkowych przechodzi
- Szacowany czas: ~50 dni roboczych do końca projektu
- Ścieżka krytyczna: F0 -> F4 -> F6

---

### User [2025-12-07 ~10:00-13:00]:
kontynuuj workflow (Phase 0 + Demo)

### Assistant:
Kontynuowano workflow od Phase 0. Wykonano:

**Phase 0: Prerequisites - COMPLETED**
- F0.T1: Host Machine Setup - Hyper-V enabled, TzarBotSwitch created, NAT configured
- F0.T2: VM DEV created - Windows 10 Pro, .NET 8.0.416
- F0.T3: Tzar game installed from files/tzared.windows.zip
- F0.T4: Environment verified - network connectivity OK
- F0.T5: Infrastructure documented (env_settings.md, docs/infrastructure.md)

**Demo Execution na VM DEV:**
- Utworzono skrypty demo: Run-Phase0Demo.ps1, Run-Phase1Demo.ps1, Run-AllDemos.ps1
- Naprawiono kompatybilność: net10.0 → net8.0, Vortice 3.8.1 → 3.6.2
- Utworzono klasyczny TzarBot.sln (zamiast .slnx)
- Uruchomiono demo automatycznie przez PowerShell Direct
- Phase 0: 7/7 testów PASS
- Phase 1: 5/7 testów PASS (Build OK, moduły wykryte)
- Wyniki skopiowane do demo_results/

**Pliki utworzone/zmodyfikowane:**
- `env_settings.md` - konfiguracja środowiska
- `docs/infrastructure.md` - dokumentacja infrastruktury
- `scripts/demo/*.ps1` - skrypty demo
- `scripts/deploy_to_vm.ps1` - deployment na VM
- `scripts/copy_results_from_vm.ps1` - pobieranie wyników
- `TzarBot.sln` - klasyczny plik solution
- Wszystkie csproj - zmiana na net8.0 + RollForward

---

### User [2025-12-07 13:25]:
zapisz raport, plik continue, oraz zrób update chat_history. Jeżeli jest wymagane zaktualizuj cloude.md

### Assistant:
Zapisano:
- `reports/3_demo_execution_report.md` - pełny raport z wykonania demo
- `continue.md` - zaktualizowany status workflow
- `chat_history.md` - dodane wpisy z sesji
- `workflow_progress.md` - zaktualizowany progress
- `project_management/demo/phase_0_demo.md` - dokumentacja demo Phase 0
- `project_management/demo/phase_1_demo.md` - zaktualizowana z wynikami z VM

---

### User [2025-12-07 15:15]:
Ponowne uruchomienie demo Phase 0 i Phase 1 z pelnym zbieraniem dowodow.
Audyt delivery wykazal braki w dokumentacji demo: brak screenshotow, wyniki testow wymagaja weryfikacji na VM.

### Assistant:
Wykonano ponowne uruchomienie demo na VM DEV:

**Utworzone skrypty:**
- `scripts/demo/Run-VMDemoWithScreenshots.ps1` - glowny skrypt uruchamiajacy demo
- `scripts/demo/Capture-VMScreenshots.ps1` - skrypt do zbierania screenshotow przez Enhanced Session
- `scripts/demo/Configure-VMAutoDemo.ps1` - konfiguracja AutoLogon dla automatycznego demo

**Wyniki demo 2025-12-07 15:22:**
- Phase 0: 7/7 testow PASS (100%)
  - System Info, Network IP, Gateway, Internet, .NET SDK, Tzar Game, Disk Space
- Phase 1: 35/46 testow PASS (76.1%)
  - Build: 0 errors, 0 warnings
  - 11 failures srodowiskowych (9x ScreenCapture, 2x WindowDetection)
  - IPC: wszystkie testy przeszly (poprawa z poprzedniego uruchomienia)

**Logi zebrane:**
- `project_management/demo/phase_0_evidence/phase0_demo_2025-12-07_15-22-36.log`
- `project_management/demo/phase_0_evidence/phase0_report_2025-12-07_15-22-36.md`
- `project_management/demo/phase_1_evidence/build_2025-12-07_15-22-48.log`
- `project_management/demo/phase_1_evidence/tests_2025-12-07_15-22-48.log`
- `project_management/demo/phase_1_evidence/phase1_demo_2025-12-07_15-22-48.log`
- `project_management/demo/phase_1_evidence/phase1_report_2025-12-07_15-22-48.md`

**Dokumentacja zaktualizowana:**
- `project_management/demo/phase_0_demo.md` (v1.1)
- `project_management/demo/phase_1_demo.md` (v1.4)

**Uwaga dot. screenshotow:**
Screenshoty nie zostaly zebrane - PowerShell Direct nie ma dostepu do sesji graficznej.
Dla screenshotow wymagane jest uruchomienie przez RDP/Enhanced Session z uzytkownikiem zalogowanym interaktywnie.

---

### User [2025-12-07 22:00]:
continue

### Assistant:
Kontynuacja workflow Phase 2: Neural Network Architecture.

**F2.T1: NetworkGenome & Serialization - COMPLETED**

Agent tzarbot-agent-ai-senior zaimplementował kompletny moduł genomu sieci neuronowej:

**Pliki utworzone:**
- `src/TzarBot.NeuralNetwork/TzarBot.NeuralNetwork.csproj` - nowy projekt
- `src/TzarBot.NeuralNetwork/Models/ActivationType.cs` - enum typów aktywacji
- `src/TzarBot.NeuralNetwork/Models/ConvLayerConfig.cs` - konfiguracja warstw konwolucyjnych
- `src/TzarBot.NeuralNetwork/Models/DenseLayerConfig.cs` - konfiguracja warstw gęstych
- `src/TzarBot.NeuralNetwork/Models/NetworkGenome.cs` - główna klasa genomu
- `src/TzarBot.NeuralNetwork/Models/NetworkConfig.cs` - domyślna konfiguracja sieci
- `src/TzarBot.NeuralNetwork/GenomeSerializer.cs` - serializacja MessagePack
- `tests/TzarBot.Tests/NeuralNetwork/NetworkGenomeTests.cs` - 14 testów

**Kluczowe funkcjonalności:**
- NetworkGenome z frozen ConvLayers i evolved HiddenLayers
- Xavier initialization dla wag
- MessagePack serialization/deserialization
- Clone(), TotalWeightCount(), IsValid()
- Output heads: MousePosition (2 neurony, Tanh) + ActionType (30 neuronów, Softmax)

**Wyniki:**
- Build: SUCCESS (0 errors, 0 warnings)
- Tests: 14/14 PASS (NetworkGenome tests)

Rozpoczęto F2.T2: Image Preprocessor...

---
