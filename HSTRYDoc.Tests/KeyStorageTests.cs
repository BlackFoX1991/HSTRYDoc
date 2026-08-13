namespace HSTRYDoc.Tests
{
    public sealed class KeyStorageTests
    {
        [Fact]
        public void GetKeyFolderForDrive_UsesHstryKeyFolderAtDriveRoot()
        {
            string folder = KeyStorage.GetKeyFolderForDrive(@"E:\");

            Assert.Equal(@"E:\HSTRY_KEY", folder);
        }

        [Fact]
        public void TryGetDriveRootAndFileNameFromKeyPath_AcceptsOnlyManagedKeyFolder()
        {
            Assert.True(KeyStorage.TryGetDriveRootAndFileNameFromKeyPath(
                @"E:\HSTRY_KEY\owner.hstrypriv",
                out string driveRoot,
                out string fileName));

            Assert.Equal(@"E:\", driveRoot);
            Assert.Equal("owner.hstrypriv", fileName);

            Assert.False(KeyStorage.TryGetDriveRootAndFileNameFromKeyPath(
                @"E:\Other\owner.hstrypriv",
                out _,
                out _));
        }

        [Fact]
        public void AppState_MigratesLegacyManagedPrivateKeyPath()
        {
            var state = new AppState
            {
                PrivateKeyPath = @"E:\HSTRY_KEY\team.hstrypriv"
            };

            state.MigrateLegacyPrivateKeyPath();

            Assert.Equal(@"E:\", state.PrivateKeyDriveRoot);
            Assert.Equal("team.hstrypriv", state.PrivateKeyFileName);
            Assert.Null(state.PrivateKeyPath);
        }

        [Fact]
        public void GetPrivateKeyFileNames_ReturnsSortedFileNames()
        {
            string[] names = KeyStorage.GetPrivateKeyFileNames(new[]
            {
                @"E:\HSTRY_KEY\zeta.hstrypriv",
                @"E:\HSTRY_KEY\alpha.hstrypriv",
                @"E:\HSTRY_KEY\alpha.hstrypriv"
            });

            Assert.Equal(new[] { "alpha.hstrypriv", "zeta.hstrypriv" }, names);
        }
    }
}
