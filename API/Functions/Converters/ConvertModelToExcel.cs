using System.Reflection;
using EasySMS.API.Azure.Models;
using EasySMS.API.Common;
using EasySMS.API.Http;
using Microsoft.Azure.Functions.Worker.Http;
using OfficeOpenXml;

namespace EasySMS.API.Functions.Converters;

public static class ModelConvertor
{
    public static Stream GenerateExcel<T>(List<T> entities, bool excludeBaseEntities = false)
    {
        var stream = new MemoryStream();

        using (var package = new ExcelPackage(stream))
        {
            var allProperties = typeof(T).GetProperties();

            var listProperties = allProperties
                .Where(p =>
                    typeof(System.Collections.IEnumerable).IsAssignableFrom(p.PropertyType) &&
                    p.PropertyType != typeof(string))
                .ToList();

            // If no list properties exist, return empty stream (or you could throw or log)
            if (listProperties.Count == 0)
                return stream;

            // Non-list properties of parent to include in each list worksheet
            var parentProperties = allProperties
                .Where(p =>
                    !typeof(System.Collections.IEnumerable).IsAssignableFrom(p.PropertyType) ||
                    p.PropertyType == typeof(string))
                .Where(p =>
                    !p.Name.Contains("userId", StringComparison.OrdinalIgnoreCase) &&
                    !p.Name.Contains("Error", StringComparison.OrdinalIgnoreCase) &&
                    !p.Name.Equals(nameof(EntityBase.PartitionKey), StringComparison.OrdinalIgnoreCase))
                .DistinctBy(p => p.Name)
                .ToList();

            if (excludeBaseEntities)
            {
                parentProperties = parentProperties.Where(p =>
                    !p.Name.Equals(nameof(EntityBase.CreatedBy), StringComparison.OrdinalIgnoreCase))
                    .Where(p =>
                    !p.Name.Contains(nameof(EntityBase.CreatedDate), StringComparison.OrdinalIgnoreCase))
                    .Where(p =>
                    !p.Name.Contains(nameof(EntityBase.ModifiedBy), StringComparison.OrdinalIgnoreCase))
                      .Where(p =>
                    !p.Name.Equals(nameof(EntityBase.ModifiedDate), StringComparison.OrdinalIgnoreCase))
                               .Where(p =>
                    !p.Name.Equals(nameof(EntityBase.RowKey), StringComparison.OrdinalIgnoreCase))
                                .Where(p =>
                    !p.Name.Contains(nameof(EntityBase.RowKey), StringComparison.OrdinalIgnoreCase))
                    .ToList();

            }

            foreach (var listProp in listProperties)
            {
                var sheet = package.Workbook.Worksheets.Add(listProp.Name.SplitPascalCase());
                int rowIndex = 1;

                foreach (var entity in entities)
                {
                    if (listProp.GetValue(entity) is not System.Collections.IEnumerable list)
                        continue;

                    foreach (var item in list)
                    {
                        var itemType = item.GetType();
                        var itemProps = itemType.GetProperties();

                        // Header row (first time only)
                        if (rowIndex == 1)
                        {
                            int col = 1;
                            foreach (var pProp in parentProperties)
                            {
                                sheet.Cells[1, col++].Value = pProp.Name.SplitPascalCase();
                            }
                            foreach (var iProp in itemProps)
                            {
                                sheet.Cells[1, col++].Value = iProp.Name.SplitPascalCase();
                            }
                            rowIndex++;
                        }

                        // Data row
                        int dataCol = 1;
                        foreach (var pProp in parentProperties)
                        {
                            sheet.Cells[rowIndex, dataCol++].Value = pProp.GetValue(entity);
                        }
                        foreach (var iProp in itemProps)
                        {
                            sheet.Cells[rowIndex, dataCol++].Value = iProp.GetValue(item);
                        }
                        rowIndex++;
                    }
                }
            }

            package.Save();
        }

        stream.Position = 0;
        return stream;
    }



    public static async Task<HttpResponseData> DownloadExcelFileAsync<T>(
        HttpRequestData req,
        List<T> res
    )
    {
        var stream = GenerateExcel(res);
        var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        var responseData = await req.CreateOkResponseAsync(string.Empty);
        _ = responseData.Headers.Remove("Content-Type");
        responseData.Headers.Add("Content-Type", contentType);

        responseData.Body = stream;

        return responseData;
    }




    public static List<TDestination> ConvertModels<TSource, TDestination>(List<TSource> sources)
        where TDestination : new()
    {
        List<TDestination> destinations = [];

        foreach (var source in sources)
        {
            var converted = ConvertModelChangeDateTimeToString<TSource, TDestination>(source);
            destinations.Add(converted);
        }
        return destinations;
    }

    public static TDestination ConvertModel<TSource, TDestination>(TSource source)
        where TDestination : new()
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        // Create an instance of the destination type
        TDestination destination = new();

        // Get properties of both source and destination types
        var sourceProperties = typeof(TSource).GetProperties(
            BindingFlags.Public | BindingFlags.Instance
        );
        var destinationProperties = typeof(TDestination).GetProperties(
            BindingFlags.Public | BindingFlags.Instance
        );

        // Create a dictionary for quick lookup of destination properties by name
        var destinationPropertyMap = destinationProperties.ToDictionary(p => p.Name, p => p);

        foreach (var sourceProperty in sourceProperties)
        {
            // Check if the destination has a matching property
            if (
                destinationPropertyMap.TryGetValue(sourceProperty.Name, out var destinationProperty)
            )
            {
                // Check if the property types are compatible
                if (
                    destinationProperty.PropertyType.IsAssignableFrom(sourceProperty.PropertyType)
                    && destinationProperty.CanWrite
                )
                {
                    // Copy the value from the source to the destination
                    var value = sourceProperty.GetValue(source);
                    destinationProperty.SetValue(destination, value);
                }
            }
        }
        return destination;
    }

    public static TDestination ConvertModelChangeDateTimeToString<TSource, TDestination>(
        TSource source
    )
        where TDestination : new()
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        // Create an instance of the destination type
        TDestination destination = new();

        // Get properties of both source and destination types
        var sourceProperties = typeof(TSource)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .DistinctBy(x => x.Name);
        var destinationProperties = typeof(TDestination)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .DistinctBy(x => x.Name);

        // Create a dictionary for quick lookup of destination properties by name
        var destinationPropertyMap = destinationProperties.ToDictionary(p => p.Name, p => p);

        foreach (var sourceProperty in sourceProperties)
        {
            // Check if the destination has a matching property
            if (
                destinationPropertyMap.TryGetValue(sourceProperty.Name, out var destinationProperty)
            )
            {
                var sourceValue = sourceProperty.GetValue(source);

                // Check if the property types are compatible
                if (
                    destinationProperty.PropertyType == typeof(string)
                    && (
                        sourceProperty.PropertyType == typeof(DateTime)
                        || sourceProperty.PropertyType == typeof(DateTime?)
                    )
                )
                {
                    // Convert DateTime to string
                    destinationProperty.SetValue(
                        destination,
                        sourceValue == null ? null : ((DateTime)sourceValue).ToShortDateString()
                    ); // ISO 8601 format
                }
                else if (
                    destinationProperty.PropertyType.IsAssignableFrom(sourceProperty.PropertyType)
                    && destinationProperty.CanWrite
                )
                {
                    // Copy the value from the source to the destination
                    destinationProperty.SetValue(destination, sourceValue);
                }
            }
        }

        return destination;
    }
}
