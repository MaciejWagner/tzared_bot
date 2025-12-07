# Backlog Fazy 0: Prerequisites

**Ostatnia aktualizacja:** 2025-12-07
**Status Fazy:** PENDING
**Priorytet:** MUST (wymagane przed rozpoczeciem Fazy 1)

---

## Podsumowanie

Faza 0 zawiera prerekvizity niezbedne do rozpoczecia prac rozwojowych nad TzarBot. Obejmuje konfiguracje srodowiska host, maszyny wirtualnej deweloperskiej oraz instalacje gry Tzar.

---

## Infrastructure Constraints

| Constraint | Value | Notes |
|------------|-------|-------|
| **Max RAM dla VM** | 10 GB | Suma RAM wszystkich VM (DEV + Workers) nie moze przekroczyc 10GB |
| **VM Development** | DEV | Glowna maszyna deweloperska |

---

## Taski

### F0.T1: Host Machine Setup
| Pole | Wartosc |
|------|---------|
| **ID** | F0.T1 |
| **Tytul** | Konfiguracja maszyny host |
| **Opis** | Instalacja i konfiguracja Hyper-V na maszynie host, utworzenie virtual switch |
| **Priorytet** | MUST |
| **Szacowany naklad** | S (Small) |
| **Status** | PENDING |
| **Agent** | DEVOPS_SENIOR |
| **Zaleznosci** | Brak |

**Kryteria akceptacji:**
- [ ] Hyper-V wlaczony na maszynie host
- [ ] Utworzony Internal Virtual Switch "TzarBotSwitch"
- [ ] NAT skonfigurowany dla dostepu do internetu z VM
- [ ] PowerShell Hyper-V module dostepny
- [ ] **env_settings.md zaktualizowany** (switch name, NAT subnet, gateway IP)

**Powiazane pliki:**
- `plans/phase_0_prerequisites.md`
- `scripts/vm/New-TzarBotSwitch.ps1`
- `env_settings.md`

---

### F0.T2: Development VM Setup
| Pole | Wartosc |
|------|---------|
| **ID** | F0.T2 |
| **Tytul** | Konfiguracja maszyny wirtualnej deweloperskiej "DEV" |
| **Opis** | Utworzenie VM o nazwie "DEV" z Windows do rozwoju i testowania bota |
| **Priorytet** | MUST |
| **Szacowany naklad** | M (Medium) |
| **Status** | PENDING |
| **Agent** | DEVOPS_SENIOR |
| **Zaleznosci** | F0.T1 |

**Kryteria akceptacji:**
- [ ] VM o nazwie "DEV" utworzona z Windows 10/11
- [ ] 4 GB RAM przydzielone (UWAGA: max 10GB dla calej infrastruktury)
- [ ] 2+ CPU cores
- [ ] 60+ GB dysk
- [ ] Enhanced Session Mode wlaczony
- [ ] .NET 10 SDK zainstalowany
- [ ] Visual Studio Code / VS zainstalowane
- [ ] **env_settings.md zaktualizowany** (VM name: DEV, IP, specs, credentials)

**Powiazane pliki:**
- `plans/phase_0_prerequisites.md`
- `scripts/vm/New-DevelopmentVM.ps1`
- `env_settings.md`

---

### F0.T3: Tzar Game Installation
| Pole | Wartosc |
|------|---------|
| **ID** | F0.T3 |
| **Tytul** | Instalacja gry Tzar |
| **Opis** | Instalacja gry Tzar na maszynie deweloperskiej, konfiguracja ustawien |
| **Priorytet** | MUST |
| **Szacowany naklad** | S (Small) |
| **Status** | PENDING |
| **Agent** | DEVOPS_SENIOR |
| **Zaleznosci** | F0.T2 |

**Kryteria akceptacji:**
- [ ] Gra zainstalowana z `files/tzared.windows.zip`
- [ ] Gra uruchamia sie poprawnie
- [ ] Rozdzielczosc ustawiona na 1920x1080 (lub inna docelowa)
- [ ] Gra dziala w trybie okienkowym (windowed mode)
- [ ] Testowa rozgrywka zakonczona bez bledow
- [ ] **env_settings.md zaktualizowany** (game path, resolution, settings)

**Powiazane pliki:**
- `files/tzared.windows.zip`
- `plans/phase_0_prerequisites.md`
- `env_settings.md`

---

### F0.T4: Environment Verification
| Pole | Wartosc |
|------|---------|
| **ID** | F0.T4 |
| **Tytul** | Weryfikacja srodowiska |
| **Opis** | Koncowa weryfikacja ze wszystkie komponenty dzialaja poprawnie |
| **Priorytet** | MUST |
| **Szacowany naklad** | S (Small) |
| **Status** | PENDING |
| **Agent** | DEVOPS_SENIOR |
| **Zaleznosci** | F0.T1, F0.T2, F0.T3 |

**Kryteria akceptacji:**
- [ ] Host komunikuje sie z VM przez siec
- [ ] Gra uruchamia sie i konczy bez bledow
- [ ] dotnet build dziala na VM
- [ ] Wszystkie wymagane narzedzia dostepne
- [ ] Dokumentacja weryfikacyjna przygotowana

**Powiazane pliki:**
- `plans/phase_0_prerequisites.md`
- `scripts/vm/Test-Environment.ps1`

---

### F0.T5: Infrastructure Documentation
| Pole | Wartosc |
|------|---------|
| **ID** | F0.T5 |
| **Tytul** | Dokumentacja infrastruktury |
| **Opis** | Szczegolowa dokumentacja calej infrastruktury projektu (VM, sieci, limitow zasobow) |
| **Priorytet** | SHOULD |
| **Szacowany naklad** | S (Small) |
| **Status** | PENDING |
| **Agent** | DEVOPS_SENIOR |
| **Zaleznosci** | F0.T1, F0.T2, F0.T4 |

**Kryteria akceptacji:**
- [ ] Diagram architektury infrastruktury (Mermaid)
- [ ] Dokumentacja wszystkich VM (nazwy, IP, specyfikacje, przeznaczenie)
- [ ] Dokumentacja sieci (switch, NAT, subnety)
- [ ] Dokumentacja limitow zasobow (max 10GB RAM)
- [ ] Instrukcje odtworzenia srodowiska
- [ ] Zapisane w `docs/infrastructure.md`

**Powiazane pliki:**
- `docs/infrastructure.md`
- `env_settings.md`

---

## Metryki Fazy

| Metryka | Wartosc |
|---------|---------|
| Liczba taskow | 5 |
| Ukonczonych | 0 |
| W trakcie | 0 |
| Zablokowanych | 0 |
| Oczekujacych | 5 |
| Postep | 0% |

---

## Zaleznosci zewnetrzne

1. **Licencja Windows** - wymagana dla VM
2. **Hardware** - min. 16GB RAM na host, 4 cores CPU
3. **Instalator gry** - `files/tzared.windows.zip` (dostepny)

---

## Notatki

- Faza 0 jest prerekvizytowa - musi byc ukonczona przed rozpoczeciem Fazy 1
- Wszystkie taski sa oznaczone jako MUST
- Development VM sluzy jako srodowisko testowe dla Phase 1-6
- Template VM dla treningu (Phase 4) bedzie bazowac na Development VM
