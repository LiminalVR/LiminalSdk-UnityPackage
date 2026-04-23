using System;
using System.IO;

namespace SevenZip.Compression.LZMA
{
    public static class SevenZipHelper
    {
        static int dictionary = 1 << 23;
        static bool eos = false;

        static CoderPropID[] propIDs = 
		{
			CoderPropID.DictionarySize,
			CoderPropID.PosStateBits,
			CoderPropID.LitContextBits,
			CoderPropID.LitPosBits,
			CoderPropID.Algorithm,
			CoderPropID.NumFastBytes,
			CoderPropID.MatchFinder,
			CoderPropID.EndMarker
		};

        // these are the default properties, keeping it simple for now:
        static object[] properties = 
		{
			(int)(dictionary),
			(int)(2),
			(int)(3),
			(int)(0),
			(int)(2),
			(int)(128),
			"bt4",
			eos
		};
        
        public static byte[] Compress(byte[] inputBytes)
        {
            using (var inStream = new MemoryStream(inputBytes))
            using (var outStream = new MemoryStream())
            {
                Compress(inStream, outStream);
                return outStream.ToArray();
            }
        }

        public static void Compress(Stream inStream, Stream outStream)
        {
            var encoder = new Encoder();
            encoder.SetCoderProperties(propIDs, properties);
            encoder.WriteCoderProperties(outStream);
            long fileSize = inStream.Length;
            for (int i = 0; i < 8; i++)
            {
                outStream.WriteByte((Byte)(fileSize >> (8 * i)));
            }

            encoder.Code(inStream, outStream, -1, -1, null);
        }

        public static byte[] Decompress(byte[] inputBytes)
        {
            using (var inStream = new MemoryStream(inputBytes))
            using (var outStream = new MemoryStream())
            {
                Decompress(inStream, outStream);
                return outStream.ToArray();
            }
        }

        public static byte[] Decompress(Stream inStream)
        {
            using (var outStream = new MemoryStream())
            {
                Decompress(inStream, outStream);
                return outStream.ToArray();
            }
        }

        public static void Decompress(Stream inStream, Stream outStream)
        {
            var decoder = new Decoder();

            inStream.Seek(0, 0);
            
            byte[] properties2 = new byte[5];
            if (inStream.Read(properties2, 0, 5) != 5)
                throw (new Exception("input .lzma is too short"));
            long outSize = 0;
            for (int i = 0; i < 8; i++)
            {
                int v = inStream.ReadByte();
                if (v < 0)
                    throw (new Exception("Can't Read 1"));
                outSize |= ((long)(byte)v) << (8 * i);
            }
            decoder.SetDecoderProperties(properties2);

            long compressedSize = inStream.Length - inStream.Position;
            decoder.Code(inStream, outStream, compressedSize, outSize, null);
        }
    }
}
