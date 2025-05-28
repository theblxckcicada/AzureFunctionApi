namespace DMIX.API.Common.Models
{
    public record DocumentFilter
    {
        public required string Name
        {
            get; init;
        }
        public DocumentType DocumentType
        {
            get; init;
        }
        public required string UserId
        {
            get; init;
        }

        public string ToBlobPath() => ToBlobPath(this);

        private static string ToBlobPath(DocumentFilter documentFilter)
        {
            var blobPath = $"{documentFilter.UserId}/";

            return documentFilter.DocumentType switch
            {
                DocumentType.Invoice => blobPath + $"{nameof(DocumentType.Invoice)}/",
                DocumentType.BankStatement => blobPath + $"{nameof(DocumentType.BankStatement)}/",
                DocumentType.Identity => blobPath + $"{nameof(DocumentType.Identity)}/",
                _ => string.Empty,
            };
        }
    }

    public enum DocumentType
    {
        Invoice,
        BankStatement,
        Identity
    }
}
