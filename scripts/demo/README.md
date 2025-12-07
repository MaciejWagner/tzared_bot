# TzarBot Demo Scripts

Skrypty do uruchamiania demo i generowania raportów dla poszczególnych faz projektu.

## Wymagania

- Windows 10/11
- PowerShell 5.1+
- .NET SDK 8.0+
- Projekt TzarBot skopiowany na VM

## Szybki start

### Na hoście - skopiuj skrypty i projekt na VM:

```powershell
# Skopiuj skrypty demo
Copy-VMFile -Name "DEV" -SourcePath "C:\Users\maciek\ai_experiments\tzar_bot\scripts\demo" -DestinationPath "C:\TzarBot\scripts\demo" -FileSource Host -CreateFullPath

# Skopiuj caly projekt (jesli jeszcze nie skopiowany)
Copy-VMFile -Name "DEV" -SourcePath "C:\Users\maciek\ai_experiments\tzar_bot" -DestinationPath "C:\TzarBot" -FileSource Host -CreateFullPath
```

### Na VM DEV - uruchom demo:

```powershell
# Przejdz do katalogu
cd C:\TzarBot\scripts\demo

# Uruchom wszystkie demo
.\Run-AllDemos.ps1 -ProjectPath "C:\TzarBot"

# Lub uruchom pojedyncze demo:
.\Run-Phase0Demo.ps1
.\Run-Phase1Demo.ps1 -ProjectPath "C:\TzarBot"
```

## Skrypty

### Run-AllDemos.ps1

Glowny skrypt uruchamiajacy wszystkie demo.

**Parametry:**
- `-OutputDir` - Katalog wyjsciowy (domyslnie: `C:\TzarBot-Demo`)
- `-ProjectPath` - Sciezka do projektu TzarBot (domyslnie: `C:\TzarBot`)
- `-SkipScreenshots` - Pomin robienie screenshotow

**Przyklad:**
```powershell
.\Run-AllDemos.ps1 -ProjectPath "C:\TzarBot" -OutputDir "C:\Demo-Output"
```

### Run-Phase0Demo.ps1

Demo weryfikacji prerekvizytow (Phase 0).

**Testuje:**
- Informacje systemowe (OS, CPU, RAM)
- Konfiguracje sieci (IP, gateway)
- Polaczenie z internetem
- Instalacje .NET SDK
- Instalacje gry Tzar
- Wolne miejsce na dysku

### Run-Phase1Demo.ps1

Demo Game Interface (Phase 1).

**Testuje:**
- Build projektu
- Testy jednostkowe (46 testow)
- Modul Screen Capture
- Modul Input Injection
- Modul IPC Named Pipes
- Modul Window Detection

## Wyniki

Po uruchomieniu demo generowane sa:

```
C:\TzarBot-Demo\
├── Phase0\
│   ├── phase0_report_YYYY-MM-DD_HH-mm-ss.md    # Raport
│   ├── phase0_demo_YYYY-MM-DD_HH-mm-ss.log     # Logi
│   └── *.png                                     # Screenshoty
├── Phase1\
│   ├── phase1_report_YYYY-MM-DD_HH-mm-ss.md    # Raport
│   ├── build_YYYY-MM-DD_HH-mm-ss.log           # Log buildu
│   ├── tests_YYYY-MM-DD_HH-mm-ss.log           # Log testow
│   └── *.png                                     # Screenshoty
└── demo_summary_YYYY-MM-DD_HH-mm-ss.md          # Podsumowanie
```

## Kopiowanie wynikow na hosta

Po zakonczeniu demo, skopiuj wyniki na hosta:

```powershell
# Na hoscie
Copy-VMFile -Name "DEV" -SourcePath "C:\TzarBot-Demo" -DestinationPath "C:\Users\maciek\ai_experiments\tzar_bot\demo_results" -FileSource Guest
```

Lub przez siec:
```powershell
# Na VM - udostepnij folder
New-SmbShare -Name "TzarBotDemo" -Path "C:\TzarBot-Demo" -ReadAccess "Everyone"

# Na hoscie - skopiuj
Copy-Item "\\192.168.100.10\TzarBotDemo\*" -Destination "C:\Users\maciek\ai_experiments\tzar_bot\demo_results" -Recurse
```

## Troubleshooting

### Problem: Skrypt nie moze znalezc projektu
```powershell
# Podaj pelna sciezke
.\Run-Phase1Demo.ps1 -ProjectPath "C:\pelna\sciezka\do\TzarBot"
```

### Problem: Brak uprawnien do screenshotow
```powershell
# Uruchom z flaga skip
.\Run-AllDemos.ps1 -SkipScreenshots
```

### Problem: Build fails
Sprawdz czy .NET SDK jest zainstalowany:
```powershell
dotnet --version
```
