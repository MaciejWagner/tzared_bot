# Prompt do planowania bota do gry Tzar

## Kontekst

Tworzysz szczegółowy plan budowy bota AI do gry strategicznej Tzar (https://tza.red/). Bot ma uczyć się grać poprzez algorytm genetyczny operujący na sieciach neuronowych.

## Zadanie

Stwórz kompleksowy plan projektu w pliku `plans/1general_plan.md` z podziałem na następujące fazy:

### Faza 1: Interfejs bota (Game Interface Layer)
Zaprojektuj prosty program działający obok gry, który:
- Przechwytuje zrzuty ekranu z gry
- Wysyła do gry: ruchy myszką, kliknięcia, wciśnięcia klawiszy
- Działa jako warstwa pośrednia między siecią neuronową a grą

### Faza 2: Architektura sieci neuronowej
Zaproponuj wstępną architekturę sieci:
- **Warstwa wejściowa**: screenshot z gry (określ format, rozdzielczość, preprocessing)
- **Warstwy ukryte**: rozpoznawanie obiektów na ekranie, analiza sytuacji w grze
- **Warstwa wyjściowa**: komendy do interfejsu (ruchy myszy, kliknięcia, skróty klawiszowe)

### Faza 3: Algorytm genetyczny
Zaprojektuj algorytm genetyczny, który:
- Manipuluje architekturą warstw ukrytych
- Dodaje/usuwa warstwy
- Zmienia liczbę neuronów w warstwach
- Określ funkcje mutacji i krzyżowania

### Faza 4: Infrastruktura Hyper-V
Zaplanuj:
- Automatyczne tworzenie maszyn wirtualnych z Windows
- Uruchamianie instancji gry z różnymi sieciami neuronowymi
- Zarządzanie wieloma równoległymi sesjami treningowymi
- Użyj Puppet/Terraform do automatyzacji

### Faza 5: Detekcja wyniku gry
Zaprojektuj funkcję, która:
- Rozpoznaje ekran wygranej/przegranej
- Zwraca wynik do systemu uczenia
- Obsługuje edge cases (crash, timeout, itp.)

### Faza 6: Protokół uczenia (Training Pipeline)
Zaplanuj etapy ewolucji:
1. **Etap podstawowy**: Sieci uczą się manipulować mapą (scrollowanie, zaznaczanie). Kryterium: wykonanie jakiejkolwiek sensownej akcji.
2. **Etap walki z AI**: Sieci walczą z wbudowanym SI na różnych poziomach trudności. Kryterium: min. 10/20 wygranych do awansu.
3. **Etap turniejowy**: Sieci walczą między sobą. Selekcja najlepszych do następnej generacji.

## Wymagania do planu

1. **Dla każdego punktu podaj**:
   - Szczegółowy opis implementacji
   - Sugerowane technologie (preferowane: C#, C, Puppet/Terraform, ale TensorFlow/PyTorch jeśli konieczne)
   - Konkretne komendy Claude Code do użycia w danym punkcie
   - Szacowany nakład pracy (S/M/L/XL)
   - Zależności od innych punktów
   - Potencjalne ryzyka i ich mitygacja

2. **Struktura planu**:
   - Diagram zależności między fazami
   - Kolejność implementacji (co można robić równolegle)
   - MVP dla każdej fazy
   - Metryki sukcesu dla każdej fazy

3. **Aspekty techniczne do rozważenia**:
   - Jak efektywnie przetwarzać screenshoty (rozdzielczość, FPS, format)
   - Jak reprezentować akcje gry (przestrzeń akcji)
   - Jak serializować i deserializować sieci neuronowe
   - Jak komunikować się między procesami (interfejs <-> sieć)
   - Jak skalować trening na wiele maszyn Hyper-V

4. **Komendy Claude Code do uwzględnienia**:
   - `/init` - inicjalizacja projektu
   - Użycie Task tool z subagent_type=Plan dla planowania architektury
   - Użycie Task tool z subagent_type=Explore dla analizy istniejących rozwiązań
   - Bash commands dla setup infrastruktury
   - Edit/Write dla tworzenia kodu

## Format wyjściowy

Zapisz plan do pliku `plan.md` w formacie Markdown z:
- Spisem treści
- Diagramami (ASCII lub Mermaid)
- Tabelami z podsumowaniem technologii
- Checklistami dla każdej fazy
- Sekcją "Następne kroki" na końcu

## Dodatkowe wytyczne

- Bądź pragmatyczny - zaproponuj rozwiązania, które da się zaimplementować
- Uwzględnij, że gra Tza.red to remake gry RTS z 1999 roku
- Rozważ czy lepiej użyć reinforcement learning czy czystego algorytmu genetycznego
- Zaproponuj sposób debugowania i wizualizacji postępów uczenia
- Uwzględnij backup/checkpoint sieci podczas treningu
