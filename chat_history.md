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
