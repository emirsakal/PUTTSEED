using NUnit.Framework;
using PuttSeed.Core.FixedMath;
using PuttSeed.Core.Replay;
using PuttSeed.Core.Sim;

namespace PuttSeed.Core.Tests.Replay
{
    [TestFixture]
    public class ReplayCodecTests
    {
        [Test]
        public void Encode_ProducesPuttPrefixedBase64Url()
        {
            var code = ReplayCodec.Encode(42UL, new[] { new ShotInput(0, 255) });
            Assert.That(code, Does.StartWith("PUTT-"));
            Assert.That(code, Does.Not.Contain("+"));
            Assert.That(code, Does.Not.Contain("/"));
            Assert.That(code, Does.Not.Contain("="));
        }

        [Test]
        public void RoundTrip_PreservesSeedAndShots()
        {
            var shots = new[]
            {
                new ShotInput(0, 255),
                new ShotInput(512, 128),
                new ShotInput(1023, 0),
                new ShotInput(300, 77),
            };
            var code = ReplayCodec.Encode(0xDEADBEEFCAFEBABEUL, shots);

            Assert.That(ReplayCodec.TryDecode(code, out var seed, out var decoded), Is.True);
            Assert.That(seed, Is.EqualTo(0xDEADBEEFCAFEBABEUL));
            Assert.That(decoded.Length, Is.EqualTo(shots.Length));
            for (int i = 0; i < shots.Length; i++)
            {
                Assert.That(decoded[i].AngleIndex, Is.EqualTo(shots[i].AngleIndex), $"angle {i}");
                Assert.That(decoded[i].PowerIndex, Is.EqualTo(shots[i].PowerIndex), $"power {i}");
            }
        }

        [Test]
        public void RoundTrip_EmptyShotList()
        {
            var code = ReplayCodec.Encode(7UL, System.Array.Empty<ShotInput>());
            Assert.That(ReplayCodec.TryDecode(code, out var seed, out var shots), Is.True);
            Assert.That(seed, Is.EqualTo(7UL));
            Assert.That(shots, Is.Empty);
        }

        [Test]
        public void RoundTrip_RandomShotLists_1000Cases()
        {
            var rng = new FixRng(2024UL);
            for (int caseNo = 0; caseNo < 1000; caseNo++)
            {
                ulong seed = ((ulong)rng.NextUInt() << 32) | rng.NextUInt();
                var shots = new ShotInput[rng.NextInt(0, 12)];
                for (int i = 0; i < shots.Length; i++)
                {
                    shots[i] = new ShotInput(rng.NextInt(0, 1024), rng.NextInt(0, 256));
                }

                var code = ReplayCodec.Encode(seed, shots);
                Assert.That(ReplayCodec.TryDecode(code, out var seed2, out var shots2), Is.True, $"case {caseNo}");
                Assert.That(seed2, Is.EqualTo(seed), $"case {caseNo}");
                Assert.That(shots2.Length, Is.EqualTo(shots.Length), $"case {caseNo}");
                for (int i = 0; i < shots.Length; i++)
                {
                    Assert.That(shots2[i].AngleIndex, Is.EqualTo(shots[i].AngleIndex));
                    Assert.That(shots2[i].PowerIndex, Is.EqualTo(shots[i].PowerIndex));
                }
            }
        }

        [Test]
        public void Encode_MaxShotCount_RoundTrips()
        {
            var shots = new ShotInput[255];
            for (int i = 0; i < shots.Length; i++)
            {
                shots[i] = new ShotInput(i * 4 % 1024, i % 256);
            }

            var code = ReplayCodec.Encode(1UL, shots);
            Assert.That(ReplayCodec.TryDecode(code, out _, out var decoded), Is.True);
            Assert.That(decoded.Length, Is.EqualTo(255));
        }

        [Test]
        public void Encode_TooManyShots_Throws()
        {
            var shots = new ShotInput[256];
            Assert.Throws<System.ArgumentException>(() => ReplayCodec.Encode(1UL, shots));
        }

        [Test]
        public void TryDecode_RejectsGarbage()
        {
            Assert.That(ReplayCodec.TryDecode("", out _, out _), Is.False);
            Assert.That(ReplayCodec.TryDecode("HELLO", out _, out _), Is.False);
            Assert.That(ReplayCodec.TryDecode("PUTT-", out _, out _), Is.False);
            Assert.That(ReplayCodec.TryDecode("PUTT-!!!!", out _, out _), Is.False);
            Assert.That(ReplayCodec.TryDecode("NOPE-AAAA", out _, out _), Is.False);
        }

        [Test]
        public void TryDecode_RejectsTruncatedPayload()
        {
            var code = ReplayCodec.Encode(42UL, new[] { new ShotInput(100, 50), new ShotInput(200, 60) });
            // Chop characters off the end: every strict prefix must be rejected.
            for (int keep = 5; keep < code.Length - 1; keep++)
            {
                Assert.That(ReplayCodec.TryDecode(code.Substring(0, keep), out _, out _), Is.False,
                    $"decoded a truncated code of length {keep}");
            }
        }

        [Test]
        public void TryDecode_RejectsWrongVersion()
        {
            // Handcrafted payload with version byte 99: [99][seed:8][count:0]
            var payload = new byte[] { 99, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            var code = "PUTT-" + System.Convert.ToBase64String(payload)
                .Replace('+', '-').Replace('/', '_').TrimEnd('=');
            Assert.That(ReplayCodec.TryDecode(code, out _, out _), Is.False);
        }

        [Test]
        public void Encode_GoldenFixture_IsStable()
        {
            // Frozen encoding of a fixed replay; changes to the wire format must
            // be intentional and bump the codec version.
            var code = ReplayCodec.Encode(42UL, new[]
            {
                new ShotInput(0, 255),
                new ShotInput(512, 128),
                new ShotInput(1023, 0),
            });
            Assert.That(code, Is.EqualTo("PUTT-ASoAAAAAAAAAAwD8AwACAv8DAA"),
                $"actual: {code}");
        }

        [Test]
        public void V2RoundTrip_CarriesConfigVersion()
        {
            var shots = new[] { new ShotInput(300, 77) };
            var code = ReplayCodec.Encode(42UL, shots, configVersion: 2);

            Assert.That(ReplayCodec.TryDecode(code, out var seed, out var decoded, out int version), Is.True);
            Assert.That(version, Is.EqualTo(2));
            Assert.That(seed, Is.EqualTo(42UL));
            Assert.That(decoded.Length, Is.EqualTo(1));
        }

        [Test]
        public void V1Codes_DecodeAsConfigVersion1()
        {
            var code = ReplayCodec.Encode(42UL, System.Array.Empty<ShotInput>());
            Assert.That(ReplayCodec.TryDecode(code, out _, out _, out int version), Is.True);
            Assert.That(version, Is.EqualTo(1), "legacy codes are generator v1 by definition");
        }

        [Test]
        public void TwoArgTryDecode_AcceptsV2Codes()
        {
            // The version-blind overload keeps old call sites compiling and
            // must not reject the new wire version.
            var code = ReplayCodec.Encode(9UL, System.Array.Empty<ShotInput>(), configVersion: 2);
            Assert.That(ReplayCodec.TryDecode(code, out var seed, out _), Is.True);
            Assert.That(seed, Is.EqualTo(9UL));
        }

        [Test]
        public void Encode_RejectsUnknownConfigVersion()
        {
            Assert.Throws<System.ArgumentException>(
                () => ReplayCodec.Encode(1UL, System.Array.Empty<ShotInput>(), configVersion: 3));
        }
    }
}
