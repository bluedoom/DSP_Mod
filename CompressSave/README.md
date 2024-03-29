# 存档压缩 CompressSave
请注意时常使用原存档按钮备份非压缩存档！！！
Please pay attention to backup uncompressed file use orignal save button usually！！！

## 更新/ Changes
### 1.1.11
  * fix 1.1.10打包失败
### 1.1.10
  * 修复1.1.8使用DIY系统存档损坏问题，已经损坏的存档可以使用[Fix118]修复
  * Fix 1.1.8 Archive corruption with DIY System, corrupted archives can be fixed by using [Fix118] mod
  
  [Fix118]: https://github.com/bluedoom/DSP_Mod/blob/master/Fix118/README.MD

### 1.1.9
  * 因为对机甲DIY系统支持有问题，暂时关闭存储压缩功能
  * CompressSave is temporarily disabled due to some error with the DIY system.

### 1.1.8
  * 适配 0.9.24.11029

### 1.1.7
  * Fix: 统计面板信息不正确的问题 / statistic panel "Data" is not precise.
  * 进一步提升保存性能

### 1.1.6
  * fix memory leak

### 1.1.5 (Game Version 0.8.22)
  * 适配新版本 / Adapt new game release.
  * Thanks [@starfi5h] for
    - PatchSave now use transpiler for better robustness.
    - Change version check to soft warning.
    - Add PeekableReader so other mods can use BinaryReader.PeekChar().
    - Change LZ4DecompressionStream.Position behavior. Position setter i  - available now.

[@starfi5h]: https://github.com/starfi5h
### 1.1.4 (Game Version 0.8.19)
  * 适配新版本 / Adapt new game release.
### 1.1.3 (2021/05/29) (Game Version 0.7.18)
  * 适配新版本 / Adapt new game release.
  * 修复内存泄漏问题 / Fixed memory leak.
### 1.1.2 (2021/03/24) (Game Version 0.6.17)
  * Handle lz4 library missing Error
### 1.1.1 (2021/03/17) (Game Version 0.6.17)
  * Fix Load Error
### 1.1.0 (2021/03/17) (Game Version 0.6.17)
  * 增加UI按钮 / Add UI button

## 简介
  减少30%存档大小|减少75%保存时间 
  | 压缩前 | 压缩后 |
  | - | - |
  | 50MB 0.8s | 30MB 0.2s |
  | 220M 3.6s | 147M 0.7s |
  | 1010M 19.1s | 690M 3.6s |
  
  HDD + i7-4790K@4.4G + DDR3 2400MHz
## 安装
  解压至 [游戏根目录/BepInEx/plugins] 文件夹.(依赖 [BepinEx] )
  
[BepinEx]: https://github.com/BepInEx/BepInEx/releases

  首次安装插件时建议另存一个存档文件，可以正常保存/读取后再继续使用。
  插件安装成功后，除手动保存（原保存按钮）外所有存档方式会被替换为压缩存档。
  载入界面会提供解压按钮（绿色按键）。
## 版本不匹配的情况
加载界面无法正常加载压缩存档，但解压功能可以正常使用，此时需要手动解压存档并载入。解压存档文件为“[Recovery]-存档名字[n].dsv”。

## 使用外部工具解压存档(备用方案)
  将压缩存档拖拽到 [UnzipSave.exe] ，等待Success出现。解压文件为“[Recovery]-存档名字[n].dsv”。

[UnzipSave.exe]: https://github.com/bluedoom/DSP_Mod/releases

# Archive compression CompressSave

## Introduction
  Reduce archive size by 30% | Reduce save time by 75%
  | Before | After |
  | - | - |
  | 50MB 0.8s | 30MB 0.2s |
  | 220M 3.6s | 147M 0.7s |
  | 1010M 19.1s | 690M 3.6s |
  
  Hard Disk + i7-4790K@4.4G + DDR3 2400MHz
## Installation
  Unzip it to the [game root directory/BepInEx/plugins] folder. (Depends on [BepinEx])
  
[BepinEx]: https://github.com/BepInEx/BepInEx/releases

  When installing the plug-in for the first time, it is recommended to save another archive file, which can be saved/read normally before continuing to use.
  While the plug-in being installed successfully, all save actions will be replaced with compressed vserion,except manual saving. (original saving button).
  The loading interface will provide a decompression button (green button).
## Version Mismatch
The loading interface cannot load the compressed archive normally, but the decompression function can be used. At this time, you need to manually decompress the archive and load it, which name is "[Recovery]-Archive name[n].dsv".

## External Tools (Alternative)

Drag the compressed archive to [UnzipSave.exe] and wait for Success to appear. The decompressed file is "[Recovery]-Archive Name[n].dsv".

[UnzipSave.exe]: https://github.com/bluedoom/DSP_Mod/releases