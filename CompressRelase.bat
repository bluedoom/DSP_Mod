cd release
mkdir x64
xcopy "LZ4.dll" "x64/LZ4.dll" /y
echo F| xcopy "..\LZ4\static\LICENCE" "x64\LICENCE" /y
@echo off
"E:\Tools\7z\7za.exe" a CompressSave.zip ./x64 ./CompressSave.dll
"E:\Tools\7z\7za.exe" a UnzipSave.zip ./UnzipSave.exe ./x64 ./MonoMode.Utils.dll

move CompressSave.zip ..\CompressSave.zip
move UnzipSave.zip ..\UnzipSave.zip