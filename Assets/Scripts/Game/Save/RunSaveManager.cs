using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace ProjectBlood
{
    /// <summary>
    /// 关卡进度存档管理器：单槽位文件 + 原子写 + bak 回退 + CRC32 校验 + XOR 混淆。
    /// </summary>
    public static class RunSaveManager
    {
        public const int Version = 1;
        public const string FileName = "run_save";
        public const string BakName = "run_save.bak";

        private static readonly byte[] ObfKey = new byte[] { 0x4A, 0x71, 0x2F, 0x8E, 0xB3, 0xD5, 0x19, 0x66 };
        private static readonly uint[] CrcTable = new uint[256];

        private static string SavePath => Path.Combine(Application.persistentDataPath, FileName);
        private static string BakPath => Path.Combine(Application.persistentDataPath, BakName);

        static RunSaveManager()
        {
            const uint poly = 0xEDB88320;
            for (uint i = 0; i < 256; i++)
            {
                uint crc = i;
                for (int j = 0; j < 8; j++)
                    crc = (crc & 1) == 1 ? (crc >> 1) ^ poly : crc >> 1;
                CrcTable[i] = crc;
            }
        }

        public static bool HasSave => File.Exists(SavePath);

        public static bool TryLoad(out RunSaveData data)
        {
            data = null;
            if (!HasSave) return false;

            try
            {
                data = LoadCore(SavePath);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[RunSaveManager] 主存档读取失败：{e.Message}，尝试回退 bak");

                if (File.Exists(BakPath))
                {
                    try
                    {
                        data = LoadCore(BakPath);
                        File.Copy(BakPath, SavePath, overwrite: true);
                        return true;
                    }
                    catch (Exception bakE)
                    {
                        Debug.LogWarning($"[RunSaveManager] bak 回退也失败：{bakE.Message}");
                    }
                }
            }

            return false;
        }

        public static void Save(RunSaveData data)
        {
            var json = JsonUtility.ToJson(data);
            var crc = ComputeCrc32(json);
            var obf = Obfuscate(json);

            // 格式：version:crc32:base64ObfuscatedJson（base64 不含冒号，Split(':') 安全）
            var content = $"{Version}:{crc:X8}:{obf}";
            var tmpPath = SavePath + ".tmp";

            if (File.Exists(SavePath))
            {
                if (File.Exists(BakPath)) File.Delete(BakPath);
                File.Copy(SavePath, BakPath);
            }

            File.WriteAllText(tmpPath, content, Encoding.UTF8);
            if (File.Exists(SavePath)) File.Delete(SavePath);
            File.Move(tmpPath, SavePath);
        }

        public static void DeleteSave()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
            if (File.Exists(BakPath)) File.Delete(BakPath);
        }

        private static RunSaveData LoadCore(string path)
        {
            var content = File.ReadAllText(path, Encoding.UTF8);
            var parts = content.Split(':', 3);
            if (parts.Length != 3)
                throw new FormatException("存档格式异常");

            if (!int.TryParse(parts[0], out var version) || version != Version)
                throw new FormatException($"存档版本不匹配：期望 {Version}，实际 {parts[0]}");

            if (!uint.TryParse(parts[1], System.Globalization.NumberStyles.HexNumber, null, out var expectedCrc))
                throw new FormatException("CRC32 格式异常");

            var json = Deobfuscate(parts[2]);
            var actualCrc = ComputeCrc32(json);
            if (actualCrc != expectedCrc)
                throw new InvalidDataException("CRC32 校验失败");

            var data = JsonUtility.FromJson<RunSaveData>(json);
            if (data == null)
                throw new InvalidDataException("反序列化结果为 null");

            return data;
        }

        public static uint ComputeCrc32(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            uint crc = 0xFFFFFFFF;
            foreach (var b in bytes)
                crc = (crc >> 8) ^ CrcTable[(crc ^ b) & 0xFF];
            return ~crc;
        }

        private static string Obfuscate(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] ^= ObfKey[i % ObfKey.Length];
            return Convert.ToBase64String(bytes);
        }

        private static string Deobfuscate(string input)
        {
            var bytes = Convert.FromBase64String(input);
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] ^= ObfKey[i % ObfKey.Length];
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
