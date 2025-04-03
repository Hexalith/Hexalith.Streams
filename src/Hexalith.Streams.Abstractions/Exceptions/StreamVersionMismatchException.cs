// <copyright file="StreamVersionMismatchException.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Streams.Exceptions;

using System;

/// <summary>
/// Exception thrown when the stream version does not match the added item version.
/// </summary>
public sealed class StreamVersionMismatchException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StreamVersionMismatchException"/> class.
    /// </summary>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="version">The version of the stream.</param>
    /// <param name="expectedVersion">The expected version of the stream.</param>
    public StreamVersionMismatchException(string streamId, long version, long expectedVersion)
        : base($"The added item version {version} does not match the Stream '{streamId}' expected version '{expectedVersion}'.")
    {
        StreamId = streamId;
        Version = version;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamVersionMismatchException"/> class.
    /// </summary>
    public StreamVersionMismatchException()
        : base() => StreamId = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamVersionMismatchException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public StreamVersionMismatchException(string? message)
        : base(message) => StreamId = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamVersionMismatchException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public StreamVersionMismatchException(string? message, Exception? innerException)
        : base(message, innerException) => StreamId = string.Empty;

    /// <summary>
    /// Gets the stream next item expected version.
    /// </summary>
    public long ExpectedVersion { get; }

    /// <summary>
    /// Gets the stream identifier.
    /// </summary>
    public string StreamId { get; }

    /// <summary>
    /// Gets the version.
    /// </summary>
    public long Version { get; }
}