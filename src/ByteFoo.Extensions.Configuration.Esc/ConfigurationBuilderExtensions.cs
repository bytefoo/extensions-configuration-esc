using System;
using Microsoft.Extensions.Configuration;

namespace ByteFoo.Extensions.Configuration.Esc
{
    public static class ConfigurationBuilderExtensions
    {
        public static IConfigurationBuilder AddEscConfiguration(
            this IConfigurationBuilder builder,
            Action<EscConfigurationOptions> action)
        {
            return builder.Add(new EscConfigurationSource(action));
        }
    }
}