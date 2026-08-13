using System.Security.Cryptography;
using System.Text;

namespace HSTRYDoc.Tests
{
    public sealed class HSTRYContainerTests
    {
        [Fact]
        public void SaveLoadRoundTrip_PreservesBlockMutations()
        {
            using var temp = new TemporaryDirectory();
            string path = Path.Combine(temp.Path, "roundtrip.hstry");

            using var ownerEcdh = HSTRYContainer.EcdhKeyFiles.CreateNewKeyPair();
            using var ownerSig = HSTRYContainer.EcdsaKeyFiles.CreateNewKeyPair();

            HSTRYContainer container = HSTRYContainer.CreateNewForRecipients(
                ownerSig,
                ownerEcdh,
                new[] { ownerEcdh },
                Encoding.UTF8);

            container.AddRtfDocument(ownerSig, "Alpha", CreateRtf("Alpha", "first body"));
            container.AddRtfDocument(ownerSig, "Beta", CreateRtf("Beta", "second body"));
            container.RenameBlock(ownerSig, 1, "Gamma");
            container.RemoveBlock(ownerSig, 0);

            container.Save(path);

            HSTRYContainer loaded = HSTRYContainer.LoadWithPrivateKeyFile(path, ownerEcdh);

            Assert.Single(loaded.Blocks);
            Assert.Equal("Gamma", loaded.Blocks[0].Title);
            Assert.Contains("second body", loaded.GetRtfDocument(0), StringComparison.Ordinal);
        }

        [Fact]
        public void RemoveRecipientByKeyIdHex_RejectsOwnerEntry()
        {
            using var ownerEcdh = HSTRYContainer.EcdhKeyFiles.CreateNewKeyPair();
            using var ownerSig = HSTRYContainer.EcdsaKeyFiles.CreateNewKeyPair();

            HSTRYContainer container = HSTRYContainer.CreateNewForRecipients(
                ownerSig,
                ownerEcdh,
                new[] { ownerEcdh },
                Encoding.UTF8);

            string ownerKeyIdHex = Convert.ToHexString(SHA256.HashData(ownerEcdh.ExportSubjectPublicKeyInfo()));

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
                () => container.RemoveRecipientByKeyIdHex(ownerSig, ownerKeyIdHex));

            Assert.Contains("cannot be removed", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void AddRtfDocument_RejectsNonOwnerSession()
        {
            using var temp = new TemporaryDirectory();
            string path = Path.Combine(temp.Path, "recipient-open.hstry");

            using var ownerEcdh = HSTRYContainer.EcdhKeyFiles.CreateNewKeyPair();
            using var ownerSig = HSTRYContainer.EcdsaKeyFiles.CreateNewKeyPair();
            using var recipientEcdh = HSTRYContainer.EcdhKeyFiles.CreateNewKeyPair();
            using var recipientPub = CreatePublicOnlyKey(recipientEcdh);

            HSTRYContainer container = HSTRYContainer.CreateNewForRecipients(
                ownerSig,
                ownerEcdh,
                new[] { ownerEcdh, recipientPub },
                Encoding.UTF8);

            container.Save(path);

            HSTRYContainer loaded = HSTRYContainer.LoadWithPrivateKeyFile(path, recipientEcdh);

            Assert.False(loaded.IsOpenedAsOwner);
            Assert.Throws<UnauthorizedAccessException>(
                () => loaded.AddRtfDocument(ownerSig, "Recipient", CreateRtf("Recipient", "body")));
        }

        [Fact]
        public void Save_KeepsRecoveryCopyOfPreviousVersion()
        {
            using var temp = new TemporaryDirectory();
            string path = Path.Combine(temp.Path, "recovery.hstry");
            string recoveryPath = HSTRYContainer.GetRecoveryFilePath(path);

            using var ownerEcdh = HSTRYContainer.EcdhKeyFiles.CreateNewKeyPair();
            using var ownerSig = HSTRYContainer.EcdsaKeyFiles.CreateNewKeyPair();

            HSTRYContainer container = HSTRYContainer.CreateNewForRecipients(
                ownerSig,
                ownerEcdh,
                new[] { ownerEcdh },
                Encoding.UTF8);

            container.AddRtfDocument(ownerSig, "Doc", CreateRtf("Doc", "version one"));
            container.Save(path);

            container.UpdateRtfDocument(ownerSig, 0, CreateRtf("Doc", "version two"));
            container.Save(path);

            Assert.True(File.Exists(recoveryPath));

            HSTRYContainer current = HSTRYContainer.LoadWithPrivateKeyFile(path, ownerEcdh);
            HSTRYContainer recovery = HSTRYContainer.LoadWithPrivateKeyFile(recoveryPath, ownerEcdh);

            Assert.Contains("version two", current.GetRtfDocument(0), StringComparison.Ordinal);
            Assert.Contains("version one", recovery.GetRtfDocument(0), StringComparison.Ordinal);
        }

        [Fact]
        public void TransferOwnership_AllowsNewOwnerToContinueAsOwner()
        {
            using var temp = new TemporaryDirectory();
            string path = Path.Combine(temp.Path, "transfer.hstry");

            using var oldOwnerEcdh = HSTRYContainer.EcdhKeyFiles.CreateNewKeyPair();
            using var oldOwnerSig = HSTRYContainer.EcdsaKeyFiles.CreateNewKeyPair();
            using var newOwnerEcdh = HSTRYContainer.EcdhKeyFiles.CreateNewKeyPair();
            using var newOwnerSig = HSTRYContainer.EcdsaKeyFiles.CreateNewKeyPair();

            HSTRYContainer container = HSTRYContainer.CreateNewForRecipients(
                oldOwnerSig,
                oldOwnerEcdh,
                new[] { oldOwnerEcdh },
                Encoding.UTF8);

            container.AddRtfDocument(oldOwnerSig, "Before", CreateRtf("Before", "initial"));
            container.TransferOwnership(oldOwnerSig, oldOwnerEcdh, newOwnerSig, newOwnerEcdh);
            container.Save(path);

            HSTRYContainer loaded = HSTRYContainer.LoadWithPrivateKeyFile(path, newOwnerEcdh);

            Assert.True(loaded.IsOpenedAsOwner);
            loaded.AddRtfDocument(newOwnerSig, "After", CreateRtf("After", "new owner"));
            Assert.Equal(2, loaded.Blocks.Count);
        }

        [Fact]
        public void Load_RejectsTamperedBlockCount()
        {
            using var ownerEcdh = HSTRYContainer.EcdhKeyFiles.CreateNewKeyPair();
            using var ownerSig = HSTRYContainer.EcdsaKeyFiles.CreateNewKeyPair();

            HSTRYContainer container = HSTRYContainer.CreateNewForRecipients(
                ownerSig,
                ownerEcdh,
                new[] { ownerEcdh },
                Encoding.UTF8);

            container.AddRtfDocument(ownerSig, "Alpha", CreateRtf("Alpha", "first body"));
            container.AddRtfDocument(ownerSig, "Beta", CreateRtf("Beta", "second body"));

            using MemoryStream ms = new();
            container.Save(ms);

            byte[] bytes = ms.ToArray();
            int blockCountOffset = FindBlockCountOffset(bytes);
            BitConverter.GetBytes(1).CopyTo(bytes, blockCountOffset);

            using MemoryStream tampered = new(bytes);
            InvalidDataException ex = Assert.Throws<InvalidDataException>(
                () => HSTRYContainer.LoadWithPrivateKey(tampered, ownerEcdh));

            Assert.Contains("manifest", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void GrantReadAllBlocks_CancelDuringMutation_RollsBackContainer()
        {
            using var ownerEcdh = HSTRYContainer.EcdhKeyFiles.CreateNewKeyPair();
            using var ownerSig = HSTRYContainer.EcdsaKeyFiles.CreateNewKeyPair();
            using var recipientEcdh = HSTRYContainer.EcdhKeyFiles.CreateNewKeyPair();
            using var recipientPub = CreatePublicOnlyKey(recipientEcdh);

            HSTRYContainer container = HSTRYContainer.CreateNewForRecipients(
                ownerSig,
                ownerEcdh,
                new[] { ownerEcdh },
                Encoding.UTF8);

            container.AddRtfDocument(ownerSig, "Alpha", CreateRtf("Alpha", "first body"));
            container.AddRtfDocument(ownerSig, "Beta", CreateRtf("Beta", "second body"));
            container.AddRecipient(ownerSig, recipientPub);

            byte[] recipientKeyId = SHA256.HashData(recipientEcdh.ExportSubjectPublicKeyInfo());
            using CancellationTokenSource cts = new();
            var progress = new CancelOnFirstAccessMutationProgress(cts);

            Assert.Throws<OperationCanceledException>(() =>
                container.GrantReadAllBlocks(ownerSig, ownerEcdh, recipientPub, progress, cts.Token));

            Assert.True(container.Validate(out string error), error);
            Assert.DoesNotContain(container.Blocks, b => b.KeySlots.Any(s => s.KeyId.SequenceEqual(recipientKeyId)));
        }

        private sealed class CancelOnFirstAccessMutationProgress : IProgress<UiProgress>
        {
            private readonly CancellationTokenSource _cts;
            private bool _cancelled;

            public CancelOnFirstAccessMutationProgress(CancellationTokenSource cts)
            {
                _cts = cts;
            }

            public void Report(UiProgress value)
            {
                if (_cancelled)
                    return;

                if ((value.Message ?? string.Empty).StartsWith("Updating access lists", StringComparison.Ordinal) &&
                    value.Value.GetValueOrDefault() >= 1)
                {
                    _cancelled = true;
                    _cts.Cancel();
                }
            }
        }

        private static ECDiffieHellman CreatePublicOnlyKey(ECDiffieHellman privateKey)
        {
            byte[] spki = privateKey.ExportSubjectPublicKeyInfo();
            var publicOnly = ECDiffieHellman.Create();
            publicOnly.ImportSubjectPublicKeyInfo(spki, out _);
            return publicOnly;
        }

        private static string CreateRtf(string title, string body)
        {
            string plain = $"{title}\\par {body}";
            return "{\\rtf1\\ansi " + EscapeRtf(plain) + "}";
        }

        private static string EscapeRtf(string text)
            => text.Replace("\\", "\\\\", StringComparison.Ordinal)
                   .Replace("{", "\\{", StringComparison.Ordinal)
                   .Replace("}", "\\}", StringComparison.Ordinal);

        private static int FindBlockCountOffset(byte[] bytes)
        {
            using MemoryStream ms = new(bytes, writable: false);
            using BinaryReader br = new(ms, Encoding.UTF8, leaveOpen: true);

            br.ReadBytes(Global.FileMagic.Length);
            br.ReadByte();
            int encLen = br.ReadByte();
            br.ReadBytes(encLen);
            br.ReadBytes(br.ReadUInt16());
            br.ReadBytes(br.ReadUInt16());
            br.ReadBytes(32);
            br.ReadBytes(32);

            int recipientCount = br.ReadInt32();
            for (int i = 0; i < recipientCount; i++)
            {
                br.ReadBytes(br.ReadByte());
                br.ReadBytes(br.ReadUInt16());
                br.ReadByte();
                br.ReadBytes(br.ReadUInt16());
                br.ReadBytes(br.ReadUInt16());
            }

            br.ReadByte();
            br.ReadBytes(br.ReadUInt16());
            return checked((int)ms.Position);
        }

        private sealed class TemporaryDirectory : IDisposable
        {
            public TemporaryDirectory()
            {
                Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "HSTRYDoc.Tests", Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(Path);
            }

            public string Path { get; }

            public void Dispose()
            {
                try
                {
                    if (Directory.Exists(Path))
                        Directory.Delete(Path, recursive: true);
                }
                catch
                {
                    // best effort cleanup
                }
            }
        }
    }
}
