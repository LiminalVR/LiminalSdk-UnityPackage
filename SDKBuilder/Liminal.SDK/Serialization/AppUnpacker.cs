using SevenZip.Compression.LZMA;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;
using UnityEngine.Profiling;

namespace Liminal.SDK.Serialization
{
    /// <summary>
    /// Unpacks a Liminal App asynchronously. Due to the LZMA compression applied to AppPack files, unpacking can take time.
    /// The unpacker always runs asychronously on a separate thread.
    /// </summary>
    public class AppUnpacker
    {
        private Stream mInputStream;
        private Thread mThread;
        private ECompressionType mCompressionType;

        private volatile bool mRunning;
        private volatile bool mDone;
        private volatile bool mFaulted;

        #region Properties

        /// <summary>
        /// Indicates if the packing operation has completed.
        /// </summary>
        public bool IsDone
        {
            get { return !mRunning && mDone; }
        }

        /// <summary>
        /// Indicates if the unpacking operation faulted with an exception.
        /// </summary>
        public bool IsFaulted
        {
            get { return !mRunning && mFaulted; }
        }

        /// <summary>
        /// Gets the exception that was thrown if the unpacking operation faulted.
        /// </summary>
        public Exception Exception { get; private set; }

        /// <summary>
        /// If the unpacking operation was successfully completed, returns the unpacked data.
        /// </summary>
        public AppPack Data { get; private set; }

        #endregion
        
        /// <summary>
        /// Unpacks a raw compressed AppPack byte array.
        /// </summary>
        /// <param name="rawData">The raw compressed raw byte array to unpack.</param>
        public AppUnpacker UnpackAsync(byte[] rawData,ECompressionType compression = ECompressionType.LMZA)
        {
            if (mRunning)
            {
                Debug.LogFormat("An unpack operation is already in progress. Waiting for thread to join...");
            }
            
            if (mThread != null)
            {
                mThread.Join();
                mThread = null;
            }

            mRunning = true;
            mCompressionType = compression;
            ResetState();
            mInputStream = new MemoryStream(rawData);

            mThread = new Thread(DoUnpack)
            {
                Name = "AppUnpacker",
                IsBackground = true,
                Priority = System.Threading.ThreadPriority.Lowest
            };

            Debug.LogFormat("[AppUnpacker] Unpacking app on thread {0}", mThread.ManagedThreadId);
            mThread.Start();
            return this;
        }
        
        /// <summary>
        /// Wait for the unpacking process to complete.
        /// </summary>
        public void Wait()
        {
            if (mRunning && (mThread != null))
            {
                mThread.Join();
                mThread = null;
            }
        }

        private void ResetState()
        {
            mDone = false;
            mFaulted = false;
            Exception = null;
            Data = null;
        }

        private void DoUnpack()
        {
            //TODO: More recent Unity versions have Profiler.BeginThreadProfiling
            Profiler.BeginSample("DoUnpack");
            try
            {
                using (var outStream = new MemoryStream())
                {
                    Profiler.BeginSample("Decompress");
                    switch (mCompressionType)
                    {
                        case ECompressionType.LMZA:
                            SevenZipHelper.Decompress(mInputStream, outStream);
                            break;
                        case ECompressionType.Uncompressed:
                            mInputStream.CopyTo(outStream);
                            break;
                    }

                    outStream.Position = 0;

                    Data = UnpackDecompressed(outStream);
                    Profiler.EndSample();
                }

                mDone = true;
                mFaulted = false;
            }
            catch (Exception ex)
            {
                Exception = ex;
                mDone = true;
                mFaulted = true;
                Data = null;
            }
            finally
            {
                if (mInputStream != null)
                {
                    mInputStream.Close();
                    mInputStream.Dispose();
                    mInputStream = null;
                }
            }

            GC.Collect();

            // Done, thread will terminate
            mRunning = false;
            Profiler.EndSample();
        }

        private AppPack UnpackDecompressed(Stream stream)
        {
            Profiler.BeginSample("UnpackDecompressed");

            var pack = new AppPack();
            using (var reader = new BinaryReader(stream))
            {
                var version = reader.ReadInt16();

                // Verify header
                for (int i = 0; i < AppPack.Identifier.Length; ++i)
                {
                    if (reader.ReadByte() != AppPack.Identifier[i])
                        throw new FormatException("Unexpected identifier encountered");
                }

                pack.TargetPlatform = (AppPackPlatform)reader.ReadUInt16();
                pack.ApplicationId = (int)reader.ReadUInt32();
                pack.ApplicationVersion = (int)reader.ReadUInt32();

                int asmCount = reader.ReadByte();
                pack.Assemblies = new List<byte[]>(asmCount);
                for (int i = 0; i < asmCount; ++i)
                {
                    var asmLen = reader.ReadInt32();
                    if (asmLen > 0)
                    {
                        pack.Assemblies.Add(reader.ReadBytes(asmLen));
                    }
                }

                var sceneLen = reader.ReadInt32();
                if (sceneLen > 0)
                    pack.SceneBundle = reader.ReadBytes(sceneLen);
            }

            Profiler.EndSample();
            return pack;
        }
    }
}
