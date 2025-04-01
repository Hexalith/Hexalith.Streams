// <copyright file="StreamStoreResultTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Streams.Tests.Stores;

using Hexalith.Streams.Stores;

using Shouldly;

/// <summary>
/// Tests for <see cref="StreamStoreResult{TData}"/>.
/// </summary>
public class StreamStoreResultTests
{
    /// <summary>
    /// Tests that the result can be constructed correctly.
    /// </summary>
    [Fact]
    public void Constructor_ShouldCreateInstance()
    {
        // Arrange
        StreamStoreObject<string>[] items =
        [
            new StreamStoreObject<string>("data1", "idempotency1"),
            new StreamStoreObject<string>("data2", "idempotency2"),
        ];
        const long version = 2;

        // Act
        var result = new StreamStoreResult<string>(items, version);

        // Assert
        _ = result.ShouldNotBeNull();
        result.Objects.ShouldBe(items);
        result.Version.ShouldBe(version);
    }

    /// <summary>
    /// Tests that null items collection is handled correctly.
    /// </summary>
    [Fact]
    public void Constructor_WithNullItems_ShouldCreateInstanceWithEmptyCollection()
    {
        // Act
        var result = new StreamStoreResult<string>(null!, 1);

        // Assert
        _ = result.Objects.ShouldNotBeNull();
        result.Objects.ShouldBeEmpty();
    }

    /// <summary>
    /// Tests that Items property can be accessed correctly.
    /// </summary>
    [Fact]
    public void Items_ShouldReturnCorrectCollection()
    {
        // Arrange
        StreamStoreObject<string>[] items =
        [
            new StreamStoreObject<string>("data1", "idempotency1"),
            new StreamStoreObject<string>("data2", "idempotency2"),
        ];
        var result = new StreamStoreResult<string>(items, 2);

        // Act
        IEnumerable<IStreamStoreObject<string>> resultItems = result.Objects;

        // Assert
        _ = resultItems.ShouldNotBeNull();
        resultItems.Count().ShouldBe(2);
        resultItems.ShouldBe(items);
    }

    /// <summary>
    /// Tests that Version property can be accessed correctly.
    /// </summary>
    [Fact]
    public void Version_ShouldReturnCorrectValue()
    {
        // Arrange
        StreamStoreObject<string>[] items =
        [
            new StreamStoreObject<string>("data1", "idempotency1"),
            new StreamStoreObject<string>("data2", "idempotency2"),
        ];
        const long version = 5;
        var result = new StreamStoreResult<string>(items, version);

        // Act
        long resultVersion = result.Version;

        // Assert
        resultVersion.ShouldBe(version);
    }
}