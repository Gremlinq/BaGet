using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BaGet.Azure
{
    /// <summary>
    /// BaGet's configurations to use Azure Blob Storage to store packages.
    /// See: https://loic-sharma.github.io/BaGet/quickstart/azure/#azure-blob-storage
    /// </summary>
    public class AzureBlobStorageOptions : IValidatableObject
    {
        /// <summary>
        /// The Azure Blob Storage connection string.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// The Azure Blob Storage container name.
        /// </summary>
        public string Container { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            const string helpUrl = "https://loic-sharma.github.io/BaGet/quickstart/azure/#azure-blob-storage";

            if (string.IsNullOrEmpty(ConnectionString))
            {
                yield return new ValidationResult(
                    $"The {nameof(ConnectionString)} configuration is required. See {helpUrl}",
                    new[] { nameof(ConnectionString) });

            }

            if (string.IsNullOrEmpty(Container))
            {
                yield return new ValidationResult(
                    $"The {nameof(Container)} configuration is required. See {helpUrl}",
                    new[] { nameof(Container) });
            }
        }
    }
}
