# ByteFoo.Extensions.Configuration.Esc

based on: https://github.com/Azure/AppConfiguration-DotnetProvider

```
using ByteFoo.Extensions.Configuration.Esc;

c.AddEscConfiguration(options =>
{
    options
        .Connect("Pulumi-Api-Key")
        .Path(escPath)
        .MapKeys(s => new ValueTask<string>(s.Replace("_", ":")))
        .MapValues(s => new ValueTask<string>(s.Replace("foo", "bar")))
        .Select("SomePrefix:AppSettings*")
        .TrimKeyPrefix("SomePrefix:");
});
```