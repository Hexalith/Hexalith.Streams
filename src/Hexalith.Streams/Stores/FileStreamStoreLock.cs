// <copyright file="FileStreamStoreLock.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Streams.Stores;

using System;
using System.Runtime.Serialization;

/// <summary>
/// Represents a lock for a file stream store.
/// </summary>
/// <param name="SessionId"> The session identifier.</param>
/// <param name="Expiration"> The expiration date and time of the lock.</param>
[DataContract]
public record FileStreamStoreLock(
    [property: DataMember(Order = 1)] string SessionId,
    [property: DataMember(Order = 2)] DateTimeOffset Expiration);