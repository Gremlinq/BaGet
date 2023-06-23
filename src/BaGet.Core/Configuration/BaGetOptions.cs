namespace BaGet.Core
{
    public class BaGetOptions
    {
        /// <summary>
        /// The API Key required to authenticate package
        /// operations. If empty, package operations do not require authentication.
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// How BaGet should interpret package deletion requests.
        /// </summary>
        public PackageDeletionBehavior PackageDeletionBehavior { get; set; } = PackageDeletionBehavior.Unlist;

        /// <summary>
        /// If enabled, pushing a package that already exists will replace the
        /// existing package.
        /// </summary>
        public bool AllowPackageOverwrites { get; set; } = false;

        /// <summary>
        /// If true, disables package pushing, deleting, and re-listing.
        /// </summary>
        public bool IsReadOnlyMode { get; set; } = false;

        public StorageOptions Storage { get; set; }

        public SearchOptions Search { get; set; }

        public MirrorOptions Mirror { get; set; }
    }
}
