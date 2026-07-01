using System;

namespace Agenda.Objects;

public record Username
{

    /// <summary>
    /// The username value.
    /// </summary>
    public string Value { get; }

    private Username(string value)
    {
        Value = value;
    }

    public static Username Empty => new(string.Empty);

    /// <summary>
    /// Implicitly converts a <see cref="Username"/> to a <see cref="string"/>.
    /// </summary>
    /// <param name="username">The <see cref="Username"/> instance.</param>
    public static implicit operator string(Username username) => username.Value;

    /// <summary>
    /// Creates a new <see cref="Username"/> from the specified <paramref name="username"/> string.
    /// </summary>
    /// <param name="username">The username string.</param>
    /// <returns>A new <see cref="Username"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown if the <paramref name="username"/> is <see langword="null"/> or whitespace.</exception>
    public static Username FromString(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username cannot be null or whitespace", nameof(username));
        }

        return new(username);
    }
}