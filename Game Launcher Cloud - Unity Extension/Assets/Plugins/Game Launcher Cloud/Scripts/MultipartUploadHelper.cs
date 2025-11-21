using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;

namespace GameLauncherCloud
{
    /// <summary>
    /// Helper for multipart uploads matching CLI implementation
    /// </summary>
    public static class MultipartUploadHelper
    {
        // Constants matching CLI implementation
        private const long MULTIPART_THRESHOLD = 500L * 1024 * 1024; // 500 MB
        private const long STANDARD_PART_SIZE = 500L * 1024 * 1024; // 500 MB parts
        private const int BUFFER_SIZE = 81920; // 80 KB buffer (CLI standard)
        private const int MAX_PARTS = 10000;

        /// <summary>
        /// Check if file should use multipart upload
        /// </summary>
        public static bool ShouldUseMultipart(long fileSize)
        {
            return fileSize > MULTIPART_THRESHOLD;
        }

        /// <summary>
        /// Calculate part size for multipart upload
        /// </summary>
        public static long CalculatePartSize(long fileSize)
        {
            if (fileSize <= STANDARD_PART_SIZE)
            {
                return fileSize;
            }

            long partSize = STANDARD_PART_SIZE;
            int partCount = (int)Math.Ceiling((double)fileSize / partSize);

            if (partCount > MAX_PARTS)
            {
                throw new NotSupportedException(
                    $"File is too large. With 500 MB parts and max {MAX_PARTS} parts, " +
                    $"maximum supported file size is {(STANDARD_PART_SIZE * MAX_PARTS) / (1024L * 1024 * 1024)} GB. " +
                    $"Your file would require {partCount} parts.");
            }

            return partSize;
        }

        /// <summary>
        /// Calculate total number of parts needed
        /// </summary>
        public static int CalculatePartCount(long fileSize, long partSize)
        {
            return (int)Math.Ceiling((double)fileSize / partSize);
        }

        /// <summary>
        /// Get buffer size for streaming
        /// </summary>
        public static int GetBufferSize()
        {
            return BUFFER_SIZE;
        }
    }

    /// <summary>
    /// HttpContent wrapper that reports progress during upload (matching CLI implementation)
    /// </summary>
    internal class ProgressFileStreamContent : HttpContent
    {
        private readonly Stream fileStream;
        private readonly long totalBytes;
        private readonly Action<long, long> progress;
        private const int BufferSize = 81920; // 80 KB - matches CLI

        private long lastBytesRead = 0;
        private DateTime lastReportTime = DateTime.UtcNow;
        private const int SPEED_REPORT_INTERVAL_MS = 500;

        public ProgressFileStreamContent(Stream fileStream, long totalBytes, Action<long, long> progress)
        {
            this.fileStream = fileStream ?? throw new ArgumentNullException(nameof(fileStream));
            this.totalBytes = totalBytes;
            this.progress = progress;
        }

        protected override async Task SerializeToStreamAsync(Stream stream, System.Net.TransportContext context)
        {
            byte[] buffer = new byte[BufferSize];
            long totalBytesRead = 0;
            int bytesRead;

            while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await stream.WriteAsync(buffer, 0, bytesRead);
                totalBytesRead += bytesRead;

                // Report progress
                var now = DateTime.UtcNow;
                var timeSinceLastReport = (now - lastReportTime).TotalSeconds;

                if (timeSinceLastReport >= SPEED_REPORT_INTERVAL_MS / 1000.0)
                {
                    progress?.Invoke(totalBytesRead, totalBytes);
                    lastBytesRead = totalBytesRead;
                    lastReportTime = now;
                }
            }

            // Final progress report
            if (totalBytesRead > lastBytesRead)
            {
                progress?.Invoke(totalBytesRead, totalBytes);
            }
        }

        protected override bool TryComputeLength(out long length)
        {
            length = totalBytes;
            return true;
        }
    }

    /// <summary>
    /// Stream wrapper that limits reading to a specific number of bytes (for multipart uploads)
    /// </summary>
    internal class LimitedStream : Stream
    {
        private readonly Stream baseStream;
        private readonly long maxBytes;
        private long bytesRead = 0;

        public LimitedStream(Stream baseStream, long maxBytes)
        {
            this.baseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            this.maxBytes = maxBytes;
        }

        public override bool CanRead => baseStream.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => maxBytes;
        public override long Position
        {
            get => bytesRead;
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (bytesRead >= maxBytes)
                return 0;

            int bytesToRead = (int)Math.Min(count, maxBytes - bytesRead);
            int actualBytesRead = baseStream.Read(buffer, offset, bytesToRead);
            bytesRead += actualBytesRead;
            return actualBytesRead;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken)
        {
            if (bytesRead >= maxBytes)
                return 0;

            int bytesToRead = (int)Math.Min(count, maxBytes - bytesRead);
            int actualBytesRead = await baseStream.ReadAsync(buffer, offset, bytesToRead, cancellationToken);
            bytesRead += actualBytesRead;
            return actualBytesRead;
        }

        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
