// MathLibrary.cpp : Defines the exported functions for the DLL.
#include "pch.h" // use stdafx.h in Visual Studio 2017 and earlier
#include <utility>
#include <limits.h>
#include "LZ4Wrap.h"
#include <cassert>
#include <stdio.h>
#define Check(assert,errorcode) if(!(assert)) {printf(LZ4F_getErrorName(errorcode)); return errorcode;}



static CContext* CreateCompressContext()
{
    return (CContext*)malloc(sizeof(CContext));
}

void FreeCompressContext(CContext* ctx)
{
    if (ctx != NULL)
    {
        LZ4F_freeCompressionContext(ctx->cctx);
        LZ4F_freeCDict(ctx->dict);
        free(ctx);
    }
}

const LZ4F_preferences_t kPrefs = {
    { LZ4F_max4MB, LZ4F_blockLinked, LZ4F_contentChecksumEnabled, LZ4F_frame,
     0 /* unknown content size */, 0/* no dictID */ , LZ4F_blockChecksumEnabled },
    0,   /* compression level; 0 == default */
    0,   /* autoflush */
    0,   /* favor decompression speed */
    { 0, 0, 0 },  /* reserved, must be set to 0 */
};

size_t CalCompressOutBufferSize(size_t inBufferSize)
{
    return LZ4F_compressBound(inBufferSize, &kPrefs) + LZ4F_HEADER_SIZE_MAX;
}

CContext* CreateCompressContext(void* dictBuffer, size_t dictSize) {
    CContext* ctx = CreateCompressContext();
    if (dictBuffer)
        ctx->dict = LZ4F_createCDict(dictBuffer, dictSize);
    else
        ctx->dict = NULL;
    if (ctx == NULL) return NULL;
    LZ4F_compressionContext_t innerctx;

    LZ4F_errorCode_t ctxCreation = LZ4F_createCompressionContext(&innerctx, LZ4F_VERSION);
    if (LZ4F_isError(ctxCreation))
    {
        LZ4F_freeCompressionContext(innerctx);
        FreeCompressContext(ctx);
        return NULL;
    }
    ctx->cctx = innerctx;

    return ctx;
}

size_t CompressBegin(CContext** pctx, void* outBuff , size_t outCapacity, void* dictBuffer, size_t dictSize)
{
    CContext* ctx = CreateCompressContext(dictBuffer, dictSize);
    if (ctx == NULL) return -1;

    if (outCapacity < LZ4F_HEADER_SIZE_MAX || outCapacity < LZ4F_compressBound(0, &kPrefs)) return LZ4F_ERROR_dstMaxSize_tooSmall;
    

    /* write frame header */
    size_t const headerSize = ctx->dict == NULL 
        ? LZ4F_compressBegin(ctx->cctx, outBuff, outCapacity, &kPrefs)
        : LZ4F_compressBegin_usingCDict(ctx->cctx, outBuff, outCapacity, ctx->dict, &kPrefs);

    if (LZ4F_isError(headerSize))
    {
        return headerSize;
    }
    *pctx = ctx;
    return headerSize;
}


size_t CompressUpdate(CContext* ctx,void* dstBuffer, size_t dstCapacity,const void* srcBuffer, size_t srcSize)
{
    auto result = ctx->dict == NULL
        ? LZ4F_compressUpdate(ctx->cctx, dstBuffer, dstCapacity, srcBuffer, srcSize, NULL)
        : LZ4F_compressFrame_usingCDict(ctx->cctx, dstBuffer, dstCapacity, srcBuffer, srcSize, ctx->dict, NULL);
    if (LZ4F_isError(result))
    {
        auto str = LZ4F_getErrorName(result);
        printf(str);
    }
    return result;
}

size_t CompressEnd(CContext* ctx, void* dstBuffer, size_t dstCapacity)
{
    size_t writeSize = LZ4F_compressEnd(ctx->cctx, dstBuffer, dstCapacity, NULL);
    return writeSize;
}

static size_t get_block_size(const LZ4F_frameInfo_t* info) {
    switch (info->blockSizeID) {
    case LZ4F_default:
    case LZ4F_max64KB:  return 1 << 16;
    case LZ4F_max256KB: return 1 << 18;
    case LZ4F_max1MB:   return 1 << 20;
    case LZ4F_max4MB:   return 1 << 22;
    default:
        return -1;
    }
}
//return: input bytes expects for next call
size_t DecompressBegin(LZ4F_dctx **pdctx,void *inBuffer,size_t *inBufferSize, size_t *blockSize)
{
    LZ4F_dctx* dctx;
    LZ4F_dctx** _pdctx = &dctx;
    size_t const dctxStatus = LZ4F_createDecompressionContext(_pdctx, LZ4F_VERSION);
    Check(!LZ4F_isError(dctxStatus), dctxStatus);

    Check(*inBufferSize >= LZ4F_HEADER_SIZE_MAX, LZ4F_ERROR_dstMaxSize_tooSmall);
    
    LZ4F_frameInfo_t info;
    size_t const fires = LZ4F_getFrameInfo(*_pdctx, &info, inBuffer, inBufferSize);
    Check(!LZ4F_isError(fires), fires);

    *blockSize = get_block_size(&info);
    *pdctx = *_pdctx;
    return fires;
}

void ResetDecompresssCTX(LZ4F_dctx* dctx)
{
    LZ4F_resetDecompressionContext(dctx);
}

size_t DecompressUpdate(LZ4F_dctx* dctx, void* outBuffer, size_t * outBufferSize, void* inBuffer, size_t * inBufferSize,void* dict,size_t dictSize)
{
    size_t ret = dict == NULL
        ? LZ4F_decompress(dctx, outBuffer, outBufferSize, inBuffer, inBufferSize, NULL)
        : LZ4F_decompress_usingDict(dctx, outBuffer, outBufferSize, inBuffer, inBufferSize, dict, dictSize,NULL);
    Check(!LZ4F_isError(ret), ret);
    return ret;
}

size_t DecompressEnd(LZ4F_dctx* ctx)
{
    return LZ4F_freeDecompressionContext(ctx);
}