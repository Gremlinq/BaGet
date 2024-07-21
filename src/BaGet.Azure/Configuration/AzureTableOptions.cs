using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BaGet.Azure
{
    public class AzureTableOptions : IValidatableObject
    {
        public string TableName { get; set; }

        public string StorageAccountName { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            const string helpUrl = "https://loic-sharma.github.io/BaGet/quickstart/azure/#azure-blob-storage";

            if (string.IsNullOrEmpty(StorageAccountName))
            {
                yield return new ValidationResult(
                    $"The {nameof(StorageAccountName)} configuration is required. See {helpUrl}",
                    new[] { nameof(StorageAccountName) });
            }

            if (string.IsNullOrEmpty(TableName))
            {
                yield return new ValidationResult(
                    $"The {nameof(TableName)} configuration is required. See {helpUrl}",
                    new[] { nameof(TableName) });
            }
        }
    }
}
