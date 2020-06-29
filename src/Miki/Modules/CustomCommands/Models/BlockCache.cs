using System;
using System.IO;
using MiScript;
using ProtoBuf;

namespace Miki.Modules.CustomCommands
{
    [ProtoContract]
    public class BlockCache
    {
        public BlockCache()
        {
        }

        private BlockCache(byte[] bytes)
        {
            Bytes = bytes;
        }

        [ProtoMember(1)]
        public byte[] Bytes { get; }

        public static BlockCache Create(Block block)
        {
            using var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);
                
            writer.WriteBlock(block);
            writer.Flush();

            return new BlockCache(stream.ToArray());
        }
    }
}