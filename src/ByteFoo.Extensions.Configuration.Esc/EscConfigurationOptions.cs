using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ByteFoo.Extensions.Configuration.Esc
{
    public class EscConfigurationOptions
    {
        private readonly List<Func<string, ValueTask<string>>>
            _keyMappers = new List<Func<string, ValueTask<string>>>();

        private readonly SortedSet<string> _keyPrefixes =
            new SortedSet<string>(Comparer<string>.Create((k1, k2) =>
                -string.Compare(k1, k2, StringComparison.OrdinalIgnoreCase)));

        private readonly List<KeyValueSelector> _kvSelectors = new List<KeyValueSelector>();

        private readonly List<Func<string, ValueTask<string>>> _valueMappers =
            new List<Func<string, ValueTask<string>>>();

        public string EnvironmentName { get; private set; }

        public string EscPath { get; private set; }

        internal IEnumerable<Func<string, ValueTask<string>>> KeyMappers => _keyMappers;

        internal IEnumerable<string> KeyPrefixes => _keyPrefixes;

        public IEnumerable<KeyValueSelector> KeyValueSelectors => _kvSelectors;

        public string OrgName { get; private set; }

        public string ProjectName { get; private set; } = "default";

        public string PulumiAccessToken { get; private set; }

        internal IEnumerable<Func<string, ValueTask<string>>> ValueMappers => _valueMappers;

        /// <summary>
        ///     Connect to Pulumi Esc.  This method must be called before any other configuration options are set.
        /// </summary>
        /// <param name="orgName">Pulumi Esc Organization Name.</param>
        /// <param name="projectName">Pulumi Esc Project Name.</param>
        /// <param name="environmentName">Pulumi Esc Environment Name.</param>
        /// <param name="pulumiAccessToken">Pulumi access token, PULUMI_ACCESS_TOKEN environment variable is the default.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public EscConfigurationOptions Connect(
            string pulumiAccessToken,
            string orgName,
            string projectName,
            string environmentName
        )
        {
            if (string.IsNullOrWhiteSpace(pulumiAccessToken))
            {
                throw new ArgumentNullException(nameof(pulumiAccessToken));
            }

            if (string.IsNullOrWhiteSpace(orgName))
            {
                throw new ArgumentNullException(nameof(orgName));
            }

            if (string.IsNullOrWhiteSpace(projectName))
            {
                throw new ArgumentNullException(nameof(projectName));
            }

            if (string.IsNullOrWhiteSpace(environmentName))
            {
                throw new ArgumentNullException(nameof(environmentName));
            }

            PulumiAccessToken = pulumiAccessToken;
            OrgName = orgName;
            ProjectName = projectName;
            EnvironmentName = environmentName;

            return this;
        }

        public EscConfigurationOptions Path(string escPath)
        {
            if (string.IsNullOrWhiteSpace(escPath))
            {
                throw new ArgumentNullException(nameof(escPath));
            }

            EscPath = escPath;
            return this;
        }

        public EscConfigurationOptions Select(string keyFilter)
        {
            if (string.IsNullOrEmpty(keyFilter))
            {
                throw new ArgumentNullException(nameof(keyFilter));
            }

            if (!_kvSelectors.Any(s => s.KeyFilter.Equals(keyFilter)))
            {
                _kvSelectors.Add(new KeyValueSelector
                {
                    KeyFilter = keyFilter
                });
            }

            return this;
        }

        public EscConfigurationOptions TrimKeyPrefix(string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
            {
                throw new ArgumentNullException(nameof(prefix));
            }

            _keyPrefixes.Add(prefix);
            return this;
        }

        public EscConfigurationOptions MapKeys(Func<string, ValueTask<string>> mapper)
        {
            if (mapper == null)
            {
                throw new ArgumentNullException(nameof(mapper));
            }

            _keyMappers.Add(mapper);
            return this;
        }

        public EscConfigurationOptions MapValues(Func<string, ValueTask<string>> mapper)
        {
            if (mapper == null)
            {
                throw new ArgumentNullException(nameof(mapper));
            }

            _valueMappers.Add(mapper);
            return this;
        }
    }
}