$results = Get-ChildItem "training/generation_6/results/*_trial*.json" | ForEach-Object {
    $j = Get-Content $_.FullName | ConvertFrom-Json
    [PSCustomObject]@{
        Name = $_.BaseName
        Outcome = $j.Outcome
        Duration = [math]::Round($j.ActualDurationSeconds, 1)
        Actions = $j.TotalActions
    }
}

$results | Format-Table -AutoSize

# Summary
$victories = ($results | Where-Object { $_.Outcome -eq "VICTORY" }).Count
$defeats = ($results | Where-Object { $_.Outcome -eq "DEFEAT" }).Count
$total = $results.Count

Write-Host ""
Write-Host "=== SUMMARY ===" -ForegroundColor Cyan
Write-Host "VICTORY: $victories / $total ($([math]::Round($victories * 100 / $total, 1))%)" -ForegroundColor Green
Write-Host "DEFEAT:  $defeats / $total ($([math]::Round($defeats * 100 / $total, 1))%)" -ForegroundColor Red
