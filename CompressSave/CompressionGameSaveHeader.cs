using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DSP_Plugin
{
    class CompressionGameSaveHeader:GameSaveHeader
    {
        public bool IsCompressed = false;

        public CompressionGameSaveHeader() { }

        public CompressionGameSaveHeader(GameSaveHeader toCopy)
        {
            headerVersion = toCopy.headerVersion;
            fileSize = toCopy.fileSize;
            gameClientVersion = toCopy.gameClientVersion;
            gameTick = toCopy.gameTick;
            saveTime = toCopy.saveTime;
            themeImage = toCopy.themeImage;
            accountData = toCopy.accountData;
            clusterGeneration = toCopy.clusterGeneration;
        }
    }
}
