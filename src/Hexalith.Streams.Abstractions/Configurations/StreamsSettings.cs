// <copyright file="StreamsSettings.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Streams.Configurations;

using System.Runtime.Serialization;

using Hexalith.Commons.Configurations;

/// <summary>
/// Represents the settings for streams.
/// </summary>
[DataContract]
public record StreamsSettings(string? FileStreamRootPath, TimeSpan? LockTimeout) : ISettings
{
    /// <summary>
    /// Gets the root path for the file stream.
    /// </summary>
    public string FileStreamRootPath { get; init; } = string.IsNullOrWhiteSpace(FileStreamRootPath) ? "/Hexalith/FileStreams" : FileStreamRootPath;

    /// <summary>
    /// Gets the lock timeout duration.
    /// </summary>
    public TimeSpan? LockTimeout { get; init; } = LockTimeout ?? TimeSpan.FromMinutes(1);

    /// <summary>
    /// Gets the configuration name.
    /// </summary>
    /// <returns>The configuration name.</returns>
    public static string ConfigurationName() => $"{nameof(Hexalith)}:Streams";
}