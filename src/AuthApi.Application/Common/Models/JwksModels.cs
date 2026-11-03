using System.Text.Json.Serialization;

namespace AuthApi.Application.Common.Models;

public sealed record JsonWebKeySetResponse(
    [property: JsonPropertyName("keys")] IReadOnlyList<JsonWebKeyDto> Keys);

public sealed record JsonWebKeyDto(
    [property: JsonPropertyName("kty")] string Kty,
    [property: JsonPropertyName("use")] string Use,
    [property: JsonPropertyName("alg")] string Alg,
    [property: JsonPropertyName("kid")] string Kid,
    [property: JsonPropertyName("n")] string N,
    [property: JsonPropertyName("e")] string E);

