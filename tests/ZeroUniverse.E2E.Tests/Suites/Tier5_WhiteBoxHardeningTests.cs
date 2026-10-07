using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using ZeroPlatform.Concurrency;
using ZeroPrimitives.Buffers;
using ZeroPrimitives.Concurrency;
using ZeroPrimitives.Memory;
using ZeroText.Localization;
using ZeroText.Normalization;
using ZeroText.Validation;
using ZeroVideo.Color;
using ZeroVideo.Core;

namespace ZeroUniverse.E2E.Tests.Suites
{
    /// <summary>
    /// Milestone M4 Phase 2 - Adversarial White-Box Coverage Hardening (Tier 5).
    /// Empirically stress-tests upgraded primitives, concurrency structures, video frame memory,
    /// and Vietnamese sovereign text normalizers against bounds, overflows, and race conditions.
    /// </summary>
    public class Tier5_WhiteBoxHardeningTests
    {
        #region SECTION 1: ZeroPrimitives.Core (ArrayPoolRentScope, NativeMemoryBlock, FastSpinLock)

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        [InlineData(int.MinValue)]
        public void Test_Tier5_01_ArrayPoolRentScope_NegativeAndZeroBoundaryRental(int length)
        {
            var scope = ArrayPoolRentScope<byte>.Rent(length);
            Assert.Equal(0, scope.Length);
            Assert.True(scope.Span.IsEmpty);
            Assert.True(scope.ReadOnlySpan.IsEmpty);
            Assert.Null(scope.RawArray);

            // Dispose must be completely safe
            scope.Dispose();
            scope.Dispose();
            Assert.True(scope.Span.IsEmpty);
        }

        [Fact]
        public void Test_Tier5_02_ArrayPoolRentScope_PostDisposalStateInvariants()
        {
            var scope = ArrayPoolRentScope<int>.Rent(128);
            Assert.Equal(128, scope.Length);
            Assert.False(scope.Span.IsEmpty);
            Assert.NotNull(scope.RawArray);

            scope.Span[0] = 42;
            scope.Span[127] = 99;

            // First Dispose
            scope.Dispose();
            Assert.True(scope.Span.IsEmpty);
            Assert.True(scope.ReadOnlySpan.IsEmpty);
            Assert.Null(scope.RawArray);

            // Repeated Disposes (2nd through 10th) must be completely idempotent
            for (int i = 0; i < 9; i++)
            {
                scope.Dispose();
                Assert.True(scope.Span.IsEmpty);
                Assert.Null(scope.RawArray);
            }
        }

        [Fact]
        public void Test_Tier5_03_ArrayPoolRentScope_ConcurrentMassiveRentAndMultipleDispose_Stress()
        {
            const int threadCount = 12;
            const int iterationsPerThread = 5_000;
            var exceptions = new ConcurrentBag<Exception>();
            using var barrier = new Barrier(threadCount);
            var threads = new Thread[threadCount];

            for (int t = 0; t < threadCount; t++)
            {
                int threadId = t;
                threads[t] = new Thread(() =>
                {
                    try
                    {
                        var rand = new Random(threadId * 1000 + 7);
                        barrier.SignalAndWait();

                        for (int i = 0; i < iterationsPerThread; i++)
                        {
                            int size = (i % 7 == 0) ? 0 : rand.Next(1, 65536);
                            var scope = ArrayPoolRentScope<byte>.Rent(size);

                            if (size > 0)
                            {
                                scope.Span[0] = (byte)(threadId & 0xFF);
                                scope.Span[scope.Span.Length - 1] = (byte)(i & 0xFF);
                            }

                            // Multiple disposes simulating nested scopes & premature cleanup
                            scope.Dispose();
                            scope.Dispose();
                            scope.Dispose();

                            if (!scope.Span.IsEmpty || scope.RawArray != null)
                            {
                                throw new InvalidOperationException("Post-dispose invariant violated.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                })
                {
                    IsBackground = true
                };
                threads[t].Start();
            }

            foreach (var th in threads)
            {
                bool finished = th.Join(TimeSpan.FromSeconds(25));
                Assert.True(finished, "RentScope stress worker thread timed out.");
            }

            Assert.Empty(exceptions);

            // Verify ArrayPool freelist integrity: rent 100 arrays simultaneously without collision
            var seen = new HashSet<byte[]>(ReferenceEqualityComparer.Instance);
            var rented = new List<byte[]>();
            try
            {
                for (int i = 0; i < 100; i++)
                {
                    var arr = ArrayPool<byte>.Shared.Rent(1024);
                    Assert.True(seen.Add(arr), "Freelist corruption: duplicate array reference rented concurrently!");
                    rented.Add(arr);
                }
            }
            finally
            {
                foreach (var arr in rented) ArrayPool<byte>.Shared.Return(arr);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-4096)]
        [InlineData(int.MinValue)]
        public void Test_Tier5_04_NativeMemoryBlock_NonPositiveAllocationThrows(int badCapacity)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => NativeMemoryBlock.Allocate(badCapacity));
        }

        [Fact]
        public void Test_Tier5_05_NativeMemoryBlock_LengthBoundaryEnforcement()
        {
            using var block = NativeMemoryBlock.Allocate(1024);
            Assert.Equal(1024, block.Capacity);
            Assert.Equal(1024, block.Length);

            // Valid length adjustments
            block.Length = 500;
            Assert.Equal(500, block.Length);
            Assert.Equal(500, block.Span.Length);
            Assert.Equal(1024, block.CapacitySpan.Length);

            block.Length = 0;
            Assert.Equal(0, block.Length);
            Assert.True(block.Span.IsEmpty);

            block.Length = 1024;
            Assert.Equal(1024, block.Length);

            // Invalid length adjustments
            Assert.Throws<ArgumentOutOfRangeException>(() => block.Length = -1);
            Assert.Throws<ArgumentOutOfRangeException>(() => block.Length = 1025);
            Assert.Throws<ArgumentOutOfRangeException>(() => block.Length = int.MaxValue);
        }

        [Fact]
        public void Test_Tier5_06_NativeMemoryBlock_PostDisposalInvariantsAndMultiThreadedDispose()
        {
            var block = NativeMemoryBlock.Allocate(2048);
            block.Span.Fill(0xAA);

            // Multi-threaded concurrent dispose
            Parallel.For(0, 10, _ =>
            {
                block.Dispose();
            });

            Assert.True(block.IsDisposed);
            unsafe
            {
                Assert.Throws<ObjectDisposedException>(() => _ = block.Pointer);
            }
            Assert.Throws<ObjectDisposedException>(() => _ = block.Span);
            Assert.Throws<ObjectDisposedException>(() => _ = block.ReadOnlySpan);
            Assert.Throws<ObjectDisposedException>(() => _ = block.CapacitySpan);
            Assert.Throws<ObjectDisposedException>(() => block.Clear());
        }

        [Fact]
        public void Test_Tier5_07_NativeMemoryBlock_GCFinalizerSafety()
        {
            // Allocate blocks without explicit disposal; ensure GC collection invokes finalizer cleanly
            void AllocUnreferenced()
            {
                for (int i = 0; i < 50; i++)
                {
                    var b = NativeMemoryBlock.Allocate(4096);
                    b.Span[0] = 0x55;
                }
            }

            AllocUnreferenced();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            // Succeeds without access violation or heap corruption
        }

        [Fact]
        public void Test_Tier5_08_FastSpinLock_ContentionStress_MutualExclusion()
        {
            const int threadCount = 16;
            const int iterationsPerThread = 25_000;
            int counter = 0;
            var spinLock = new FastSpinLock();

            Parallel.For(0, threadCount, _ =>
            {
                for (int i = 0; i < iterationsPerThread; i++)
                {
                    spinLock.Enter();
                    try
                    {
                        counter++;
                    }
                    finally
                    {
                        spinLock.Exit();
                    }
                }
            });

            Assert.Equal(threadCount * iterationsPerThread, counter);
            Assert.False(spinLock.IsHeld);
        }

        [Fact]
        public void Test_Tier5_09_FastSpinLock_ScopeRAII_MultiThreadedStress()
        {
            const int threadCount = 16;
            const int iterationsPerThread = 25_000;
            int counter = 0;
            var spinLock = new FastSpinLock();

            Parallel.For(0, threadCount, _ =>
            {
                for (int i = 0; i < iterationsPerThread; i++)
                {
                    using (spinLock.EnterScope())
                    {
                        counter++;
                    }
                }
            });

            Assert.Equal(threadCount * iterationsPerThread, counter);
            Assert.False(spinLock.IsHeld);
        }

        [Fact]
        public void Test_Tier5_10_FastSpinLock_TryEnterAndIsHeldContracts()
        {
            var spinLock = new FastSpinLock();
            Assert.False(spinLock.IsHeld);

            Assert.True(spinLock.TryEnter());
            Assert.True(spinLock.IsHeld);

            bool secondaryAcquired = false;
            var thread = new Thread(() =>
            {
                secondaryAcquired = spinLock.TryEnter();
            });
            thread.Start();
            thread.Join();

            Assert.False(secondaryAcquired, "TryEnter must return false when lock is held by another thread.");

            spinLock.Exit();
            Assert.False(spinLock.IsHeld);

            // Re-acquisition after release succeeds
            Assert.True(spinLock.TryEnter());
            spinLock.Exit();
        }

        #endregion

        #region SECTION 2: ZeroConcurrency (ZeroNativeRingBuffer)

        [Theory]
        [InlineData(-100)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(5)]
        [InlineData(100)]
        [InlineData(1023)]
        [InlineData(1025)]
        public void Test_Tier5_11_ZeroNativeRingBuffer_NonPowerOfTwoAndInvalidCapacities(int cap)
        {
            var ex = Assert.Throws<ArgumentException>(() => new ZeroNativeRingBuffer(capacityPowerOfTwo: cap));
            Assert.Contains("power of two", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Test_Tier5_12_ZeroNativeRingBuffer_UndersizedDestinationSpan_PreservesQueue()
        {
            using var ring = new ZeroNativeRingBuffer(capacityPowerOfTwo: 16);

            byte[] p1 = new byte[80];
            p1[0] = 0xAA; p1[79] = 0xBB;
            byte[] p2 = new byte[120];
            p2[0] = 0xCC; p2[119] = 0xDD;

            Assert.True(ring.TryWrite(p1));
            Assert.True(ring.TryWrite(p2));
            Assert.Equal(2, ring.Count);

            // Undersized destination spans
            foreach (int badSize in new[] { 0, 1, 40, 79 })
            {
                byte[] undersized = new byte[badSize];
                var ex = Assert.Throws<ArgumentException>(() => ring.TryRead(undersized.AsSpan(), out _));
                Assert.Contains("smaller than payload block length", ex.Message);
                Assert.Equal(2, ring.Count);
            }

            // Adequate read
            byte[] exact = new byte[80];
            Assert.True(ring.TryRead(exact.AsSpan(), out int bytesRead));
            Assert.Equal(80, bytesRead);
            Assert.Equal(p1, exact);
            Assert.Equal(1, ring.Count);

            // Second read
            byte[] second = new byte[120];
            Assert.True(ring.TryRead(second.AsSpan(), out int bytesRead2));
            Assert.Equal(120, bytesRead2);
            Assert.Equal(p2, second);
            Assert.Equal(0, ring.Count);
        }

        [Fact]
        public void Test_Tier5_13_ZeroNativeRingBuffer_ZeroBytePayload_PreservesQueue()
        {
            using var ring = new ZeroNativeRingBuffer(capacityPowerOfTwo: 8);

            Assert.Throws<ArgumentOutOfRangeException>(() => ring.TryWrite(ReadOnlySpan<byte>.Empty));
            Assert.Equal(0, ring.Count);

            // Normal write immediately afterwards
            Assert.True(ring.TryWrite(new byte[] { 1, 2, 3 }));
            Assert.Equal(1, ring.Count);

            byte[] dest = new byte[3];
            Assert.True(ring.TryRead(dest, out int read));
            Assert.Equal(3, read);
            Assert.Equal(new byte[] { 1, 2, 3 }, dest);
        }

        [Fact]
        public async Task Test_Tier5_14_ZeroNativeRingBuffer_SPSC_HighThroughput_WrapAroundStress()
        {
            // Small ring buffer (16 slots) with 20,000 frames forces intensive wrap-around and backpressure
            using var ring = new ZeroNativeRingBuffer(capacityPowerOfTwo: 16);
            const int totalFrames = 20_000;
            var errors = new ConcurrentBag<string>();
            var sw = Stopwatch.StartNew();

            int[] primeSizes = { 3, 7, 13, 31, 67, 127, 251, 509 };

            var producer = Task.Run(() =>
            {
                for (int seq = 0; seq < totalFrames; seq++)
                {
                    int bodyLen = primeSizes[seq % primeSizes.Length];
                    byte[] packet = new byte[4 + bodyLen];
                    BitConverter.TryWriteBytes(packet.AsSpan(0, 4), seq);

                    for (int b = 0; b < bodyLen; b++)
                    {
                        packet[4 + b] = (byte)((seq + b) & 0xFF);
                    }

                    var spin = new SpinWait();
                    while (!ring.TryWrite(packet))
                    {
                        spin.SpinOnce();
                        if (sw.ElapsedMilliseconds > 20_000)
                        {
                            errors.Add($"Producer timed out at frame {seq}");
                            return;
                        }
                    }
                }
            });

            var consumer = Task.Run(() =>
            {
                byte[] recv = new byte[1024];
                for (int expectedSeq = 0; expectedSeq < totalFrames; expectedSeq++)
                {
                    var spin = new SpinWait();
                    int readBytes;
                    while (!ring.TryRead(recv.AsSpan(), out readBytes))
                    {
                        spin.SpinOnce();
                        if (sw.ElapsedMilliseconds > 20_000)
                        {
                            errors.Add($"Consumer timed out at frame {expectedSeq}");
                            return;
                        }
                    }

                    int bodyLen = primeSizes[expectedSeq % primeSizes.Length];
                    if (readBytes != 4 + bodyLen)
                    {
                        errors.Add($"Length mismatch at {expectedSeq}: expected {4 + bodyLen}, got {readBytes}");
                        return;
                    }

                    int actualSeq = BitConverter.ToInt32(recv, 0);
                    if (actualSeq != expectedSeq)
                    {
                        errors.Add($"Sequence mismatch: expected {expectedSeq}, got {actualSeq}");
                        return;
                    }

                    for (int b = 0; b < bodyLen; b++)
                    {
                        byte expectedByte = (byte)((expectedSeq + b) & 0xFF);
                        if (recv[4 + b] != expectedByte)
                        {
                            errors.Add($"Byte corruption at frame {expectedSeq}, byte {b}");
                            return;
                        }
                    }
                }
            });

            await Task.WhenAll(producer, consumer).WaitAsync(TimeSpan.FromSeconds(25));
            Assert.Empty(errors);
            Assert.Equal(0, ring.Count);
        }

        [Fact]
        public void Test_Tier5_15_ZeroNativeRingBuffer_DisposalIdempotencyAndStateInvariants()
        {
            var ring = new ZeroNativeRingBuffer(capacityPowerOfTwo: 16);
            ring.TryWrite(new byte[] { 1, 2, 3 });
            ring.TryWrite(new byte[] { 4, 5, 6 });

            // Idempotent double dispose
            ring.Dispose();
            ring.Dispose();

            Assert.True(ring.IsDisposed);
            Assert.Throws<ObjectDisposedException>(() => ring.TryWrite(new byte[] { 1 }));
            Assert.Throws<ObjectDisposedException>(() => ring.TryRead(new byte[10].AsSpan(), out _));
            Assert.Throws<ObjectDisposedException>(() => ring.TryPeek(out _));
        }

        #endregion

        #region SECTION 3: ZeroVideo (ColorConverter, VideoFramePool)

        [Theory]
        [InlineData(VideoPixelFormat.Yuv420p)]
        [InlineData(VideoPixelFormat.Nv12)]
        [InlineData(VideoPixelFormat.Rgb24)]
        public void Test_Tier5_16_ColorConverter_TruncatedSourceAndDestinationBuffers_Throw(VideoPixelFormat srcFmt)
        {
            int w = 32, h = 32;
            var targetFmt = (srcFmt == VideoPixelFormat.Rgb24) ? VideoPixelFormat.Gray8 : VideoPixelFormat.Rgb24;

            var validSrc = new VideoFrameBuffer(w, h, srcFmt);
            var validDst = new VideoFrameBuffer(w, h, targetFmt);

            // Truncated source buffer (1 byte)
            var truncSrc = new VideoFrameBuffer(w, h, srcFmt, new byte[1]);
            Assert.Throws<ArgumentException>(() =>
            {
                if (srcFmt == VideoPixelFormat.Yuv420p) ColorConverter.Yuv420pToRgb(truncSrc, validDst);
                else if (srcFmt == VideoPixelFormat.Nv12) ColorConverter.Nv12ToRgb(truncSrc, validDst);
                else ColorConverter.RgbToGray8(truncSrc, validDst);
            });

            // Truncated destination buffer (1 byte)
            var truncDst = new VideoFrameBuffer(w, h, targetFmt, new byte[1]);
            Assert.Throws<ArgumentException>(() =>
            {
                if (srcFmt == VideoPixelFormat.Yuv420p) ColorConverter.Yuv420pToRgb(validSrc, truncDst);
                else if (srcFmt == VideoPixelFormat.Nv12) ColorConverter.Nv12ToRgb(validSrc, truncDst);
                else ColorConverter.RgbToGray8(validSrc, truncDst);
            });
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(3, 3)]
        [InlineData(15, 15)]
        [InlineData(17, 9)]
        [InlineData(63, 63)]
        public void Test_Tier5_17_ColorConverter_OddDimensions_TrailingPixelSafety(int w, int h)
        {
            // Yuv420p
            var yuv = new VideoFrameBuffer(w, h, VideoPixelFormat.Yuv420p);
            var rgb1 = new VideoFrameBuffer(w, h, VideoPixelFormat.Rgb24);
            yuv.Data.AsSpan().Fill(128);
            ColorConverter.Yuv420pToRgb(yuv, rgb1);
            Assert.True(rgb1.Data.Length >= w * h * 3);

            // Nv12 (odd width trailing pixel index protection)
            var nv12 = new VideoFrameBuffer(w, h, VideoPixelFormat.Nv12);
            var rgb2 = new VideoFrameBuffer(w, h, VideoPixelFormat.Rgb24);
            nv12.Data.AsSpan().Fill(128);
            ColorConverter.Nv12ToRgb(nv12, rgb2);
            Assert.True(rgb2.Data.Length >= w * h * 3);

            // Rgb to Gray8
            var gray = new VideoFrameBuffer(w, h, VideoPixelFormat.Gray8);
            ColorConverter.RgbToGray8(rgb1, gray);
            Assert.True(gray.Data.Length >= w * h);
        }

        [Fact]
        public void Test_Tier5_18_ColorConverter_MismatchedDimensions_StrictlyThrows()
        {
            var src = new VideoFrameBuffer(64, 48, VideoPixelFormat.Rgb24);
            var dst = new VideoFrameBuffer(64, 32, VideoPixelFormat.Gray8);

            var ex = Assert.Throws<ArgumentException>(() => ColorConverter.RgbToGray8(src, dst));
            Assert.Contains("dimensions must match", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Test_Tier5_19_VideoFramePool_ExtremeOversized8K_SlabFallback()
        {
            // 8K frame: 7680 x 4320 RGB24 = ~99.5MB > 4MB slab size
            using var pool = new VideoFramePool(FramePoolBackend.OffHeapSlab, slabSize: 4 * 1024 * 1024);

            using (var frame = pool.Rent(7680, 4320, VideoPixelFormat.Rgb24))
            {
                Assert.IsType<NativeVideoFrameBuffer>(frame);
                var span = frame.AsSpan();
                Assert.Equal(7680 * 4320 * 3, span.Length);

                span[0] = 0x11;
                span[span.Length - 1] = 0x99;

                Assert.Equal(0x11, span[0]);
                Assert.Equal(0x99, span[span.Length - 1]);

                // Active blocks in slab allocator must be 0 because oversized frame falls back to unpooled NativeMemoryBlock
                Assert.Equal(0, pool.ActiveBlocks);
            }

            Assert.Equal(0, pool.ActiveBlocks);
        }

        [Fact]
        public void Test_Tier5_20_VideoFramePool_PostDisposalRentAndAccess_Throws()
        {
            var pool = new VideoFramePool(FramePoolBackend.ManagedArrayPool);
            var frame = pool.Rent(64, 48, VideoPixelFormat.Rgb24);

            frame.Dispose();
            Assert.True(frame.IsDisposed);
            Assert.Throws<ObjectDisposedException>(() => frame.AsSpan());
            Assert.Throws<ObjectDisposedException>(() => frame.AsReadOnlySpan());
            Assert.Throws<ObjectDisposedException>(() => frame.GetRowSpan(0));

            pool.Dispose();
            Assert.True(pool.IsDisposed);
            Assert.Throws<ObjectDisposedException>(() => pool.Rent(64, 48, VideoPixelFormat.Rgb24));
        }

        #endregion

        #region SECTION 4: ZeroText (VietnameseSearchNormalizer, VnCurrencyWords, VnMasterDataValidators)

        [Fact]
        public void Test_Tier5_21_VietnameseSearchNormalizer_MassiveText_ConcurrentFuzzing()
        {
            var seed = "Cộng hòa Xã hội Chủ nghĩa Việt Nam - Độc lập, Tự do, Hạnh phúc! Số phiếu #9988/VP-2026. ";
            var sb = new StringBuilder(40000);
            while (sb.Length < 35000)
            {
                sb.Append(seed);
            }
            string massive = sb.ToString();

            Parallel.For(0, 30, _ =>
            {
                string kw = VietnameseSearchNormalizer.ToSearchKeyword(massive);
                Assert.NotNull(kw);
                Assert.StartsWith("cong hoa xa hoi chu nghia viet nam", kw);
                Assert.DoesNotContain("Đ", kw);
                Assert.DoesNotContain("ệ", kw);

                string slug = VietnameseSearchNormalizer.ToSlug(massive);
                Assert.NotNull(slug);
                Assert.StartsWith("cong-hoa-xa-hoi-chu-nghia-viet-nam", slug);
                Assert.DoesNotContain("--", slug);

                string diacriticsRemoved = VietnameseSearchNormalizer.RemoveDiacritics(massive);
                Assert.Equal(massive.Length, diacriticsRemoved.Length);
                Assert.StartsWith("Cong hoa Xa hoi Chu nghia Viet Nam", diacriticsRemoved);
            });
        }

        [Fact]
        public void Test_Tier5_22_VietnameseSearchNormalizer_StackallocBoundary_256vs257Chars()
        {
            // Exactly 256 characters (stackalloc path)
            string text256 = new string('á', 256);
            string norm256 = VietnameseSearchNormalizer.ToSearchKeyword(text256);
            Assert.Equal(new string('a', 256), norm256);

            // Exactly 257 characters (ArrayPool path)
            string text257 = new string('đ', 257);
            string norm257 = VietnameseSearchNormalizer.ToSearchKeyword(text257);
            Assert.Equal(new string('d', 257), norm257);

            // 512 characters
            string text512 = new string('ệ', 512);
            string norm512 = VietnameseSearchNormalizer.ToSearchKeyword(text512);
            Assert.Equal(new string('e', 512), norm512);
        }

        [Theory]
        [InlineData("", "")]
        [InlineData("   ", "")]
        [InlineData("---___---", "")]
        [InlineData("Đơn Hàng / Bán Lẻ #100", "don-hang-ban-le-100")]
        [InlineData("Sản phẩm: Áo Sơ Mi Nam - Size XL", "san-pham-ao-so-mi-nam-size-xl")]
        public void Test_Tier5_23_VietnameseSearchNormalizer_SlugFormattingInvariants(string input, string expected)
        {
            string slug = VietnameseSearchNormalizer.ToSlug(input);
            Assert.Equal(expected, slug);

            if (slug.Length > 0)
            {
                Assert.False(slug.StartsWith("-"));
                Assert.False(slug.EndsWith("-"));
                Assert.DoesNotContain("--", slug);
            }
        }

        [Fact]
        public void Test_Tier5_24_VnCurrencyWords_ExtremeBoundaries_LongAndDecimalMinMax()
        {
            // 0 & negatives
            Assert.Equal("Không", 0L.ToVnWords());
            Assert.Equal("Âm một", (-1L).ToVnWords());
            Assert.Equal("Không đồng chẵn", 0m.ToVnCurrencyWords());

            // long.MaxValue & long.MinValue
            string maxLongWords = long.MaxValue.ToVnWords();
            Assert.StartsWith("Chín triệu tỷ", maxLongWords);
            Assert.EndsWith("bảy", maxLongWords);

            string minLongWords = long.MinValue.ToVnWords();
            Assert.StartsWith("Âm chín triệu tỷ", minLongWords);
            Assert.EndsWith("tám", minLongWords);

            // decimal.MaxValue & decimal.MinValue
            string maxDecWords = decimal.MaxValue.ToVnCurrencyWords();
            Assert.NotNull(maxDecWords);
            Assert.EndsWith("đồng chẵn", maxDecWords);

            string minDecWords = decimal.MinValue.ToVnCurrencyWords();
            Assert.NotNull(minDecWords);
            Assert.StartsWith("Âm", minDecWords);
            Assert.EndsWith("đồng chẵn", minDecWords);
        }

        [Fact]
        public void Test_Tier5_25_VnCurrencyWords_SubunitsAndDialectMatrix()
        {
            // Subunits
            Assert.Equal("Một xu", 0.01m.ToVnCurrencyWords());
            Assert.Equal("Năm xu", 0.05m.ToVnCurrencyWords());
            Assert.Equal("Năm mươi xu", 0.50m.ToVnCurrencyWords());
            Assert.Equal("Chín mươi chín xu", 0.99m.ToVnCurrencyWords());
            Assert.Equal("Một nghìn đồng và bảy mươi lăm xu", 1000.75m.ToVnCurrencyWords());

            // Dialects: tens == 0 strictly uses "linh bốn" / "lẻ bốn" per Circular 200 / VN grammar
            long testNum04 = 104004L;
            Assert.Equal("Một trăm linh bốn nghìn không trăm linh bốn", testNum04.ToVnWords(VnWordsOptions.Default));
            Assert.Equal("Một trăm lẻ bốn ngàn không trăm lẻ bốn", testNum04.ToVnWords(VnWordsOptions.SouthernDialect));

            // Dialects: tens > 1 with unit 4 uses "mươi tư" by default or "mươi bốn" if configured
            long testNum24 = 105024L;
            Assert.Equal("Một trăm linh năm nghìn không trăm hai mươi tư", testNum24.ToVnWords(VnWordsOptions.Default));
            var optBon = new VnWordsOptions { UseTuForFour = false, UseSouthernThousands = true, UseSouthernZeroTens = true };
            Assert.Equal("Một trăm lẻ năm ngàn không trăm hai mươi bốn", testNum24.ToVnWords(optBon));
        }

        [Fact]
        public void Test_Tier5_26_VnMasterDataValidators_Mst_Modulo11Checksum_EnterpriseAndBranch()
        {
            string viettel = "0100109106";
            Assert.True(VnMasterDataValidators.IsValidMst(viettel));
            Assert.True(VnMasterDataValidators.IsValidTaxCode(viettel));
            Assert.Equal(viettel, VnMasterDataValidators.NormalizeMst(viettel));

            // Valid branch
            Assert.True(VnMasterDataValidators.IsValidMst(viettel + "-001"));
            Assert.True(VnMasterDataValidators.IsValidMst(viettel + "999"));
            Assert.Equal(viettel + "-001", VnMasterDataValidators.NormalizeMst(viettel + "-001"));

            // Prohibited branch 000
            Assert.False(VnMasterDataValidators.IsValidMst(viettel + "-000"));
            Assert.False(VnMasterDataValidators.IsValidMst(viettel + "000"));
            Assert.Null(VnMasterDataValidators.NormalizeMst(viettel + "-000"));

            // Check digit corruption
            for (int d = 0; d <= 9; d++)
            {
                if (d == 6) continue;
                string bad = viettel.Substring(0, 9) + d;
                Assert.False(VnMasterDataValidators.IsValidMst(bad));
            }

            // Malformed lengths
            Assert.False(VnMasterDataValidators.IsValidMst("010010910"));
            Assert.False(VnMasterDataValidators.IsValidMst("01001091061"));
            Assert.False(VnMasterDataValidators.IsValidMst("010010910601"));
            Assert.False(VnMasterDataValidators.IsValidMst("01001091060001"));
        }

        [Fact]
        public void Test_Tier5_27_VnMasterDataValidators_Cccd_AllProvincesAndCenturies()
        {
            // Valid CCCD from Ha Noi (001), Male born 1985 (0), year 85, seq 006789
            Assert.True(VnMasterDataValidators.IsValidCccd("001085006789", out int y1, out bool m1, out string? p1));
            Assert.Equal("Hà Nội", p1);
            Assert.Equal(1985, y1);
            Assert.True(m1);

            // Valid CCCD from TP.HCM (079), Female born 2002 (3), year 02, seq 123456
            Assert.True(VnMasterDataValidators.IsValidCccd("079302123456", out int y2, out bool m2, out string? p2));
            Assert.Equal("TP. Hồ Chí Minh", p2);
            Assert.Equal(2002, y2);
            Assert.False(m2);

            // Invalid province code
            Assert.False(VnMasterDataValidators.IsValidCccd("000085006789"));
            Assert.False(VnMasterDataValidators.IsValidCccd("099085006789"));

            // Invalid lengths
            Assert.False(VnMasterDataValidators.IsValidCccd("00108500678"));
            Assert.False(VnMasterDataValidators.IsValidCccd("0010850067890"));
        }

        [Fact]
        public void Test_Tier5_28_VnMasterDataValidators_Phone_LocalAndInternationalFormats()
        {
            string[] validPhones = { "0901234567", "0381234567", "0561234567", "0771234567", "0891234567" };
            foreach (var phone in validPhones)
            {
                Assert.True(VnMasterDataValidators.IsValidPhone(phone));
                Assert.Equal(phone, VnMasterDataValidators.NormalizePhone(phone, international: false));
                Assert.Equal("+84" + phone.Substring(1), VnMasterDataValidators.NormalizePhone(phone, international: true));
            }

            // Input with separators
            Assert.Equal("0901234567", VnMasterDataValidators.NormalizePhone("(090) 123-4567"));
            Assert.Equal("+84901234567", VnMasterDataValidators.NormalizePhone("+84 90 123 4567", international: true));

            // Invalid prefixes
            Assert.False(VnMasterDataValidators.IsValidPhone("0101234567"));
            Assert.False(VnMasterDataValidators.IsValidPhone("0241234567"));
            Assert.False(VnMasterDataValidators.IsValidPhone("0401234567"));
            Assert.Null(VnMasterDataValidators.NormalizePhone("0101234567"));
        }

        #endregion

        private sealed class ReferenceEqualityComparer : IEqualityComparer<byte[]>
        {
            public static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();
            public bool Equals(byte[]? x, byte[]? y) => ReferenceEquals(x, y);
            public int GetHashCode(byte[] obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }
}
