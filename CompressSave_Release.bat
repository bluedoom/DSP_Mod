@REM xcopy "LZ4.dll" "x64/LZ4.dll" /y
set sz=%cd%\7z\7za.exe
del CompressSavePack.zip
del CompressSave.zip
del UnzipSave.zip

mkdir "CompressSave\Release\x64"
move "%cd%\CompressSave\Release\LZ4.dll" "%cd%\CompressSave\Release\x64\LZ4.dll" 
echo F| xcopy "LZ4\static\LICENSE" "CompressSave\Release\x64\LICENSE" /y

pushd "CompressSave\Release"
%sz% a CompressSave.zip ./x64 ./CompressSave.dll ./System.Runtime.CompilerServices.Unsafe.dll
%sz% a CompressSavePack.zip ./x64 ./CompressSave.dll ./System.Runtime.CompilerServices.Unsafe.dll ../icon.png ../manifest.json ../README.md
move CompressSave.zip ..\..\CompressSave.zip
move CompressSavePack.zip ..\..\CompressSavePack.zip
popd

mkdir "UnzipSave\Release\x64"
move "%cd%\UnzipSave\Release\LZ4.dll" "%cd%\UnzipSave\Release\x64\LZ4.dll" 
echo F| xcopy "LZ4\static\LICENSE" "UnzipSave\Release\x64\LICENSE" /y

pushd "UnzipSave\Release"
%sz% a UnzipSave.zip ./UnzipSave.exe ./x64 ./MonoMod.Utils.dll
move UnzipSave.zip ..\..\UnzipSave.zip
popd