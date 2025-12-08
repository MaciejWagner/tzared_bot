# TzarBot Test Report - 2025-12-08

## Executive Summary

| Metric | Value |
|--------|-------|
| **Data uruchomienia** | 2025-12-08 15:35 |
| **VM** | DEV |
| **Typ testów** | Phase1 (Game Interface) |
| **Wynik** | PARTIAL (z oczekiwanymi ograniczeniami) |

## Test Environment

| Parameter | Value |
|-----------|-------|
| VM Name | DEV |
| .NET Version | 8.0.416 |
| RAM | 4 GB |
| Dysk wolny | ~25 GB |
| Połączenie | PowerShell Direct (Hyper-V) |

## Test Results Summary - Phase1

### InputInjectorTests (14/14 PASS)

| Test | Status | Time |
|------|--------|------|
| TypeKey_DoesNotThrow | ✅ PASS | 64ms |
| LeftClick_DoesNotThrow | ✅ PASS | 15ms |
| MoveMouse_DoesNotThrow | ✅ PASS | <1ms |
| TypeHotkey_DoesNotThrow | ✅ PASS | 45ms |
| DoubleClick_DoesNotThrow | ✅ PASS | 93ms |
| DragStartAndEnd_DoesNotThrow | ✅ PASS | 63ms |
| MultipleConcurrentOperations_AreThreadSafe | ✅ PASS | 9ms |
| MinActionDelay_CanBeChanged | ✅ PASS | 1ms |
| Scroll_DoesNotThrow | ✅ PASS | 64ms |
| ActionCooldown_EnforcesMinDelay | ✅ PASS | 143ms |
| RightClick_DoesNotThrow | ✅ PASS | 11ms |
| PressAndReleaseKey_DoesNotThrow | ✅ PASS | <1ms |
| MoveMouseRelative_DoesNotThrow | ✅ PASS | <1ms |
| DefaultMinActionDelay_Is50ms | ✅ PASS | <1ms |

### WindowDetectorTests (10/12 PASS)

| Test | Status | Notes |
|------|--------|-------|
| EnumerateWindows_WindowsHaveValidHandles | ✅ PASS | |
| SetForeground_WithInvalidHandle_ReturnsFalse | ✅ PASS | |
| FindWindowByProcess_WithNonExistingProcess_ReturnsNull | ✅ PASS | |
| WindowInfo_HasClassName | ✅ PASS | |
| EnumerateWindows_WindowsHaveTitles | ✅ PASS | |
| GetWindowInfo_WithInvalidHandle_ReturnsNull | ✅ PASS | |
| TzarWindow_ConstantsAreDefined | ✅ PASS | |
| FindWindow_WithExistingPattern_ReturnsWindow | ✅ PASS | |
| WindowInfo_HasValidBounds | ✅ PASS | |
| FindWindow_WithNonExistingPattern_ReturnsNull | ✅ PASS | |
| **EnumerateWindows_ReturnsWindows** | ⚠️ FAIL | Brak widocznych okien (VM bez GUI) |
| **GetWindowInfo_WithValidHandle_ReturnsInfo** | ⚠️ FAIL | Brak okien do pobrania info |

### ScreenCaptureTests (0/9 - EXPECTED FAILURES)

| Test | Status | Error |
|------|--------|-------|
| Wszystkie testy | ⚠️ FAIL | `Failed to get output 0` |

**Przyczyna:** VM DEV działa bez aktywnej sesji graficznej DXGI. Enhanced Session Mode nie jest włączony lub brak zalogowanego użytkownika z wyświetlaczem.

**Rozwiązanie:** Testy ScreenCapture wymagają:
1. Enhanced Session Mode w Hyper-V
2. Zalogowanego użytkownika z aktywną sesją pulpitu
3. Sterowników graficznych

## Test Results Summary - Phase2 (NeuralNetwork)

| Status | Notes |
|--------|-------|
| **TIMEOUT** | Testy zawieszają się po ~5 minutach |

**Przyczyna:** Prawdopodobnie problem z:
- ONNX Runtime na VM bez GPU
- Niewystarczająca ilość RAM (4GB) dla ciężkich operacji NN
- Brak odpowiednich bibliotek runtime

**Zalecenia:**
1. Uruchomić testy NeuralNetwork lokalnie na hoście z GPU
2. Zwiększyć RAM VM DEV do 8GB dla testów NN
3. Rozważyć skip testów NN na VM bez GPU

## Known Limitations

### Środowisko VM bez sesji graficznej

| Komponent | Host | VM DEV (headless) | VM DEV (GUI) |
|-----------|------|-------------------|--------------|
| InputInjector | ✅ | ✅ | ✅ |
| WindowDetector | ✅ | ⚠️ Częściowo | ✅ |
| ScreenCapture | ✅ | ❌ | ✅ |
| NeuralNetwork | ✅ | ⏳ Timeout | ? |

### Rekomendacje

1. **Dla testów ScreenCapture:** Zalogować się do VM DEV przez RDP lub uruchomić Enhanced Session
2. **Dla testów NeuralNetwork:** Uruchamiać lokalnie lub na VM z większą ilością RAM
3. **Dla CI/CD:** Oznaczać testy wymagające GUI jako `[Trait("Category", "RequiresDisplay")]`

## Scripts Created

| Script | Purpose |
|--------|---------|
| `scripts/run_tests_on_vm.ps1` | Kompleksowy runner testów na VM (deploy + build + test) |
| `scripts/run_phase1_tests_vm.ps1` | Runner tylko testów Phase1 |
| `scripts/run_phase2_tests_vm.ps1` | Runner testów NeuralNetwork z timeout |
| `scripts/check_vm_env.ps1` | Weryfikacja środowiska VM |
| `scripts/get_test_status.ps1` | Status testów i procesów na VM |
| `scripts/check_dotnet_cpu.ps1` | Monitoring CPU procesów dotnet |
| `scripts/debug_vm_tests.ps1` | Debug i cleanup procesów |

## Conclusion

Infrastruktura testowa na VM DEV działa poprawnie:
- ✅ PowerShell Direct połączenie działa
- ✅ Deploy projektu na VM działa
- ✅ Build na VM działa
- ✅ Uruchamianie testów działa
- ⚠️ Niektóre testy wymagają aktywnej sesji GUI
- ⚠️ Testy NeuralNetwork wymagają więcej zasobów

**Status ogólny:** GOTOWE DO UŻYCIA (z udokumentowanymi ograniczeniami)
