---
name: continue-workflow
description: Kontynuuj workflow TzarBot od miejsca zatrzymania na podstawie continue.md
---

# Kontynuacja Workflow TzarBot

Przeczytaj plik `continue.md` w katalogu głównym projektu i kontynuuj workflow od miejsca zatrzymania.

## Instrukcje:

1. **Przeczytaj `continue.md`** - zawiera aktualny status i następne kroki
2. **Sprawdź `workflow_progress.md`** - dla pełnego kontekstu postępu
3. **Zweryfikuj warunki wznowienia** - np. czy Hyper-V jest zainstalowany
4. **Kontynuuj od następnego kroku** - zgodnie z raportem
5. **Aktualizuj `workflow_progress.md`** - oznacz task jako IN_PROGRESS
6. **Po zakończeniu** - zaktualizuj `continue.md` z nowym statusem

## Weryfikacja przed kontynuacją:

```powershell
# Sprawdź czy Hyper-V jest dostępny
Get-VMSwitch
```

Jeśli komenda działa - Hyper-V jest gotowy. Kontynuuj workflow.
Jeśli błąd - poczekaj na restart lub instalację Hyper-V.
