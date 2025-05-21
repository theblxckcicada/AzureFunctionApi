using System.Reflection;
using EasySMS.API.Azure.Models;

namespace EasySMS.API.DependencyInjection
{
    public interface IEntityTypeProvider
    {
        IEnumerable<Type> GetAll();
        Type? GetByName(string name);
    }

    public class EntityTypeProvider(IEnumerable<Type> entityTypes) : IEntityTypeProvider
    {
        private readonly List<Type> entityTypes = [.. entityTypes];

        public IEnumerable<Type> GetAll() => entityTypes;

        public Type? GetByName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            return entityTypes.FirstOrDefault(t =>
                string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase)
            );
        }

        private static readonly Type EntityBaseType = typeof(EntityBase);

        public static IEnumerable<Type> GetEntityTypes(params Assembly[] assemblies)
        {
            if (assemblies.Length == 0)
            {
                return [];
            }

            return assemblies
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type =>
                    EntityBaseType.IsAssignableFrom(type)
                    && type != EntityBaseType
                    && !type.IsAbstract
                );
        }

        public static IEnumerable<Type> GetEntityTypesInAssemblyContaining<T>()
        {
            var assembly = typeof(T).Assembly;
            return GetEntityTypes(assembly);
        }
    }
}
