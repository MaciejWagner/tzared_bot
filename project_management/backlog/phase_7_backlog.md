# Backlog Fazy 7: Training Execution

**Ostatnia aktualizacja:** 2025-12-12
**Status Fazy:** PENDING (0/12 tasks)
**Priorytet:** MUST (uruchomienie treningu)

---

## Podsumowanie

Faza 7 obejmuje faktyczne uruchomienie treningu botow - od stworzenia map edukacyjnych, przez konfiguracje VM workerow, do przeprowadzenia pelnego cyklu treningowego. Ta faza wykorzystuje caly zaimplementowany pipeline z Faz 1-6.

**Zaleznosci:** Wszystkie poprzednie fazy (0-6) COMPLETED

**Dokumentacja strategii:** `plans/training_strategy.md`

---

## Faza 7.0: Curriculum "Wykonanie Akcji = Sukces"

### Filozofia

Faza 7.0 koncentruje sie na **binarnym sukcesie** - bot albo wykonuje konkretna akcje poprawnie (TAK), albo nie (NIE). Nie ma tutaj skomplikowanych funkcji fitness - liczy sie tylko czy akcja zostala wykonana.

**Kluczowa zasada:** Zacznij od najprostszych, atomowych umiejętności i buduj na nich kompozycje.

---

### Tabela podsumowujaca wszystkie etapy

| ID | Nazwa | Cel | Czas Limit | Zaleznosci | Priorytet |
|----|-------|-----|------------|------------|-----------|
| **7.0.1** | Selection-Single | Zaznacz pojedyncza jednostke | 30s | Brak | P0 |
| **7.0.2** | Selection-Box | Zaznacz grupe jednostek (box select) | 45s | 7.0.1 | P0 |
| **7.0.3** | Selection-DoubleClick | Zaznacz wszystkie jednostki tego samego typu | 30s | 7.0.1 | P1 |
| **7.0.4** | Move-Single | Przesun jednostke do punktu | 60s | 7.0.1 | P0 |
| **7.0.5** | Move-Group | Przesun grupe jednostek | 60s | 7.0.2 | P0 |
| **7.0.6** | Gather-Wood | Zbierz 50 drewna | 120s | 7.0.1, 7.0.4 | P0 |
| **7.0.7** | Gather-Gold | Zbierz 50 zlota | 120s | 7.0.1, 7.0.4 | P0 |
| **7.0.8** | Gather-Stone | Zbierz 50 kamienia | 120s | 7.0.1, 7.0.4 | P1 |
| **7.0.9** | Gather-Food | Zbierz 50 jedzenia (farma) | 150s | 7.0.1, 7.0.4 | P1 |
| **7.0.10** | Build-LumberMill | Zbuduj Tartak | 180s | 7.0.6 | P0 |
| **7.0.11** | Build-House | Zbuduj Dom | 150s | 7.0.6 | P0 |
| **7.0.12** | Build-Barracks | Zbuduj Koszary | 180s | 7.0.10, 7.0.11 | P0 |
| **7.0.13** | Build-Farm | Zbuduj Farme | 180s | 7.0.6 | P1 |
| **7.0.14** | Train-Peasant | Wytrenuj chlopa w Zamku | 120s | 7.0.1 | P0 |
| **7.0.15** | Train-Soldier | Wytrenuj zolnierza w Koszarach | 150s | 7.0.12 | P0 |
| **7.0.16** | Attack-Building | Zniszcz budynek wroga | 180s | 7.0.5 | P0 |
| **7.0.17** | Attack-Unit | Zabij jednostke wroga | 120s | 7.0.4 | P0 |
| **7.0.18** | Scroll-Map | Przewin mape do okreslonego punktu | 45s | Brak | P1 |
| **7.0.19** | Hotkey-Group | Przypisz grupe do hotkey i przywolaj | 60s | 7.0.2 | P1 |
| **7.0.20** | Combo-GatherBuild | Zbierz 100 drewna + zbuduj Tartak | 240s | 7.0.6, 7.0.10 | P0 |
| **7.0.21** | Combo-EconomySetup | Zbuduj 2 domy + 1 tartak + wytrenuj 3 chlopow | 300s | 7.0.10, 7.0.11, 7.0.14 | P0 |
| **7.0.22** | Combo-MilitarySetup | Zbuduj koszary + wytrenuj 3 zolnierzy | 300s | 7.0.12, 7.0.15 | P0 |
| **7.0.23** | Combo-FirstAttack | Wytrenuj 5 zolnierzy + zniszcz 1 budynek wroga | 420s | 7.0.22, 7.0.16 | P0 |
| **7.0.24** | Combo-FullCycle | Ekonomia + Armia + Atak | 600s | 7.0.21, 7.0.23 | P0 |

---

### Diagram zaleznosci

```
                                   7.0.1 Selection-Single
                                          |
               +--------------------------+---------------------------+
               |                          |                           |
               v                          v                           v
      7.0.2 Selection-Box           7.0.4 Move-Single           7.0.14 Train-Peasant
               |                          |                           |
       +-------+-------+          +-------+-------+                   |
       |               |          |               |                   |
       v               v          v               v                   |
 7.0.3 DoubleClick  7.0.5 Move-Group    7.0.6 Gather-Wood             |
       |               |                    |                         |
       |               |          +---------+---------+------+        |
       |               |          |         |         |      |        |
       |               v          v         v         v      v        |
       |         7.0.17 Attack  7.0.7    7.0.8    7.0.9   7.0.10      |
       |          -Unit        Gold     Stone    Food   LumberMill    |
       |               |                                   |          |
       |               |                                   |          |
       |               v                                   v          |
       |         7.0.16 Attack                       7.0.11 House     |
       |          -Building                               |           |
       |               |                                   |          |
       |               |                                   v          |
       |               |                             7.0.12 Barracks  |
       |               |                                   |          |
       |               |                                   v          |
       |               |                             7.0.15 Train     |
       |               |                              -Soldier        |
       |               |                                   |          |
       |               |                                   |          |
       v               v                                   v          v
 7.0.19 Hotkey    +----------------------------------------------------------+
  -Group          |                  KOMPOZYCJE                              |
                  |                                                          |
                  |  7.0.20 Combo-GatherBuild  (7.0.6 + 7.0.10)              |
                  |           |                                              |
                  |           v                                              |
                  |  7.0.21 Combo-EconomySetup (7.0.10+11+14)                |
                  |           |                                              |
                  |           |     7.0.22 Combo-MilitarySetup (7.0.12+15)  |
                  |           |              |                              |
                  |           |              v                              |
                  |           |     7.0.23 Combo-FirstAttack (7.0.22+16)    |
                  |           |              |                              |
                  |           +------+-------+                              |
                  |                  v                                      |
                  |         7.0.24 Combo-FullCycle                          |
                  |         (GRADUATION TEST)                               |
                  +----------------------------------------------------------+
```

---

### Szczegolowe opisy etapow

#### 7.0.1: Selection-Single (Selekcja pojedynczej jednostki)

| Pole | Wartosc |
|------|---------|
| **ID** | 7.0.1 |
| **Nazwa** | Selection-Single |
| **Cel** | Bot musi nauczyc sie podstawowej umiejetnosci - klikniecia na jednostke aby ja zaznaczyc |
| **Mapa startowa** | 1 Chlop w centrum ekranu, brak innych elementow rozpraszajacych |
| **Sekwencja akcji** | 1. Znajdz jednostke (chlopa) na ekranie<br>2. Najedz kursorem na jednostke<br>3. Kliknij lewym przyciskiem myszy |
| **Metryka sukcesu** | BINARNA: Jednostka jest zaznaczona (widoczny panel jednostki na dole ekranu) |
| **Czas limit** | 30 sekund |
| **Zaleznosci** | Brak - to fundamentalna umiejetnosc |

---

#### 7.0.2: Selection-Box (Selekcja grupowa)

| Pole | Wartosc |
|------|---------|
| **ID** | 7.0.2 |
| **Nazwa** | Selection-Box |
| **Cel** | Nauka zaznaczania wielu jednostek jednoczesnie (drag select) |
| **Mapa startowa** | 5 Chlopow blisko siebie (w kwadracie 3x3 tiles) |
| **Sekwencja akcji** | 1. Nacisnij lewy przycisk myszy w rogu grupy<br>2. Przeciagnij tworzac prostokat<br>3. Pusc przycisk |
| **Metryka sukcesu** | BINARNA: Wszystkie 5 jednostek zaznaczonych |
| **Czas limit** | 45 sekund |
| **Zaleznosci** | 7.0.1 |

---

#### 7.0.3: Selection-DoubleClick (Selekcja typu)

| Pole | Wartosc |
|------|---------|
| **ID** | 7.0.3 |
| **Nazwa** | Selection-DoubleClick |
| **Cel** | Nauka zaznaczania wszystkich jednostek danego typu przez podwojne klikniecie |
| **Mapa startowa** | 3 Chlopow + 2 Zolnierzy rozrzuconych na mapie |
| **Metryka sukcesu** | BINARNA: Wszystkie 3 Chlopy LUB wszyscy 2 Zolnierze zaznaczeni |
| **Czas limit** | 30 sekund |
| **Zaleznosci** | 7.0.1 |

---

#### 7.0.4: Move-Single (Ruch pojedynczej jednostki)

| Pole | Wartosc |
|------|---------|
| **ID** | 7.0.4 |
| **Nazwa** | Move-Single |
| **Cel** | Nauka wydawania rozkazu ruchu jednostce |
| **Mapa startowa** | 1 Chlop na (10,10), znacznik/flaga na (50,50) |
| **Sekwencja akcji** | 1. Zaznacz Chlopa (lewy klik)<br>2. Kliknij prawym przyciskiem na cel |
| **Metryka sukcesu** | BINARNA: Chlop dotarl do obszaru 5 tiles od celu |
| **Czas limit** | 60 sekund |
| **Zaleznosci** | 7.0.1 |

---

#### 7.0.5: Move-Group (Ruch grupy)

| Pole | Wartosc |
|------|---------|
| **ID** | 7.0.5 |
| **Nazwa** | Move-Group |
| **Cel** | Nauka przemieszczania grupy jednostek |
| **Mapa startowa** | 5 Zolnierzy na (10,10), znacznik na (50,50) |
| **Metryka sukcesu** | BINARNA: Wszystkie 5 jednostek w promieniu 10 tiles od celu |
| **Czas limit** | 60 sekund |
| **Zaleznosci** | 7.0.2, 7.0.4 |

---

#### 7.0.6: Gather-Wood (Zbieranie drewna)

| Pole | Wartosc |
|------|---------|
| **ID** | 7.0.6 |
| **Nazwa** | Gather-Wood |
| **Cel** | Nauka zbierania podstawowego zasobu - drewna |
| **Mapa startowa** | 1 Chlop + Zamek + Las blisko (5 tiles) |
| **Sekwencja akcji** | 1. Zaznacz Chlopa<br>2. Prawy klik na drzewo<br>3. Chlop automatycznie zbiera i nosi do Zamku |
| **Metryka sukcesu** | BINARNA: Player.Wood >= 50 |
| **Czas limit** | 120 sekund |
| **Zaleznosci** | 7.0.1, 7.0.4 |

---

#### 7.0.7: Gather-Gold (Zbieranie zlota)

| Pole | Wartosc |
|------|---------|
| **ID** | 7.0.7 |
| **Nazwa** | Gather-Gold |
| **Cel** | Nauka zbierania zlota z kopalni |
| **Mapa startowa** | 1 Chlop + Zamek + Kopalnia zlota blisko |
| **Metryka sukcesu** | BINARNA: Player.Gold >= 50 |
| **Czas limit** | 120 sekund |
| **Zaleznosci** | 7.0.1, 7.0.4 |

---

#### 7.0.8: Gather-Stone (Zbieranie kamienia)

| Pole | Wartosc |
|------|---------|
| **ID** | 7.0.8 |
| **Nazwa** | Gather-Stone |
| **Cel** | Nauka zbierania kamienia |
| **Mapa startowa** | 1 Chlop + Zamek + Kopalnia kamienia blisko |
| **Metryka sukcesu** | BINARNA: Player.Stone >= 50 |
| **Czas limit** | 120 sekund |
| **Zaleznosci** | 7.0.1, 7.0.4 |

---

#### 7.0.9: Gather-Food (Zbieranie jedzenia)

| Pole | Wartosc |
|------|---------|
| **ID** | 7.0.9 |
| **Nazwa** | Gather-Food |
| **Cel** | Nauka zbierania jedzenia z farmy |
| **Mapa startowa** | 1 Chlop + Zamek + Farma (pre-built) z krowami |
| **Metryka sukcesu** | BINARNA: Player.Food >= 50 |
| **Czas limit** | 150 sekund |
| **Zaleznosci** | 7.0.1, 7.0.4 |

---

#### 7.0.10: Build-LumberMill (Budowa tartaku)

| Pole | Wartosc |
|------|---------|
| **ID** | 7.0.10 |
| **Nazwa** | Build-LumberMill |
| **Cel** | Nauka budowania budynku - Tartak |
| **Mapa startowa** | 1 Chlop + Zamek + Start: 200 Drewna, 50 Zlota |
| **Sekwencja akcji** | 1. Zaznacz Chlopa<br>2. Nacisnij B (Build menu)<br>3. Wybierz Tartak<br>4. Kliknij na miejsce budowy<br>5. Poczekaj na ukonczenie |
| **Metryka sukcesu** | BINARNA: Tartak zbudowany i ukonczony |
| **Czas limit** | 180 sekund |
| **Zaleznosci** | 7.0.6 |

---

#### 7.0.11: Build-House (Budowa domu)

| Pole | Wartosc |
|------|---------|
| **ID** | 7.0.11 |
| **Nazwa** | Build-House |
| **Cel** | Nauka budowania Domu (zwieksza population cap) |
| **Mapa startowa** | 1 Chlop + Zamek + Start: 150 Drewna |
| **Metryka sukcesu** | BINARNA: Dom zbudowany |
| **Czas limit** | 150 sekund |
| **Zaleznosci** | 7.0.6 |

---

#### 7.0.12: Build-Barracks (Budowa koszar)

| Pole | Wartosc |
|------|---------|
| **ID** | 7.0.12 |
| **Nazwa** | Build-Barracks |
| **Cel** | Nauka budowania Koszar (produkcja piechoty) |
| **Mapa startowa** | 1 Chlop + Zamek + Tartak (pre-built) + Start: 300 Drewna, 100 Zlota |
| **Metryka sukcesu** | BINARNA: Koszary zbudowane |
| **Czas limit** | 180 sekund |
| **Zaleznosci** | 7.0.10, 7.0.11 |

---

#### 7.0.13: Build-Farm (Budowa farmy)

| Pole | Wartosc |
|------|---------|
| **ID** | 7.0.13 |
| **Nazwa** | Build-Farm |
| **Cel** | Nauka budowania Farmy |
| **Mapa startowa** | 1 Chlop + Zamek + Start: 200 Drewna, 50 Zlota |
| **Metryka sukcesu** | BINARNA: Farma zbudowana |
| **Czas limit** | 180 sekund |
| **Zaleznosci** | 7.0.6 |

---

#### 7.0.14: Train-Peasant (Trenowanie chlopa)

| Pole | Wartosc |
|------|---------|
| **ID** | 7.0.14 |
| **Nazwa** | Train-Peasant |
| **Cel** | Nauka produkcji jednostek w Zamku |
| **Mapa startowa** | Zamek + Start: 100 Jedzenia, 0 Chlopow |
| **Sekwencja akcji** | 1. Zaznacz Zamek (klik)<br>2. Kliknij ikone Chlopa w panelu produkcji |
| **Metryka sukcesu** | BINARNA: Liczba Chlopow > 0 |
| **Czas limit** | 120 sekund |
| **Zaleznosci** | 7.0.1 |

---

#### 7.0.15: Train-Soldier (Trenowanie zolnierza)

| Pole | Wartosc |
|------|---------|
| **ID** | 7.0.15 |
| **Nazwa** | Train-Soldier |
| **Cel** | Nauka produkcji jednostek wojskowych |
| **Mapa startowa** | Zamek + Koszary (pre-built) + Start: 100 Jedzenia, 50 Zlota |
| **Metryka sukcesu** | BINARNA: Liczba Zolnierzy >= 1 |
| **Czas limit** | 150 sekund |
| **Zaleznosci** | 7.0.12 |

---

#### 7.0.16: Attack-Building (Atak na budynek)

| Pole | Wartosc |
|------|---------|
| **ID** | 7.0.16 |
| **Nazwa** | Attack-Building |
| **Cel** | Nauka niszczenia budynkow wroga |
| **Mapa startowa** | 5 Zolnierzy + 1 budynek wroga (Dom) blisko, bez obrony |
| **Sekwencja akcji** | 1. Zaznacz Zolnierzy (box select)<br>2. Prawy klik na budynek wroga<br>3. Poczekaj az budynek zostanie zniszczony |
| **Metryka sukcesu** | BINARNA: Budynkow wroga == 0 |
| **Czas limit** | 180 sekund |
| **Zaleznosci** | 7.0.5 |

---

#### 7.0.17: Attack-Unit (Atak na jednostke)

| Pole | Wartosc |
|------|---------|
| **ID** | 7.0.17 |
| **Nazwa** | Attack-Unit |
| **Cel** | Nauka walki z jednostkami wroga |
| **Mapa startowa** | 3 Zolnierzy + 1 Chlop wroga (latwy cel) |
| **Metryka sukcesu** | BINARNA: Jednostek wroga == 0 |
| **Czas limit** | 120 sekund |
| **Zaleznosci** | 7.0.4 |

---

#### 7.0.18: Scroll-Map (Przewijanie mapy)

| Pole | Wartosc |
|------|---------|
| **ID** | 7.0.18 |
| **Nazwa** | Scroll-Map |
| **Cel** | Nauka nawigacji po mapie |
| **Mapa startowa** | Jednostka gracza daleko od centrum widoku, marker w innym miejscu |
| **Metryka sukcesu** | BINARNA: Marker widoczny na ekranie glownym |
| **Czas limit** | 45 sekund |
| **Zaleznosci** | Brak |

---

#### 7.0.19: Hotkey-Group (Grupy skrotow)

| Pole | Wartosc |
|------|---------|
| **ID** | 7.0.19 |
| **Nazwa** | Hotkey-Group |
| **Cel** | Nauka uzywania grup hotkey (Ctrl+1, potem 1) |
| **Mapa startowa** | 5 Zolnierzy rozrzuconych + 5 Chlopow |
| **Sekwencja akcji** | 1. Zaznacz Zolnierzy<br>2. Nacisnij Ctrl+1<br>3. Zaznacz cos innego<br>4. Nacisnij 1 |
| **Metryka sukcesu** | BINARNA: Po nacisnieciu 1, wszystkich 5 Zolnierzy zaznaczonych |
| **Czas limit** | 60 sekund |
| **Zaleznosci** | 7.0.2 |

---

#### 7.0.20: Combo-GatherBuild (Zbieranie + Budowa)

| Pole | Wartosc |
|------|---------|
| **ID** | 7.0.20 |
| **Nazwa** | Combo-GatherBuild |
| **Cel** | Kompozycja: zbierz zasoby i zbuduj budynek |
| **Mapa startowa** | 2 Chlopow + Zamek + Las + Start: 50 Drewna |
| **Sekwencja akcji** | 1. Wyslij Chlopow do lasu<br>2. Zbierz 100 drewna<br>3. Zbuduj Tartak |
| **Metryka sukcesu** | BINARNA: Tartak ukonczony |
| **Czas limit** | 240 sekund |
| **Zaleznosci** | 7.0.6, 7.0.10 |

---

#### 7.0.21: Combo-EconomySetup (Setup ekonomii)

| Pole | Wartosc |
|------|---------|
| **ID** | 7.0.21 |
| **Nazwa** | Combo-EconomySetup |
| **Cel** | Podstawowy setup ekonomiczny: domy + tartak + wiecej chlopow |
| **Mapa startowa** | 3 Chlopow + Zamek + Las + Start: 100 Drewna, 50 Jedzenia |
| **Sekwencja akcji** | 1. Zbieraj drewno<br>2. Zbuduj 2 Domy<br>3. Zbuduj Tartak<br>4. Wytrenuj 3 dodatkowych Chlopow |
| **Metryka sukcesu** | BINARNA: Domy >= 2 AND Tartak AND Chlopow >= 6 |
| **Czas limit** | 300 sekund (5 min) |
| **Zaleznosci** | 7.0.10, 7.0.11, 7.0.14 |

---

#### 7.0.22: Combo-MilitarySetup (Setup militarny)

| Pole | Wartosc |
|------|---------|
| **ID** | 7.0.22 |
| **Nazwa** | Combo-MilitarySetup |
| **Cel** | Zbuduj infrastrukture wojskowa i wytrenuj armie |
| **Mapa startowa** | 2 Chlopow + Zamek + Tartak (pre-built) + Start: 300 Drewna, 200 Jedzenia, 150 Zlota |
| **Sekwencja akcji** | 1. Zbuduj Koszary<br>2. Wytrenuj 3 Zolnierzy |
| **Metryka sukcesu** | BINARNA: Koszary ukonczone AND Zolnierzy >= 3 |
| **Czas limit** | 300 sekund |
| **Zaleznosci** | 7.0.12, 7.0.15 |

---

#### 7.0.23: Combo-FirstAttack (Pierwszy atak)

| Pole | Wartosc |
|------|---------|
| **ID** | 7.0.23 |
| **Nazwa** | Combo-FirstAttack |
| **Cel** | Zbuduj armie i zniszcz budynek wroga |
| **Mapa startowa** | Zamek + Koszary (pre-built) + Start: 500 Jedzenia, 300 Zlota + 1 Dom wroga w odleglosci ~30 tiles |
| **Sekwencja akcji** | 1. Wytrenuj 5 Zolnierzy<br>2. Zgrupuj ich<br>3. Wyslij do ataku<br>4. Zniszcz budynek |
| **Metryka sukcesu** | BINARNA: Budynkow wroga == 0 |
| **Czas limit** | 420 sekund (7 min) |
| **Zaleznosci** | 7.0.22, 7.0.16 |

---

#### 7.0.24: Combo-FullCycle (Pelny cykl) - GRADUATION TEST

| Pole | Wartosc |
|------|---------|
| **ID** | 7.0.24 |
| **Nazwa** | Combo-FullCycle |
| **Cel** | KULMINACJA FAZY 7.0: Pelny cykl od startu do zwyciestwa |
| **Mapa startowa** | Zamek + 3 Chlopow + Start: 200 Drewna, 100 Jedzenia + Wrog: Zamek + 2 Domy + 2 Chlopow (Passive AI) |
| **Sekwencja akcji** | 1. Rozbuduj ekonomie (zbieraj, buduj domy)<br>2. Zbuduj Koszary<br>3. Wytrenuj armie 5+ jednostek<br>4. Zniszcz baze wroga |
| **Metryka sukcesu** | BINARNA: Wszystkie budynki wroga zniszczone |
| **Czas limit** | 600 sekund (10 min) |
| **Zaleznosci** | 7.0.21, 7.0.23 |

---

### Parametry treningu dla Fazy 7.0

| Parametr | Wartosc | Uzasadnienie |
|----------|---------|--------------|
| Populacja | 100 | Standardowa wielkosc |
| Generacje/etap | 30-50 | Wystarczajace do nauki prostych zadan |
| Gry/genom | 3 | Srednia dla binarnej metryki |
| Kryterium awansu | >80% populacji | Etap opanowany gdy 80 genomow przechodzi |
| Elite count | 10% | Zachowaj najlepsze genomy |
| Mutation rate | 5% | Standardowo |
| Crossover rate | 70% | Standardowo |

---

### Fitness Function dla Fazy 7.0

```csharp
public float CalculateBinaryFitness(ScenarioResult result)
{
    float fitness = 0;

    // Glowna metryka: sukces/porazka
    if (result.ObjectiveCompleted)
    {
        fitness = 1000;

        // Bonus za szybkosc (im szybciej, tym lepiej)
        float timeRatio = 1 - (result.TimeElapsed / result.TimeLimit);
        fitness += timeRatio * 500;
    }
    else
    {
        // Partial credit za progress (jesli mierzalny)
        fitness = result.ProgressPercent * 200;
    }

    // Kara za bezczynnosc
    if (result.ActionsPerformed < 5)
    {
        fitness -= 100;
    }

    return Math.Max(0, fitness);
}
```

---

### Szacowany czas treningu Fazy 7.0

| Grupa etapow | Etapy | Avg czas/gra | Gry/gen | Gen | Total czas |
|--------------|-------|--------------|---------|-----|------------|
| Selekcja | 7.0.1-3 | 30s | 300 | 50 | ~4h |
| Ruch | 7.0.4-5 | 60s | 300 | 50 | ~8h |
| Zbieranie | 7.0.6-9 | 120s | 300 | 50 | ~17h |
| Budowa | 7.0.10-13 | 180s | 300 | 50 | ~25h |
| Produkcja | 7.0.14-15 | 135s | 300 | 50 | ~19h |
| Walka | 7.0.16-17 | 150s | 300 | 50 | ~21h |
| Utility | 7.0.18-19 | 50s | 300 | 30 | ~4h |
| Kompozycje | 7.0.20-24 | 360s | 300 | 100 | ~100h |

**Total szacowany czas Fazy 7.0: ~200h** (przy 6 VM pracujacych rownolegle)

---

## Etapy Fazy 7 (po 7.0)

| Etap | Nazwa | Opis | Taski |
|------|-------|------|-------|
| 7.A | Przygotowanie | Mapy, VM, konfiguracja | F7.T1-T4 |
| 7.B | Podstawy | Trening na mapach edukacyjnych | F7.T5-T6 |
| 7.C | AI Combat | Trening przeciwko wbudowanemu AI | F7.T7-T9 |
| 7.D | Zaawansowane | Test dojrzalosci + self-play | F7.T10-T12 |

---

## Taski

### F7.T1: Educational Maps Creation
| Pole | Wartosc |
|------|---------|
| **ID** | F7.T1 |
| **Tytul** | Tworzenie map edukacyjnych |
| **Opis** | Stworzenie zestawu map w edytorze Tzar z triggerami realizujacymi konkretne scenariusze treningowe |
| **Priorytet** | MUST |
| **Szacowany naklad** | L (Large) |
| **Status** | PENDING |
| **Agent** | MANUAL (edytor map Tzar) |
| **Zaleznosci** | Brak |

**Kryteria akceptacji:**
- [ ] Wszystkie 24 mapy z Fazy 7.0 utworzone
- [ ] Wszystkie mapy maja triggery zwyciestwa/porazki
- [ ] Mapy przetestowane manualnie

**Lokalizacja map:**
- `training_maps/phase70/` (24 mapy .scn/.scx)

---

### F7.T2: Worker VM Setup
| Pole | Wartosc |
|------|---------|
| **ID** | F7.T2 |
| **Tytul** | Konfiguracja VM Workerow |
| **Opis** | Stworzenie i konfiguracja 6 VM workerow do rownoleglego treningu |
| **Priorytet** | MUST |
| **Szacowany naklad** | M (Medium) |
| **Status** | PENDING |
| **Agent** | tzarbot-agent-hyperv-admin |
| **Zaleznosci** | F4 (Hyper-V Infrastructure) |

**Kryteria akceptacji:**
- [ ] 6 VM workerow utworzonych (Worker1-Worker6)
- [ ] Kazdy VM: 1.5 GB RAM, 2 vCPU
- [ ] Tzar zainstalowany na kazdym VM
- [ ] TzarBot.GameInterface zainstalowany
- [ ] Siec skonfigurowana (komunikacja z hostem)
- [ ] Auto-start VM po reboocie hosta
- [ ] PowerShell remoting dziala
- [ ] **env_settings.md zaktualizowany** (VM names, IPs, credentials)

**Konfiguracja VM:**

| VM | RAM | CPU | IP | Rola |
|----|-----|-----|-----|------|
| Worker1 | 1.5 GB | 2 | 192.168.100.21 | Training |
| Worker2 | 1.5 GB | 2 | 192.168.100.22 | Training |
| Worker3 | 1.5 GB | 2 | 192.168.100.23 | Training |
| Worker4 | 1.5 GB | 2 | 192.168.100.24 | Training |
| Worker5 | 1.5 GB | 2 | 192.168.100.25 | Training |
| Worker6 | 1.5 GB | 2 | 192.168.100.26 | Training |

---

### F7.T3: Training Orchestrator Configuration
| Pole | Wartosc |
|------|---------|
| **ID** | F7.T3 |
| **Tytul** | Konfiguracja Orchestratora |
| **Opis** | Konfiguracja Training Orchestrator do pracy z 6 VM workerami |
| **Priorytet** | MUST |
| **Szacowany naklad** | S (Small) |
| **Status** | PENDING |
| **Agent** | tzarbot-agent-dotnet-senior |
| **Zaleznosci** | F6.T1, F7.T2 |

**Kryteria akceptacji:**
- [ ] TrainingConfiguration.json z lista workerow
- [ ] Timeouty dostosowane do czasow gier
- [ ] Checkpoint co 5 generacji
- [ ] Logging do pliku i dashboardu
- [ ] Early termination rules skonfigurowane

---

### F7.T4: Map Loading Integration
| Pole | Wartosc |
|------|---------|
| **ID** | F7.T4 |
| **Tytul** | Integracja ladowania map |
| **Opis** | Implementacja automatycznego ladowania map treningowych przez TzarBot |
| **Priorytet** | MUST |
| **Szacowany naklad** | M (Medium) |
| **Status** | PENDING |
| **Agent** | tzarbot-agent-dotnet-senior |
| **Zaleznosci** | F7.T1, F1 (Game Interface) |

**Kryteria akceptacji:**
- [ ] MapLoader laduje mapy z katalogu
- [ ] Automatyczne uruchomienie mapy na VM
- [ ] Detekcja zakonczenia mapy (victory/defeat trigger)
- [ ] Timeout handling
- [ ] Restart gry po zakonczeniu
- [ ] Testy jednostkowe

---

### F7.T5: Phase 7.0 Training Run
| Pole | Wartosc |
|------|---------|
| **ID** | F7.T5 |
| **Tytul** | Uruchomienie treningu Fazy 7.0 |
| **Opis** | Przeprowadzenie treningu na 24 mapach edukacyjnych (curriculum binarny) |
| **Priorytet** | MUST |
| **Szacowany naklad** | XL (Extra Large) |
| **Status** | PENDING |
| **Agent** | MANUAL + tzarbot-agent-ai-senior |
| **Zaleznosci** | F7.T1, F7.T2, F7.T3, F7.T4 |

**Kryteria akceptacji:**
- [ ] Populacja 100 genomow zainicjalizowana
- [ ] Trening na wszystkich 24 mapach Fazy 7.0
- [ ] >80% populacji przechodzi kazdy etap
- [ ] Checkpoint zapisany
- [ ] Metryki zapisane do analizy

**Kryterium przejscia do F7.T6:**
- >80% populacji przechodzi 7.0.24 (Combo-FullCycle)

---

### F7.T6: Phase 7.0 Analysis
| Pole | Wartosc |
|------|---------|
| **ID** | F7.T6 |
| **Tytul** | Analiza treningu Fazy 7.0 |
| **Opis** | Analiza wynikow treningu podstaw, identyfikacja problemow, tuning |
| **Priorytet** | MUST |
| **Szacowany naklad** | M (Medium) |
| **Status** | PENDING |
| **Agent** | tzarbot-agent-ai-senior |
| **Zaleznosci** | F7.T5 |

**Kryteria akceptacji:**
- [ ] Raport z analizy fitness over generations
- [ ] Identyfikacja problematycznych scenariuszy
- [ ] Analiza rozkladu akcji
- [ ] Rekomendacje tuningu
- [ ] Decyzja: kontynuowac do Easy AI lub powrot do 7.0

---

### F7.T7: Easy AI Training Run
| Pole | Wartosc |
|------|---------|
| **ID** | F7.T7 |
| **Tytul** | Trening przeciwko Easy AI |
| **Opis** | Przeprowadzenie treningu przeciwko Easiest i Easy AI |
| **Priorytet** | MUST |
| **Szacowany naklad** | XL (Extra Large) |
| **Status** | PENDING |
| **Agent** | MANUAL + tzarbot-agent-ai-senior |
| **Zaleznosci** | F7.T6 (przejscie kryterium) |

**Kryteria akceptacji:**
- [ ] Trening przeciwko Easiest AI - win rate >50%
- [ ] Trening przeciwko Easy AI - win rate >40%
- [ ] Min. 100 generacji ukonczone
- [ ] Checkpointy zapisane
- [ ] Best genome eksportowany

---

### F7.T8: Normal AI Training Run
| Pole | Wartosc |
|------|---------|
| **ID** | F7.T8 |
| **Tytul** | Trening przeciwko Normal AI |
| **Opis** | Przeprowadzenie treningu przeciwko Normal AI na roznych mapach |
| **Priorytet** | MUST |
| **Szacowany naklad** | XXL (Extra Extra Large) |
| **Status** | PENDING |
| **Agent** | MANUAL + tzarbot-agent-ai-senior |
| **Zaleznosci** | F7.T7 (win rate >40% vs Easy AI) |

**Kryteria akceptacji:**
- [ ] Small map - win rate >50%
- [ ] Medium map - win rate >40%
- [ ] Large map - win rate >30%
- [ ] Min. 200 generacji ukonczone
- [ ] Checkpointy zapisane

---

### F7.T9: AI Combat Analysis
| Pole | Wartosc |
|------|---------|
| **ID** | F7.T9 |
| **Tytul** | Analiza treningu AI Combat |
| **Opis** | Analiza wynikow walki z AI, identyfikacja strategii, bottleneckow |
| **Priorytet** | SHOULD |
| **Szacowany naklad** | M (Medium) |
| **Status** | PENDING |
| **Agent** | tzarbot-agent-ai-senior |
| **Zaleznosci** | F7.T8 |

---

### F7.T10: Maturity Test
| Pole | Wartosc |
|------|---------|
| **ID** | F7.T10 |
| **Tytul** | Test dojrzalosci |
| **Opis** | Walidacja czy najlepsze genomy potrafia pokonac Hard AI |
| **Priorytet** | MUST |
| **Szacowany naklad** | L (Large) |
| **Status** | PENDING |
| **Agent** | MANUAL + tzarbot-agent-ai-senior |
| **Zaleznosci** | F7.T8 |

**Kryterium sukcesu:**
- PASS: Min. 1 genom wygrywa 3/5 vs Hard AI → przejscie do F7.T11
- FAIL: Zaden genom nie wygrywa 3/5 → powrot do F7.T8 z tuningiem

---

### F7.T11: Self-Play Tournament Setup
| Pole | Wartosc |
|------|---------|
| **ID** | F7.T11 |
| **Tytul** | Konfiguracja turnieju self-play |
| **Opis** | Przygotowanie systemu turniejowego do self-play |
| **Priorytet** | SHOULD |
| **Szacowany naklad** | M (Medium) |
| **Status** | PENDING |
| **Agent** | tzarbot-agent-ai-senior |
| **Zaleznosci** | F7.T10 (PASS), F6.T4 (Tournament System) |

---

### F7.T12: Self-Play Continuous Training
| Pole | Wartosc |
|------|---------|
| **ID** | F7.T12 |
| **Tytul** | Ciagly trening self-play |
| **Opis** | Uruchomienie ciaglego treningu self-play do dalszej optymalizacji |
| **Priorytet** | COULD |
| **Szacowany naklad** | XXL (Extra Extra Large) |
| **Status** | PENDING |
| **Agent** | MANUAL |
| **Zaleznosci** | F7.T11 |

---

## Metryki Fazy

| Metryka | Wartosc |
|---------|---------|
| Liczba taskow | 12 |
| Ukonczonych | 0 |
| W trakcie | 0 |
| Oczekujacych | 12 |
| Postep | 0% |

---

## Historia aktualizacji

| Data | Zmiana |
|------|--------|
| 2025-12-09 | Utworzenie dokumentu |
| 2025-12-12 | Dodano szczegolowe curriculum Fazy 7.0 (24 etapy) |
