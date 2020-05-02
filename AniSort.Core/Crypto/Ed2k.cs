// Copyright © 2020 Lorathas
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation
// files (the “Software”), to deal in the Software without restriction, including without limitation the rights to use, copy,
// modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR
// IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AniSort.Core.Crypto
{
    /// <summary>
    /// Functions for generating eD2k hashes
    /// </summary>
    public static class Ed2k
    {
        /// <summary>
        /// Chunk size to split the file into and then MD4 hash
        /// </summary>
        private const int ChunkSize = 9500 * 1024;

        /// <summary>
        /// Generate eD2k hash from stream
        /// </summary>
        /// <param name="stream">Stream to hash</param>
        /// <returns>eD2k hash in bytes</returns>
        public static byte[] Hash(Stream stream)
        {
            byte[] buffer = new byte[ChunkSize];

            var hashes = new List<byte[]>();

            int count;
            while ((count = stream.Read(buffer, 0, ChunkSize)) > 0)
            {
                if (count < ChunkSize)
                {
                    byte[] tempBuffer = new byte[count];

                    for (int idx = 0; idx < count; idx++)
                    {
                        tempBuffer[idx] = buffer[idx];
                    }

                    hashes.Add(Md4.Hash(tempBuffer));
                }
                else
                {
                    hashes.Add(Md4.Hash(buffer));
                }
            }

            if (hashes.Count == 0)
            {
                return null;
            }

            if (hashes.Count == 1)
            {
                return hashes[0];
            }
            else
            {
                var concatenatedHashes = new List<byte>();

                foreach (var hash in hashes)
                {
                    concatenatedHashes.AddRange(hash);
                }

                return Md4.Hash(concatenatedHashes.ToArray());
            }
        }

        /// <summary>
        /// Generate eD2k hash of a file asynchronously
        /// </summary>
        /// <param name="stream">Stream to hash</param>
        /// <returns>Hash result as a byte array</returns>
        public static async Task<byte[]> HashAsync(Stream stream)
        {
            byte[] buffer = new byte[ChunkSize];

            var hashes = new List<byte[]>();

            int count;
            while ((count = await stream.ReadAsync(buffer, 0, ChunkSize)) > 0)
            {
                if (count < ChunkSize)
                {
                    byte[] tempBuffer = new byte[count];

                    for (int idx = 0; idx < count; idx++)
                    {
                        tempBuffer[idx] = buffer[idx];
                    }

                    hashes.Add(Md4.Hash(tempBuffer));
                }
                else
                {
                    hashes.Add(Md4.Hash(buffer));
                }
            }

            if (hashes.Count == 0)
            {
                return null;
            }

            if (hashes.Count == 1)
            {
                return hashes[0];
            }
            else
            {
                var concatenatedHashes = new List<byte>();

                foreach (var hash in hashes)
                {
                    concatenatedHashes.AddRange(hash);
                }

                return Md4.Hash(concatenatedHashes.ToArray());
            }
        }

        /// <summary>
        /// Asynchronously generate an eD2k hash from a Stream.
        /// </summary>
        /// <remarks>
        /// This method uses the ThreadPool to speed up the computation of the eD2k hash. Since the eD2k hash is an MD4 hash of MD4
        /// hashes of each 9500 KB chunk of the file this can be parallelized fairly easily. Hashing of each chunk is pushed to the
        /// ThreadPool for the sake of speeding up the hashing. I've observed an approximately 4x increase in processing speed on
        /// my 8 core CPU so YMMV.
        /// </remarks>
        /// <param name="stream"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        public static async Task<byte[]> HashMultiAsync(Stream stream, IProgress<long> progress = null)
        {
            object progressUpdateLock = new object();

            byte[] buffer = new byte[ChunkSize];

            var completedHashes = new ConcurrentDictionary<int, byte[]>();

            int index = 0;
            int count;
            long processedBytes = 0;
            while ((count = await stream.ReadAsync(buffer, 0, ChunkSize)) > 0)
            {
                byte[] tempBuffer = new byte[count];

                for (int idx = 0; idx < count; idx++)
                {
                    tempBuffer[idx] = buffer[idx];
                }

                ThreadPool.QueueUserWorkItem((args) =>
                {
                    var tuple = (Tuple<int, byte[]>) args;

                    var hash = Md4.Hash(tuple.Item2);

                    completedHashes[tuple.Item1] = hash;

                    lock (progressUpdateLock)
                    {
                        processedBytes += tuple.Item2.Length;
                        progress?.Report(processedBytes);
                    }
                }, new Tuple<int, byte[]>(index, tempBuffer));

                index++;
            }

            while (completedHashes.Count < index)
            {
                Thread.Sleep(50);
            }

            var hashes = new List<byte[]>();

            foreach (int idx in completedHashes.Keys.OrderBy(i => i))
            {
                hashes.Add(completedHashes[idx]);
            }

            if (hashes.Count == 0)
            {
                return null;
            }

            if (hashes.Count == 1)
            {
                return hashes[0];
            }
            else
            {
                var concatenatedHashes = new List<byte>();

                foreach (var hash in hashes)
                {
                    concatenatedHashes.AddRange(hash);
                }

                return Md4.Hash(concatenatedHashes.ToArray());
            }
        }

        public static async Task<byte[]> HashParallelAsync(Stream stream, IProgress<long> progress = null, int? maxThreads = null)
        {
            if (maxThreads == null)
            {
                maxThreads = Environment.ProcessorCount;
            }

            object progressUpdateLock = new object();

            byte[] buffer = new byte[ChunkSize];

            var completedHashes = new ConcurrentDictionary<int, byte[]>();

            var pendingChunksToHash = new List<(int Index, byte[] Chunk)>();

            int index = 0;
            int count;
            long processedBytes = 0;
            while ((count = await stream.ReadAsync(buffer, 0, ChunkSize)) > 0)
            {
                byte[] tempBuffer = new byte[count];

                for (int idx = 0; idx < count; idx++)
                {
                    tempBuffer[idx] = buffer[idx];
                }

                if (pendingChunksToHash.Count == maxThreads.Value * 4)
                {
                    Parallel.ForEach(pendingChunksToHash,
                        new ParallelOptions {MaxDegreeOfParallelism = maxThreads.Value}, 
                        pair =>
                        {
                            byte[] hash = Md4.Hash(pair.Chunk);

                            completedHashes[pair.Index] = hash;

                            lock (progressUpdateLock)
                            {
                                processedBytes += pair.Chunk.Length;
                                progress?.Report(processedBytes);
                            }
                        });
                    pendingChunksToHash.Clear();
                }

                pendingChunksToHash.Add((index, tempBuffer));

                index++;
            }

            var hashes = new List<byte[]>();

            foreach (int idx in completedHashes.Keys.OrderBy(i => i))
            {
                hashes.Add(completedHashes[idx]);
            }

            if (hashes.Count == 0)
            {
                return null;
            }

            if (hashes.Count == 1)
            {
                return hashes[0];
            }
            else
            {
                var concatenatedHashes = new List<byte>();

                foreach (var hash in hashes)
                {
                    concatenatedHashes.AddRange(hash);
                }

                return Md4.Hash(concatenatedHashes.ToArray());
            }
        }
    }
}