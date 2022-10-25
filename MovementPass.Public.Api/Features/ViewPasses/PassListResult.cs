namespace MovementPass.Public.Api.Features.ViewPasses;

using System.Collections.Generic;

public class PassListResult
{
    public IEnumerable<PassItem> Passes { get; set; }

    public PassListKey NextKey { get; set; }
}