// <copyright file="InvalidVersionException.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Streams.Stores;

using System;
using System.Runtime.Serialization;

/// <summary>
/// Class InvalidVersionException.
/// Implements the <see cref="Exception" />.
/// </summary>
/// <seealso cref="Exception" />
[DataContract]
public class InvalidVersionException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidVersionException" /> class.
    /// </summary>
    public InvalidVersionException() => StreamId = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidVersionException" /> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public InvalidVersionException(string? message)
        : base(message) => StreamId = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidVersionException"/> class.
    /// </summary>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="expectedVersion">The expected version.</param>
    /// <param name="currentVersion">The current version.</param>
    public InvalidVersionException(string streamId, long expectedVersion, long currentVersion)
        : base($"Stream '{streamId}' expected version is '{expectedVersion}' but was '{currentVersion}'")
    {
        StreamId = streamId;
        ExpectedVersion = expectedVersion;
        CurrentVersion = currentVersion;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidVersionException" /> class.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception,
    /// or a null reference (<see langword="Nothing" /> in Visual Basic) if no inner exception is specified.</param>
    public InvalidVersionException(string? message, Exception? innerException)
        : base(message, innerException) => StreamId = string.Empty;

    /// <summary>
    /// Gets the current version.
    /// </summary>
    public long CurrentVersion { get; }

    /// <summary>
    /// Gets the expected version.
    /// </summary>
    public long ExpectedVersion { get; }

    /// <summary>
    /// Gets the stream identifier.
    /// </summary>
    public string StreamId { get; }
}