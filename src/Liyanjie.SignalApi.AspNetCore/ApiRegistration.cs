﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Liyanjie.SignalApi.AspNetCore
{
    public class ApiRegistration
    {
        readonly Type type_serviceBase = typeof(ServiceBase);
        readonly Type type_filterMetadata = typeof(IFilterMetadata);

        public ICollection<ApiDescriptor> ApiCollections { get; } = new List<ApiDescriptor>();
        public ICollection<IFilterMetadata> GlobalFilters { get; } = new List<IFilterMetadata>();

        public IAuthenticationProvider AuthenticationProvider { get; set; } = new DefaultAuthenticationProvider();
        public IAuthorizationProvider AuthorizationProvider { get; set; } = new DefaultAuthorizationProvider();
        public IValidationProvider ValidationProvider { get; set; }

        public ApiRegistration RegisterApisFromAssemblyByClass<TClass>()
        {
            var assembly = typeof(TClass).Assembly;
            var serviceTypes = assembly.DefinedTypes
                .Where(_ => !_.IsAbstract)
                .Where(_ => type_serviceBase.IsAssignableFrom(_))
                .ToList();
            foreach (var type in serviceTypes)
            {
                var typeName = type.Name.EndsWith("Service", StringComparison.OrdinalIgnoreCase)
                    ? type.Name.Substring(0, type.Name.Length - 7)
                    : type.Name;

                var typeAttributeTypes = type.GetCustomAttributesData()
                    .Select(_ => _.AttributeType)
                    .Where(_ => type_filterMetadata.IsAssignableFrom(_));
                var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
                foreach (var method in methods)
                {
                    var methodAttributeTypes = method.GetCustomAttributesData()
                        .Select(_ => _.AttributeType)
                        .Where(_ => type_filterMetadata.IsAssignableFrom(_));
                    var apiMethodAttribute = method.GetCustomAttribute<ApiMethodAttribute>();
                    var apiName = apiMethodAttribute?.Name ?? $"{typeName}.{method.Name}";

                    if (ApiCollections.Any(_ => _.Name == apiName))
                        throw new Exception($"A descriptor named \"{apiName}\" already exists");

                    ApiCollections.Add(new ApiDescriptor
                    {
                        Name = apiName,
                        Info = method,
                        FilterTypes = typeAttributeTypes.Union(methodAttributeTypes),
                    });
                }
            }

            return this;
        }
    }
}