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

            container.AddRtfDocument("Alpha", CreateRtf("Alpha", "first body"));
            container.AddRtfDocument("Beta", CreateRtf("Beta", "second body"));
            container.RenameBlock(1, "Gamma");
            container.RemoveBlock(0);

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
                () => loaded.AddRtfDocument("Recipient", CreateRtf("Recipient", "body")));
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

            container.AddRtfDocument("Doc", CreateRtf("Doc", "version one"));
            container.Save(path);

            container.UpdateRtfDocument(0, CreateRtf("Doc", "version two"));
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

            container.AddRtfDocument("Before", CreateRtf("Before", "initial"));
            container.TransferOwnership(oldOwnerSig, oldOwnerEcdh, newOwnerSig, newOwnerEcdh);
            container.Save(path);

            HSTRYContainer loaded = HSTRYContainer.LoadWithPrivateKeyFile(path, newOwnerEcdh);

            Assert.True(loaded.IsOpenedAsOwner);
            loaded.AddRtfDocument("After", CreateRtf("After", "new owner"));
            Assert.Equal(2, loaded.Blocks.Count);
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
