using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Esc.Sdk.Cli;
using Microsoft.Extensions.Configuration;

namespace ByteFoo.Extensions.Configuration.Esc
{
    public class EscConfigurationProvider : ConfigurationProvider
    {
        private readonly EscConfig _escConfig;
        private readonly EscConfigurationOptions _options;

        public EscConfigurationProvider(EscConfigurationOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));

            var escOptions = new EscOptions
            {
                PulumiAccessToken = _options.PulumiAccessToken,
                OrgName = _options.OrgName,
                ProjectName = _options.ProjectName,
                EnvironmentName = _options.EnvironmentName,
            };

            if (!string.IsNullOrWhiteSpace(options.EscPath))
            {
                escOptions.EscPath = options.EscPath;
            }

            _escConfig = new EscConfig(escOptions);
        }

        public override void Load()
        {
            InitializeAsync(CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            _escConfig.TryLoad(out Dictionary<string, string> data);
            data = await FilterKeyValues(data).ConfigureAwait(false);

            var mappedData = await MapValues(data).ConfigureAwait(false);
            SetData(await PrepareData(mappedData, cancellationToken).ConfigureAwait(false));
        }

        private async Task<Dictionary<string, string>> FilterKeyValues(Dictionary<string, string> data)
        {
            var filteredConfig = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (_options.KeyMappers.Any())
            {
                data = await MapKeys(data);
            }

            if (!_options.KeyValueSelectors.Any())
            {
                return data;
            }

            foreach (var kvp in data)
            {
                foreach (var keyValueSelector in _options.KeyValueSelectors)
                {
#if NETSTANDARD2_0
                    if (FileSystemName.MatchesSimpleExpression(keyValueSelector.KeyFilter.AsSpan(), kvp.Key.AsSpan()))
                    {
                        filteredConfig[kvp.Key] = kvp.Value;
                    }
#else
                    if (System.IO.Enumeration.FileSystemName.MatchesSimpleExpression(
                            keyValueSelector.KeyFilter.AsSpan(), kvp.Key.AsSpan()))
                    {
                        filteredConfig[kvp.Key] = kvp.Value;
                    }
#endif
                }
            }

            return filteredConfig;
        }

        private async Task<Dictionary<string, string>> MapKeys(Dictionary<string, string> data)
        {
            var mappedData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in data)
            {
                var key = kvp.Key;
                var originalKey = key;
                var originalValue = kvp.Value;

                foreach (var func in _options.KeyMappers)
                {
                    key = await func(key).ConfigureAwait(false);
                }

                if (key != null)
                {
                    mappedData.Remove(originalKey);
                    mappedData[key] = originalValue;
                }
            }

            return mappedData;
        }

        private async Task<Dictionary<string, string>> MapValues(Dictionary<string, string> data)
        {
            var mappedData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in data)
            {
                var value = kvp.Value;

                foreach (var func in _options.ValueMappers)
                {
                    value = await func(value).ConfigureAwait(false);
                }

                if (value != null)
                {
                    mappedData[kvp.Key] = value;
                }
            }

            return mappedData;
        }


        private async Task<Dictionary<string, string>> PrepareData(Dictionary<string, string> data,
            CancellationToken cancellationToken = default)
        {
            var applicationData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in data)
            {
                var key = kvp.Key;

                foreach (var prefix in _options.KeyPrefixes)
                {
                    if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        key = key.Substring(prefix.Length);
                        break;
                    }
                }

                applicationData[key] = kvp.Value;
            }

            return applicationData;
        }

        private void SetData(IDictionary<string, string> data)
        {
            // Set the application data for the configuration provider
            Data = data;

            // Notify that the configuration has been updated
            OnReload();
        }
    }
}