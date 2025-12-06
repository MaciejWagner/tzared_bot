Zadaniem jest audyt utworzonych tasków pod kątem możliwości uruchomienia projektu.

## Zakres audytu:
1. **Środowisko testowe** - czy opis uruchomienia środowiska jest kompletny i wykonalny?
2. **Kolejność tasków** - czy zależności między taskami są poprawne? Czy można wykonać task N bez ukończenia taska N-1?
3. **Luki w workflow** - jakie kroki mogą uniemożliwić płynne uruchomienie projektu?
4. **Brakujące prerekvizity** - czy wszystkie wymagane narzędzia, biblioteki, konfiguracje są opisane?
5. **Krytyczne ścieżki** - które taski są blokerami dla innych?

## Pliki do przeanalizowania:
- `plans/1general_plan.md` - główny plan projektu
- `plans/2_implementation_workflow.md` - workflow implementacyjny
- `plans/phase_*_detailed.md` - szczegółowe plany faz
- `prompts/phase_*/` - prompty dla poszczególnych tasków
- `scripts/` - skrypty walidacyjne

## Dodatkowe instrukcje:
$ARGUMENTS

## Oczekiwany wynik:
Raport zawierający:
1. **Znalezione luki** - lista problemów blokujących uruchomienie
2. **Brakujące kroki** - co należy dodać do tasków
3. **Propozycje poprawek** - konkretne zmiany w plikach
4. **Ocena gotowości** - czy workflow można uruchomić w obecnym stanie (TAK/NIE + uzasadnienie)
