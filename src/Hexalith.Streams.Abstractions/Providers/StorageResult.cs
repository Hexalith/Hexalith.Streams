// <copyright file="StorageResult.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Streams.Providers;
/// <summary>
/// Represents the result of a storage operation.
/// </summary>
/// <typeparam name="TValue">The type of the value.</typeparam>
public record StorageResult<TValue>(TValue Value, bool Success);