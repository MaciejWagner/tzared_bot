@echo off
echo === TzarBot Training Test ===
echo.

cd /d C:\TzarBot\TrainingRunner

echo Starting 35 second training test with network_00...
echo.

TrainingRunner.exe C:\TzarBot\Models\generation_0\network_00.onnx C:\TzarBot\Maps\training-0.tzared 35 C:\TzarBot\Results\network_00_test.json

echo.
echo === Test Complete ===
echo Results saved to: C:\TzarBot\Results\network_00_test.json
pause
