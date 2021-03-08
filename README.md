# 存档压缩插件 SaveCompress
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
  
[BepinEx]: https://github.com/BepInEx/BepInEx/releases "BepinEx"

  首次安装插件时建议另存一个存档文件，可以正常保存/读取后再继续使用。
## 版本不匹配的情况
1.插件会自动失效，退回游戏原生存储方式。

2.插件会尝试解压存档文件为“[Recovery]-存档名字.dsv”，再使用原生方式加载，此过程会导致读取存档时间较长，请耐心等待。

3.将存档拖拽到 UnzipSave.exe 文件会按照上述格式解压。
