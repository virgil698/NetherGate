using fNbt;
using System.IO.Compression;
using NetherGate.API.Logging;

namespace NetherGate.Core.Data;

/// <summary>
/// Anvil 区块文件格式读取器
/// 用于解析 Minecraft 的 MCA 区块文件
/// </summary>
public class AnvilChunkReader
{
    private readonly ILogger _logger;

    public AnvilChunkReader(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 从 MCA 文件读取指定区块的 NBT 数据
    /// </summary>
    /// <param name="regionFilePath">区域文件路径 (r.x.z.mca)</param>
    /// <param name="chunkX">区块 X 坐标（世界坐标）</param>
    /// <param name="chunkZ">区块 Z 坐标（世界坐标）</param>
    /// <returns>区块的 NBT 数据，如果不存在则返回 null</returns>
    public async Task<NbtCompound?> ReadChunkAsync(string regionFilePath, int chunkX, int chunkZ)
    {
        if (!File.Exists(regionFilePath))
        {
            _logger.Debug($"区域文件不存在: {regionFilePath}");
            return null;
        }

        try
        {
            // 计算区块在区域文件中的索引
            var localX = ((chunkX % 32) + 32) % 32;
            var localZ = ((chunkZ % 32) + 32) % 32;
            var chunkIndex = localX + localZ * 32;

            using var fileStream = File.OpenRead(regionFilePath);
            
            // 读取区块位置表（前4096字节）
            fileStream.Seek(chunkIndex * 4, SeekOrigin.Begin);
            var locationBytes = new byte[4];
            await fileStream.ReadExactlyAsync(locationBytes, 0, 4);

            // 解析位置：前3字节是偏移量（以4KB为单位），第4字节是扇区数量
            var offset = ((locationBytes[0] << 16) | (locationBytes[1] << 8) | locationBytes[2]) * 4096;
            var sectorCount = locationBytes[3];

            if (offset == 0 || sectorCount == 0)
            {
                // 区块未生成
                _logger.Debug($"区块 [{chunkX}, {chunkZ}] 未生成");
                return null;
            }

            // 跳转到区块数据位置
            fileStream.Seek(offset, SeekOrigin.Begin);

            // 读取区块数据长度（4字节）
            var lengthBytes = new byte[4];
            await fileStream.ReadExactlyAsync(lengthBytes, 0, 4);
            var length = (lengthBytes[0] << 24) | (lengthBytes[1] << 16) | 
                        (lengthBytes[2] << 8) | lengthBytes[3];

            if (length == 0 || length > sectorCount * 4096)
            {
                _logger.Warning($"区块数据长度异常: {length}");
                return null;
            }

            // 读取压缩类型（1字节）
            var compressionTypeByte = fileStream.ReadByte();
            if (compressionTypeByte == -1)
            {
                _logger.Warning("无法读取压缩类型");
                return null;
            }

            var compressionType = (byte)compressionTypeByte;
            
            // 读取压缩的NBT数据
            var compressedData = new byte[length - 1]; // 减去压缩类型字节
            await fileStream.ReadExactlyAsync(compressedData, 0, compressedData.Length);

            // 解压缩数据
            var nbtData = await DecompressDataAsync(compressedData, compressionType);
            if (nbtData == null)
            {
                _logger.Warning($"解压缩区块数据失败: {compressionType}");
                return null;
            }

            // 解析 NBT 数据
            using var memoryStream = new MemoryStream(nbtData);
            var nbtFile = new NbtFile();
            nbtFile.LoadFromStream(memoryStream, NbtCompression.None);

            return nbtFile.RootTag;
        }
        catch (Exception ex)
        {
            _logger.Error($"读取区块 [{chunkX}, {chunkZ}] 失败: {ex.Message}", ex);
            return null;
        }
    }

    /// <summary>
    /// 从区块 NBT 数据中获取方块实体列表
    /// </summary>
    /// <param name="chunkNbt">区块的 NBT 数据</param>
    /// <returns>方块实体的 NBT 数据列表</returns>
    public List<NbtCompound> GetBlockEntitiesFromChunk(NbtCompound? chunkNbt)
    {
        var blockEntities = new List<NbtCompound>();

        if (chunkNbt == null)
            return blockEntities;

        try
        {
            // 1.18+ 格式：block_entities 在根标签下
            if (chunkNbt.Get<NbtList>("block_entities") is NbtList blockEntitiesList)
            {
                foreach (var blockEntity in blockEntitiesList.OfType<NbtCompound>())
                {
                    blockEntities.Add(blockEntity);
                }
                return blockEntities;
            }

            // 旧格式：TileEntities 在 Level 标签下
            if (chunkNbt.Get<NbtCompound>("Level") is NbtCompound levelTag)
            {
                if (levelTag.Get<NbtList>("TileEntities") is NbtList tileEntitiesList)
                {
                    foreach (var blockEntity in tileEntitiesList.OfType<NbtCompound>())
                    {
                        blockEntities.Add(blockEntity);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"解析方块实体列表失败: {ex.Message}", ex);
        }

        return blockEntities;
    }

    /// <summary>
    /// 根据坐标从方块实体列表中查找特定方块实体
    /// </summary>
    /// <param name="blockEntities">方块实体列表</param>
    /// <param name="x">X 坐标</param>
    /// <param name="y">Y 坐标</param>
    /// <param name="z">Z 坐标</param>
    /// <returns>匹配的方块实体，如果不存在则返回 null</returns>
    public NbtCompound? FindBlockEntityByPosition(List<NbtCompound> blockEntities, int x, int y, int z)
    {
        foreach (var blockEntity in blockEntities)
        {
            // 1.18+ 格式使用单独的 x, y, z 标签
            var entityX = blockEntity.Get<NbtInt>("x")?.Value ?? int.MinValue;
            var entityY = blockEntity.Get<NbtInt>("y")?.Value ?? int.MinValue;
            var entityZ = blockEntity.Get<NbtInt>("z")?.Value ?? int.MinValue;

            if (entityX == x && entityY == y && entityZ == z)
            {
                return blockEntity;
            }
        }

        return null;
    }

    /// <summary>
    /// 解压缩区块数据
    /// </summary>
    /// <param name="compressedData">压缩的数据</param>
    /// <param name="compressionType">压缩类型 (1=GZip, 2=Zlib, 3=未压缩)</param>
    /// <returns>解压后的数据</returns>
    private async Task<byte[]?> DecompressDataAsync(byte[] compressedData, byte compressionType)
    {
        try
        {
            using var inputStream = new MemoryStream(compressedData);
            using var outputStream = new MemoryStream();

            switch (compressionType)
            {
                case 1: // GZip
                    using (var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress))
                    {
                        await gzipStream.CopyToAsync(outputStream);
                    }
                    break;

                case 2: // Zlib (Deflate with 2-byte header)
                    // 跳过 Zlib 头部（2字节）
                    inputStream.ReadByte();
                    inputStream.ReadByte();
                    
                    using (var deflateStream = new DeflateStream(inputStream, CompressionMode.Decompress))
                    {
                        await deflateStream.CopyToAsync(outputStream);
                    }
                    break;

                case 3: // 未压缩
                    return compressedData;

                default:
                    _logger.Warning($"未知的压缩类型: {compressionType}");
                    return null;
            }

            return outputStream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.Error($"解压缩失败: {ex.Message}", ex);
            return null;
        }
    }
}

