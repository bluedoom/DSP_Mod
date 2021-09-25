@REM xcopy "LZ4.dll" "x64/LZ4.dll" /y
mkdir "CompressSave\Release\x64"
move "%cd%\CompressSave\Release\LZ4.dll" "%cd%\CompressSave\Release\x64\LZ4.dll" 
echo F| xcopy "LZ4\static\LICENSE" "CompressSave\Release\x64\LICENSE" /y

pushd "CompressSave\Release"
"H:\Tools\7z\7za.exe" a CompressSave.zip ./x64 ./CompressSave.dll
"H:\Tools\7z\7za.exe" a CompressSavePack.zip ./x64 ./CompressSave.dll ../icon.png ../manifest.json ../README.md
move CompressSave.zip ..\..\CompressSave.zip
move CompressSavePack.zip ..\..\CompressSavePack.zip
popd

mkdir "UnzipSave\Release\x64"
move "%cd%\UnzipSave\Release\LZ4.dll" "%cd%\UnzipSave\Release\x64\LZ4.dll" 
echo F| xcopy "LZ4\static\LICENSE" "UnzipSave\Release\x64\LICENSE" /y

pushd "UnzipSave\Release"
"H:\Tools\7z\7za.exe" a UnzipSave.zip ./UnzipSave.exe ./x64 ./MonoMod.Utils.dll
move UnzipSave.zip ..\..\UnzipSave.zip
popd