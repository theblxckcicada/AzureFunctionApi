namespace EasySMS.API.Functions.Helpers
{
    public static class Error
    {
        public static string MessageNotSupported()
        {
            return "Message not supported";
        }

        public static string NullProductRowKeyOrPartitionKey()
        {
            return "System Product RowKey or Partition Key should not be null";
        }

        public static string NullSystemRowKeyOrPartitionKey()
        {
            return "System RowKey or Partition Key should not be null";
        }

        public static string NullSystemProductOrSystemAsset()
        {
            return "System Asset or System Product not found";
        }

        public static string NullSystemOrSystemAsset()
        {
            return "System Asset or System  not found";
        }

        public static string NullSystemRowKey()
        {
            return "System Row Key can not be null";
        }

        public static string HeaderKeyNotSupplied(string? name = null)
        {
            return $"Header Key TableName or BlobContainerName '{name}' is not supported";
        }

        public static string FilterNotSupported(string message)
        {
            return $"Filter not supported [{message}]";
        }

        public static string DocumentFilterNotSupported(string message)
        {
            return $"Document Filter not supported [{message}]";
        }

        public static string NullSystem()
        {
            return "System Not Found";
        }

        public static string InvalidSystemAssetUpdate()
        {
            return "Could Not Update The SystemAsset Data!";
        }

        public static string AuthorizationTokenNotSupplied()
        {
            return "Authorization Token is not supplied";
        }

        public static string AuthorizationKeysNotSupplied()
        {
            return "Api Key and Id are not supplied ";
        }

        public static string InvalidAuthorizationToken()
        {
            return $"Invalid Authorization Token";
        }

        public static string FailedToProcessRequest(string req, string tableName)
        {
            return $"Failed to {req} {tableName} message";
        }

        public static string RequestNotSupported(string req)
        {
            return $"Request Method {req} is not supported";
        }

        public static string InvalidRequest()
        {
            return $"Invalid request, use the web application.";
        }
    }
}
