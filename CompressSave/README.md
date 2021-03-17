# 存档压缩 CompressSave
## 更新/ Changes
### 1.1.0 (2021/03/17) (Game Version 0.6.17)
  * 增加UI按钮 / Add UI button


## 警告
使用Mod可能会损坏您的存档，请注意备份存档！
## 简介
  减少30%存档大小|减少75%保存时间 
  |原生|压缩后|
  |-|-|
  |50MB 0.8s|30MB 0.2s|
  |220M 3.6s|147M 0.7s|
  |1010M 19.1s|690M 3.6s|
  
  HDD + i7-4790K@4.4G + DDR3 2400MHz
## 安装
  解压至 [游戏根目录/BepInEx/plugins] 文件夹.(依赖 [BepinEx] )
  
[BepinEx]: https://github.com/BepInEx/BepInEx/releases

  首次安装插件时建议另存一个存档文件，可以正常保存/读取后再继续使用。
  插件安装成功后，除手动保存（原保存按钮）外所有存档方式会被替换为压缩存档。
  载入界面会提供解压按钮（绿色按键）。
## 版本不匹配的情况
1.加载界面无法正常加载压缩存档，但解压功能可以正常使用，此时需要手动解压存档并载入。

2.解压存档文件为“[Recovery]-存档名字[n].dsv”。

## 使用外部工具解压存档(备用方案)
  将压缩存档拖拽到 [UnzipSave.exe] ，等待Success出现。解压文件为“[Recovery]-存档名字[n].dsv”。

[UnzipSave.exe]: https://github.com/bluedoom/DSP_Mod/releases

# Archive compression CompressSave
## Warning
Using Mod may damage your archive, please pay attention to backup archive!
## Introduction
  Reduce archive size by 30% | Reduce save time by 75%
  |Native|After Compression|
  |-|-|
  | 50MB 0.8s | 30MB 0.2s |
  | 220M 3.6s | 147M 0.7s |
  | 1010M 19.1s | 690M 3.6s |
  
  Hard Disk + i7-4790K@4.4G + DDR3 2400MHz
## Installation
  Unzip it to the [game root directory/BepInEx/plugins] folder. (Depends on [BepinEx])
  
[BepinEx]: https://github.com/BepInEx/BepInEx/releases

  When installing the plug-in for the first time, it is recommended to save another archive file, which can be saved/read normally before continuing to use.
  While the plug-in is installed successfully, all save actions will be replaced with compressed vserion,except manual saving. (original saving button).
  The loading interface will provide a decompression button (green button).
## Version Mismatch
1. The loading interface cannot load the compressed archive normally, but the decompression function can be used normally. At this time, you need to manually decompress the archive and load it.

2. Unzip the archive file as "[Recovery]-Archive name[n].dsv".

## External Tools (Alternative)

Drag the compressed archive to [UnzipSave.exe] and wait for Success to appear. The decompressed file is "[Recovery]-Archive Name[n].dsv".

[UnzipSave.exe]: https://github.com/bluedoom/DSP_Mod/releases