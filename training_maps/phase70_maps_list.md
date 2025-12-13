# Lista Map Treningowych - Faza 7.0

**Lokalizacja:** `training_maps/`
**Liczba map:** 24 (+ mapa 0 testowa)
**Format:** .tzared (Tzar scenario)

---

## Metryka Sukcesu - GLOBALNA

**Wszystkie mapy używają tej samej metryki sukcesu:**

```
WYGRANA = Pojawienie sie ekranu "YOU ARE VICTORIOUS"
PRZEGRANA = Pojawienie sie ekranu porazki (timeout lub smierc)
```

Detekcja opiera sie na rozpoznaniu obrazu ekranu zwyciestwa/porazki.

### Ekran Zwyciestwa (Victory)
- Referencyjny screenshot: `game_screenshots/won_game.PNG`
- Tekst "YOU ARE VICTORIOUS" (zloty, na dole okna modalnego)
- Okno modalne z ozdobna ramka
- Komunikat "[Player] won!" u gory ekranu
- Przycisk X (czerwony) w prawym gornym rogu okna

### Ekran Porazki (Defeat)
- Referencyjny screenshot: `game_screenshots/defeated_game.PNG`
- Tekst porazki w oknie modalnym
- Pojawia sie po timeout (20 sekund) lub po smierci wszystkich jednostek/budynkow

### Parametry Treningu
| Parametr | Wartosc | Opis |
|----------|---------|------|
| Timeout | 20 sekund | Czas na wykonanie akcji (brak = porazka) |
| Proby | 10 | Liczba prob na bota przed oceną fitness |
| Sukces | Victory Screen | Bot wykonal zadanie |
| Porazka | Defeat Screen | Timeout lub smierc |

---

## Mapa 0: TEST (juz utworzona!)

| Pole | Wartosc |
|------|---------|
| **Nazwa pliku** | `training-0.tzared` |
| **Status** | UTWORZONA |
| **Cel** | Najprostsza mapa - poruszenie wiesniakiem = zwyciestwo |
| **Timeout** | 20 sekund (brak akcji = porazka) |

**Opis:**
Mapa testowa do weryfikacji czy caly pipeline dziala:
- 1 Wiesniaka na mapie
- Poruszenie w JAKIMKOLWIEK kierunku triggeruje zwyciestwo
- Brak ruchu przez 20 sekund = ekran porazki
- Sluzy do testowania detekcji ekranu zwyciestwa/porazki

**Trigger zwyciestwa:**
```
Wiesniaka.Pozycja != Wiesniaka.PozycjaStartowa → VICTORY
Czas >= 20 sekund AND Brak ruchu → DEFEAT
```

---

## Grupa 1: SELEKCJA (3 mapy)

**UWAGA:** Mapy selekcji sa trudne do zaimplementowania z triggerem "ekran zwyciestwa",
poniewaz samo zaznaczenie jednostki nie generuje Victory Screen.

**Opcje implementacji:**
1. Polaczyc selekcje z ruchem (zaznacz + rusz = victory)
2. Uzyc innej metryki (np. czas do pierwszej akcji)
3. Pominac te mapy i zaczac od ruchu (mapa training-0 juz to testuje)

**REKOMENDACJA:** Zaczac od mapy 7.0.4 (ruch), bo training-0 juz testuje podstawowa interakcje.

### Mapa 7.0.1: Selekcja + Ruch (polaczone)
| Pole | Wartosc |
|------|---------|
| **Nazwa pliku** | `701_selekcja_ruch.tzared` |
| **Rozmiar mapy** | Tiny (64x64) |
| **Czas limit** | 30 sekund |

**Ustawienia startowe:**
- 1 Chlop w centrum mapy
- Trigger: Poruszenie chlopa = Victory (jak training-0)

**Trigger Victory:** Chlop zmienil pozycje

---

### Mapa 7.0.2: Selekcja Grupowa + Ruch
| Pole | Wartosc |
|------|---------|
| **Nazwa pliku** | `702_selekcja_grupa_ruch.tzared` |
| **Rozmiar mapy** | Tiny (64x64) |
| **Czas limit** | 45 sekund |

**Ustawienia startowe:**
- 5 Chlopow w grupie (blisko siebie)
- Trigger: Wszystkie 5 jednostek dotarlo do strefy docelowej = Victory

**Trigger Victory:** Wszystkie jednostki w strefie docelowej

---

### Mapa 7.0.3: Selekcja Typu + Ruch
| Pole | Wartosc |
|------|---------|
| **Nazwa pliku** | `703_selekcja_typ_ruch.tzared` |
| **Rozmiar mapy** | Small (96x96) |
| **Czas limit** | 45 sekund |

**Ustawienia startowe:**
- 3 Chlopow + 2 Zolnierzy rozrzuconych
- Trigger: Wszystkie jednostki JEDNEGO typu w strefie = Victory

**Trigger Victory:** (3 Chlopy w strefie) LUB (2 Zolnierzy w strefie)

---

## Grupa 2: RUCH (2 mapy)

### Mapa 7.0.4: Ruch Pojedynczej Jednostki
| Pole | Wartosc |
|------|---------|
| **Nazwa pliku** | `704_ruch_pojedynczy.tzared` |
| **Rozmiar mapy** | Small (96x96) |
| **Czas limit** | 60 sekund |

**Ustawienia startowe:**
- 1 Chlop na pozycji startowej
- Strefa docelowa oznaczona (np. flaga)
- Teren plaski, bez przeszkod

**Trigger Victory:** Chlop w strefie docelowej

---

### Mapa 7.0.5: Ruch Grupy Jednostek
| Pole | Wartosc |
|------|---------|
| **Nazwa pliku** | `705_ruch_grupy.tzared` |
| **Rozmiar mapy** | Small (96x96) |
| **Czas limit** | 60 sekund |

**Ustawienia startowe:**
- 5 Zolnierzy w grupie
- Strefa docelowa oznaczona
- Teren plaski

**Trigger Victory:** Wszystkie 5 jednostek w strefie docelowej

---

## Grupa 3: ZBIERANIE ZASOBOW (4 mapy)

### Mapa 7.0.6: Zbieranie Drewna
| Pole | Wartosc |
|------|---------|
| **Nazwa pliku** | `706_zbieranie_drewno.tzared` |
| **Czas limit** | 120 sekund (2 min) |

**Ustawienia:** 1 Chlop + Zamek + Las blisko, Start: 0 zasobow

**Trigger Victory:** Gracz.Drewno >= 50

---

### Mapa 7.0.7: Zbieranie Zlota
| Pole | Wartosc |
|------|---------|
| **Nazwa pliku** | `707_zbieranie_zloto.tzared` |
| **Czas limit** | 120 sekund (2 min) |

**Ustawienia:** 1 Chlop + Zamek + Kopalnia zlota blisko, Start: 0 zasobow

**Trigger Victory:** Gracz.Zloto >= 50

---

### Mapa 7.0.8: Zbieranie Kamienia
| Pole | Wartosc |
|------|---------|
| **Nazwa pliku** | `708_zbieranie_kamien.tzared` |
| **Czas limit** | 120 sekund (2 min) |

**Ustawienia:** 1 Chlop + Zamek + Kopalnia kamienia blisko, Start: 0 zasobow

**Trigger Victory:** Gracz.Kamien >= 50

---

### Mapa 7.0.9: Zbieranie Jedzenia
| Pole | Wartosc |
|------|---------|
| **Nazwa pliku** | `709_zbieranie_jedzenie.tzared` |
| **Czas limit** | 150 sekund (2.5 min) |

**Ustawienia:** 1 Chlop + Zamek + Farma (pre-built) z krowami, Start: 0 zasobow

**Trigger Victory:** Gracz.Jedzenie >= 50

---

## Grupa 4: BUDOWANIE (4 mapy)

### Mapa 7.0.10: Budowa Tartaku
| Pole | Wartosc |
|------|---------|
| **Nazwa pliku** | `710_budowa_tartak.tzared` |
| **Czas limit** | 180 sekund (3 min) |

**Ustawienia:** 1 Chlop + Zamek + Las, Start: Drewno=200, Zloto=50

**Trigger Victory:** Tartak ukonczony

---

### Mapa 7.0.11: Budowa Domu
| Pole | Wartosc |
|------|---------|
| **Nazwa pliku** | `711_budowa_dom.tzared` |
| **Czas limit** | 150 sekund (2.5 min) |

**Ustawienia:** 1 Chlop + Zamek, Start: Drewno=150

**Trigger Victory:** Dom ukonczony

---

### Mapa 7.0.12: Budowa Koszar
| Pole | Wartosc |
|------|---------|
| **Nazwa pliku** | `712_budowa_koszary.tzared` |
| **Czas limit** | 180 sekund (3 min) |

**Ustawienia:** 1 Chlop + Zamek + Tartak (pre-built), Start: Drewno=300, Zloto=100

**Trigger Victory:** Koszary ukonczone

---

### Mapa 7.0.13: Budowa Farmy
| Pole | Wartosc |
|------|---------|
| **Nazwa pliku** | `713_budowa_farma.tzared` |
| **Czas limit** | 180 sekund (3 min) |

**Ustawienia:** 1 Chlop + Zamek, Start: Drewno=200, Zloto=50

**Trigger Victory:** Farma ukonczona

---

## Grupa 5: PRODUKCJA JEDNOSTEK (2 mapy)

### Mapa 7.0.14: Trenowanie Chlopa
| Pole | Wartosc |
|------|---------|
| **Nazwa pliku** | `714_trening_chlop.tzared` |
| **Czas limit** | 120 sekund (2 min) |

**Ustawienia:** 0 jednostek startowych! + Zamek, Start: Jedzenie=100

**Trigger Victory:** Chlop >= 1

---

### Mapa 7.0.15: Trenowanie Zolnierza
| Pole | Wartosc |
|------|---------|
| **Nazwa pliku** | `715_trening_zolnierz.tzared` |
| **Czas limit** | 150 sekund (2.5 min) |

**Ustawienia:** 0 jednostek + Zamek + Koszary (pre-built), Start: Jedzenie=100, Zloto=50

**Trigger Victory:** Zolnierz >= 1

---

## Grupa 6: WALKA (2 mapy)

### Mapa 7.0.16: Atak na Budynek
| Pole | Wartosc |
|------|---------|
| **Nazwa pliku** | `716_atak_budynek.tzared` |
| **Czas limit** | 180 sekund (3 min) |

**Ustawienia Gracz:** 5 Zolnierzy
**Ustawienia Wrog (Passive):** 1 Dom, 0 jednostek

**Trigger Victory:** Wrog.Budynki == 0 (zniszczone)

---

### Mapa 7.0.17: Atak na Jednostke
| Pole | Wartosc |
|------|---------|
| **Nazwa pliku** | `717_atak_jednostka.tzared` |
| **Czas limit** | 120 sekund (2 min) |

**Ustawienia Gracz:** 3 Zolnierzy
**Ustawienia Wrog (Passive):** 1 Chlop (stoi w miejscu)

**Trigger Victory:** Wrog.Jednostki == 0 (zabite)

---

## Grupa 7: UTILITY (2 mapy)

**UWAGA:** Te mapy sa trudniejsze do zaimplementowania z Victory Screen.
Rozwazyc polaczenie z innymi akcjami lub pominieicie.

### Mapa 7.0.18: Przewijanie Mapy + Ruch
| Pole | Wartosc |
|------|---------|
| **Nazwa pliku** | `718_scroll_ruch.tzared` |
| **Czas limit** | 60 sekund |

**Ustawienia:**
- 1 Chlop na (20,20), kamera startowa na (100,100)
- Strefa docelowa przy chlopie
- Bot musi przewinac mape, znalezc chlopa, i ruszyc go do strefy

**Trigger Victory:** Chlop w strefie docelowej

---

### Mapa 7.0.19: Grupy Hotkey + Atak
| Pole | Wartosc |
|------|---------|
| **Nazwa pliku** | `719_hotkey_atak.tzared` |
| **Czas limit** | 90 sekund |

**Ustawienia:**
- 5 Zolnierzy rozrzuconych + 5 Chlopow rozrzuconych
- 1 budynek wroga (Passive)
- Bot musi zgrupowac zolnierzy i zniszczyc budynek

**Trigger Victory:** Wrog.Budynki == 0

---

## Grupa 8: KOMPOZYCJE (5 map)

### Mapa 7.0.20: Zbieranie + Budowa
| Pole | Wartosc |
|------|---------|
| **Nazwa pliku** | `720_combo_zbierz_buduj.tzared` |
| **Czas limit** | 240 sekund (4 min) |

**Ustawienia:** 2 Chlopow + Zamek + Las, Start: Drewno=50 (za malo na Tartak)

**Trigger Victory:** Tartak ukonczony (wymaga zebrania drewna najpierw)

---

### Mapa 7.0.21: Setup Ekonomii
| Pole | Wartosc |
|------|---------|
| **Nazwa pliku** | `721_combo_ekonomia.tzared` |
| **Czas limit** | 300 sekund (5 min) |

**Ustawienia:** 3 Chlopow + Zamek + Las, Start: Drewno=100, Jedzenie=50

**Trigger Victory:** Domy >= 2 AND Tartak AND Chlopow >= 6

---

### Mapa 7.0.22: Setup Militarny
| Pole | Wartosc |
|------|---------|
| **Nazwa pliku** | `722_combo_militarny.tzared` |
| **Czas limit** | 300 sekund (5 min) |

**Ustawienia:** 2 Chlopow + Zamek + Tartak (pre-built), Start: Drewno=300, Jedzenie=200, Zloto=150

**Trigger Victory:** Koszary ukonczone AND Zolnierzy >= 3

---

### Mapa 7.0.23: Pierwszy Atak
| Pole | Wartosc |
|------|---------|
| **Nazwa pliku** | `723_combo_pierwszy_atak.tzared` |
| **Czas limit** | 420 sekund (7 min) |

**Ustawienia Gracz:** 0 jednostek + Zamek + Koszary (pre-built), Start: Jedzenie=500, Zloto=300
**Ustawienia Wrog (Passive):** 1 Dom

**Trigger Victory:** Wrog.Budynki == 0 (wymaga wytrenowania armii)

---

### Mapa 7.0.24: Pelny Cykl (GRADUATION TEST)
| Pole | Wartosc |
|------|---------|
| **Nazwa pliku** | `724_combo_pelny_cykl.tzared` |
| **Czas limit** | 600 sekund (10 min) |

**Ustawienia Gracz:** 3 Chlopow + Zamek + Las + Kopalnia zlota, Start: Drewno=200, Jedzenie=100, Zloto=100
**Ustawienia Wrog (Passive):** Zamek + 2 Domy + 2 Chlopow (stoja)

**Trigger Victory:** Wrog.Budynki == 0 AND Wrog.Jednostki == 0

**Oczekiwana sekwencja:**
1. Zbieranie drewna
2. Budowa Tartaku
3. Budowa Domow
4. Trening chlopow
5. Zbieranie zlota
6. Budowa Koszar
7. Trening armii (5+ zolnierzy)
8. Atak na baze wroga
9. Zniszczenie wszystkiego

---

## Podsumowanie Map

| Grupa | Ilosc | Pliki | Trudnosc |
|-------|-------|-------|----------|
| **Test (0)** | 1 | training-0 | Bardzo latwe |
| Selekcja+Ruch | 3 | 701-703 | Latwe |
| Ruch | 2 | 704-705 | Latwe |
| Zbieranie | 4 | 706-709 | Latwe |
| Budowanie | 4 | 710-713 | Srednie |
| Produkcja | 2 | 714-715 | Srednie |
| Walka | 2 | 716-717 | Srednie |
| Utility | 2 | 718-719 | Srednie |
| Kompozycje | 5 | 720-724 | Trudne |

**TOTAL: 25 map** (1 testowa + 24 treningowych)

---

## Status Map

| Mapa | Status | Notatki |
|------|--------|---------|
| training-0 | **UTWORZONA** | Podstawowy test ruchu |
| 701-724 | PENDING | Do utworzenia w edytorze |

---

## Uwagi do tworzenia map

1. **Format:** `.tzared` (format map gry Tzar)
2. **Metryka:** Wszystkie mapy uzywaja Victory Screen jako metryki sukcesu
3. **Passive AI:** Wrog na mapach walki = Passive (nie atakuje, nie produkuje)
4. **Mgla wojny:** Wylaczona na wszystkich mapach
5. **Testowanie:** Kazda mapa wymaga manualnego testu przed treningiem
6. **Referencja:** `game_screenshots/won_game.PNG` - przykladowy ekran zwyciestwa

---

## Historia aktualizacji

| Data | Zmiana |
|------|--------|
| 2025-12-12 | Utworzenie dokumentu z lista 24 map |
| 2025-12-13 | Dodano mape 0 (training-0.tzared), zaktualizowano metryki na Victory Screen |
