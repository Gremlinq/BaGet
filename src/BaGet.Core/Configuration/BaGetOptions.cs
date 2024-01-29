﻿namespace BaGet.Core
{
    public class BaGetOptions
    {
        /// <summary>
        /// The API Key required to authenticate package
        /// operations. If empty, package operations do not require authentication.
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// If true, disables package pushing, deleting, and re-listing.
        /// </summary>
        public bool IsReadOnlyMode { get; set; } = false;
    }
}
