# 存档压缩 CompressSave
## 警告
使用Mod可能会损坏您的存档，请注意备份存档！
## 简介
  减少30%存档大小|减少75%保存时间 
  |原生|压缩后|
  |----|----|
  |50MB 0.8s|30MB 0.2s|
  |220M 3.6s|147M 0.7s|
  |1010M 19.1s|690M 3.6s|
  
  HDD + i7-4790K@4.4G + DDR3 2400MHz
## 安装
  解压至 [游戏根目录/BepInEx/plugins] 文件夹.(依赖 [BepinEx] )
  
[BepinEx]: https://github.com/BepInEx/BepInEx/releases

  首次安装插件时建议另存一个存档文件，可以正常保存/读取后再继续使用。
## 版本不匹配的情况
1.插件会自动失效，退回游戏原生存储方式。

2.插件会尝试解压存档文件为“[Recovery]-存档名字.dsv”，再使用原生方式加载，此过程会导致读取存档时间较长，请耐心等待。

## 手动解压存档
  将压缩存档拖拽到 [UnzipSave.exe] ，等待Success出现。解压文件为“[Recovery]-存档名字.dsv”。

[UnzipSave.exe]: https://github.com/bluedoom/DSP_Mod/releases


#Archive compression plugin CompressSave
##caveat
Using Mod may damage your archive, please pay attention to backup archive!
##Introduction
  Reduce archive size by 30% | Reduce save time by 75%
  |Native|After Compression|
  | ---- | ---- |
  | 50MB 0.8s | 30MB 0.2s |
  | 220M 3.6s | 147M 0.7s |
  | 1010M 19.1s | 690M 3.6s |
  
  Hard Disk + i7-4790K@4.4G + DDR3 2400MHz
##installation
  Unzip it to the [game root directory/BepInEx/plugins] folder. (Depends on [BepinEx])
  
[BepinEx]: https://github.com/BepInEx/BepInEx/releases

  When installing the plug-in for the first time, it is recommended to save another archive file, which can be saved/read normally before continuing to use.
##The case of version mismatch
1. The plug-in will automatically expire and return to the native storage mode of the game.

2. The plug-in will try to decompress the archive file to "[Recovery]-Archive name.dsv", and then load it in native mode. This process will shorten the re-archiving time, please be patient.

##Manually decompress the archive
  The decompressed file is "[Recovery]-Archive name.dsv".

[UnzipSave.exe]: https://github.com/bluedoom/DSP_Mod/releases

