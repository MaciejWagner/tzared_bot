# Prompt: Workflow implementacji Tzar Bot

## Kontekst

Masz dostęp do planu projektu w `plans/1general_plan.md`. Twoim zadaniem jest stworzenie szczegółowego workflow implementacji, który:
1. Podzieli każdą fazę na podzadania możliwe do wykonania w jednej sesji Claude Code
2. Stworzy system automatycznego testowania wyników
3. Zaproponuje optymalną metodę pracy z Claude Code

## Zadanie

### Część 1: Struktura podzadań

Dla każdej z 6 faz projektu:

1. **Podziel fazę na atomowe zadania** (task), gdzie każde zadanie:
   - Można wykonać w jednej sesji Claude Code (max 30-60 minut pracy AI)
   - Ma jasno zdefiniowane wejście (pliki/dane potrzebne)
   - Ma jasno zdefiniowane wyjście (pliki/artefakty do stworzenia)
   - Ma automatyczny test weryfikujący sukces

2. **Dla każdego zadania zdefiniuj**:
   ```yaml
   task_id: "F1.T1"  # Faza.Task
   name: "Krótka nazwa"
   description: "Co dokładnie ma zostać zrobione"
   inputs:
     - "lista plików/danych potrzebnych"
   outputs:
     - "lista plików/artefaktów do stworzenia"
   test_command: "komenda do automatycznego testu"
   test_criteria: "co oznacza sukces testu"
   dependencies: ["F1.T0"]  # od jakich tasków zależy
   estimated_complexity: "S/M/L"
   claude_prompt: |
     Dokładny prompt do wklejenia w Claude Code
   ```

### Część 2: System automatycznego testowania

Zaprojektuj `tests/` katalog z:

1. **Testy jednostkowe** dla każdego modułu
2. **Testy integracyjne** między modułami
3. **Testy smoke** sprawdzające czy system działa end-to-end
4. **Skrypt walidacyjny** `scripts/validate_phase.ps1` który:
   - Przyjmuje numer fazy jako argument
   - Uruchamia wszystkie testy dla tej fazy
   - Zwraca exit code 0 jeśli wszystko przeszło
   - Generuje raport w `reports/phase_X_validation.md`

```powershell
# Przykład użycia:
.\scripts\validate_phase.ps1 -Phase 1
# Output: "Phase 1 validation: 5/5 tests passed"
```

### Część 3: Workflow Claude Code

Zaproponuj jeden z poniższych podejść (lub własne):

#### Opcja A: Sekwencyjne prompty
Seria plików `prompts/phase_X/task_Y.md` do wykonywania po kolei:
```
prompts/
├── phase_1/
│   ├── 01_project_setup.md
│   ├── 02_screen_capture.md
│   ├── 03_input_injection.md
│   └── ...
├── phase_2/
│   └── ...
```

#### Opcja B: Master workflow file
Jeden plik `workflow.md` z checklistą i promptami inline:
```markdown
## Phase 1: Game Interface
- [ ] Task 1.1: Project Setup
  ```prompt
  Utwórz solution .NET 8...
  ```
- [ ] Task 1.2: Screen Capture
  ...
```

#### Opcja C: Slash commands
Pliki `.claude/commands/` dla każdego taska:
```
.claude/commands/
├── phase1-setup.md
├── phase1-capture.md
├── phase1-input.md
└── ...
```
Użycie: `/phase1-setup`, `/phase1-capture`, etc.

#### Opcja D: Hybrid z hooks
Kombinacja powyższych + hooks do auto-walidacji:
```
.claude/hooks/
├── post-task-validation.sh  # Uruchamia testy po każdym tasku
```

### Część 4: Szczegółowe plany dla każdej fazy

Dla każdej fazy stwórz osobny plik `plans/phase_X_detailed.md` zawierający:

1. **Diagram sekwencji tasków** (ASCII/Mermaid)
2. **Checklist wszystkich tasków** z statusem
3. **Definicja Done** dla całej fazy
4. **Rollback plan** jeśli coś pójdzie nie tak
5. **Dokumentacja API** dla modułów tej fazy

### Część 5: Metryki i raportowanie

Zaprojektuj system śledzenia postępu:

```
reports/
├── progress.json          # Status wszystkich tasków
├── test_results/          # Wyniki testów per task
│   ├── F1.T1_2024-01-15.json
│   └── ...
└── daily_summary.md       # Auto-generowany raport dzienny
```

Format `progress.json`:
```json
{
  "phases": {
    "1": {
      "name": "Game Interface",
      "status": "in_progress",
      "tasks": {
        "F1.T1": {"status": "completed", "completed_at": "2024-01-15"},
        "F1.T2": {"status": "in_progress"},
        "F1.T3": {"status": "pending"}
      }
    }
  }
}
```

## Wymagania do wygenerowanego planu

1. **Granularność tasków**:
   - Każdy task powinien być wykonalny w jednej sesji AI
   - Nie więcej niż 3-4 nowe pliki na task
   - Jasne kryteria akceptacji

2. **Automatyzacja testów**:
   - Każdy task ma przynajmniej jeden automatyczny test
   - Testy można uruchomić bez manualnej interwencji
   - Testy zwracają jednoznaczny wynik (pass/fail)

3. **Izolacja tasków**:
   - Task nie powinien wymagać wiedzy z poprzednich sesji
   - Każdy prompt powinien być self-contained
   - Kontekst przekazywany przez pliki, nie pamięć

4. **Error handling**:
   - Co zrobić jeśli test nie przechodzi?
   - Jak debugować problem?
   - Kiedy wracać do poprzedniego taska?

## Format wyjściowy

Zapisz wyniki w następującej strukturze:

```
plans/
├── 2_implementation_workflow.md     # Główny dokument workflow
├── phase_1_detailed.md              # Szczegóły Fazy 1
├── phase_2_detailed.md              # Szczegóły Fazy 2
├── phase_3_detailed.md              # Szczegóły Fazy 3
├── phase_4_detailed.md              # Szczegóły Fazy 4
├── phase_5_detailed.md              # Szczegóły Fazy 5
└── phase_6_detailed.md              # Szczegóły Fazy 6

prompts/
├── phase_1/
│   ├── F1.T1_project_setup.md
│   ├── F1.T2_screen_capture.md
│   └── ...
├── phase_2/
│   └── ...
└── ...

scripts/
├── validate_phase.ps1               # Skrypt walidacyjny
├── run_all_tests.ps1                # Uruchom wszystkie testy
└── generate_progress_report.ps1     # Generuj raport postępu

tests/
├── TzarBot.Tests/                   # Projekt testów .NET
│   ├── Phase1/
│   ├── Phase2/
│   └── ...
└── integration/                     # Testy integracyjne
    └── ...
```

## Dodatkowe wytyczne

1. **Priorytetyzacja**: Zacznij od MVP każdej fazy, potem rozszerzaj
2. **Fail-fast**: Testy powinny szybko wykrywać problemy
3. **Dokumentacja**: Każdy task powinien aktualizować README lub CLAUDE.md
4. **Checkpointy**: Po każdej fazie - git commit z tagiem `phase-X-complete`
5. **Reużywalność**: Prompty powinny być na tyle ogólne, by można je było adaptować

## Przykład szczegółowego taska

```yaml
task_id: "F1.T2"
name: "Screen Capture Implementation"
description: |
  Zaimplementuj klasę ScreenCapture używającą DXGI Desktop Duplication API.
  Klasa powinna przechwytywać ekran z minimalnym narzutem CPU i zwracać
  klatki w formacie BGRA32.

inputs:
  - "src/TzarBot.GameInterface/TzarBot.GameInterface.csproj (utworzony w F1.T1)"
  - "plans/1general_plan.md (sekcja 1.1 Moduł przechwytywania ekranu)"

outputs:
  - "src/TzarBot.GameInterface/Capture/IScreenCapture.cs"
  - "src/TzarBot.GameInterface/Capture/DxgiScreenCapture.cs"
  - "src/TzarBot.GameInterface/Capture/ScreenFrame.cs"
  - "tests/TzarBot.Tests/Phase1/ScreenCaptureTests.cs"

test_command: "dotnet test tests/TzarBot.Tests --filter Phase1.ScreenCapture"

test_criteria: |
  - Testy przechodzą (exit code 0)
  - ScreenCapture.CaptureFrame() zwraca niepusty bufor
  - FPS >= 10 przez 10 sekund bez memory leak
  - Poprawny format BGRA32

dependencies: ["F1.T1"]
estimated_complexity: "M"

claude_prompt: |
  Zaimplementuj moduł Screen Capture dla projektu TzarBot.

  ## Kontekst
  Projekt znajduje się w `src/TzarBot.GameInterface/`. Potrzebuję klasy
  do przechwytywania ekranu używając DXGI Desktop Duplication API.

  ## Wymagania
  1. Utwórz interfejs `IScreenCapture` z metodą `CaptureFrame()`
  2. Zaimplementuj `DxgiScreenCapture` używając SharpDX lub Vortice.Windows
  3. Zwracaj `ScreenFrame` zawierający: byte[] Data, int Width, int Height, long Timestamp
  4. Osiągnij minimum 10 FPS bez memory leaks
  5. Obsłuż błędy gracefully (zwróć null jeśli capture się nie uda)

  ## Testy
  Napisz testy w `tests/TzarBot.Tests/Phase1/ScreenCaptureTests.cs`:
  - Test czy CaptureFrame zwraca dane
  - Test czy rozmiar bufora = Width * Height * 4 (BGRA)
  - Test wydajności (10 FPS przez 10 sekund)

  ## Technologie
  - .NET 8
  - SharpDX.DXGI lub Vortice.Windows
  - xUnit dla testów

  Po zakończeniu uruchom: `dotnet test tests/TzarBot.Tests --filter Phase1.ScreenCapture`

validation_steps:
  - "Sprawdź czy pliki zostały utworzone"
  - "Uruchom dotnet build"
  - "Uruchom dotnet test"
  - "Sprawdź output testów"

on_failure: |
  Jeśli testy nie przechodzą:
  1. Sprawdź logi błędów
  2. Upewnij się że SharpDX/Vortice jest zainstalowany
  3. Sprawdź czy masz uprawnienia do DXGI (może wymagać admina)
  4. Jeśli problem z GPU, spróbuj Software Renderer jako fallback
```

---

*Prompt wygenerowany dla projektu Tzar Bot*
*Do użycia z Claude Code*
