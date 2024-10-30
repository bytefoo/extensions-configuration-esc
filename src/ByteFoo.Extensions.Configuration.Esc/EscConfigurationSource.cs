using System;
using Microsoft.Extensions.Configuration;

namespace ByteFoo.Extensions.Configuration.Esc
{
    public class EscConfigurationSource : IConfigurationSource
    {
        private readonly Func<EscConfigurationOptions> _optionsProvider;

        public EscConfigurationSource(Action<EscConfigurationOptions> optionsInitializer)
        {
            _optionsProvider = () =>
            {
                var options = new EscConfigurationOptions();
                optionsInitializer(options);
                return options;
            };
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new EscConfigurationProvider(_optionsProvider());
        }
    }
}