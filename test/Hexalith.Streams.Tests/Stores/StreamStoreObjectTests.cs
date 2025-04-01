// <copyright file="StreamStoreObjectTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Streams.Tests.Stores;

using Hexalith.Streams.Stores;

using Shouldly;

/// <summary>
/// Tests for <see cref="StreamStoreObject{TData}"/>.
/// </summary>
public class StreamStoreObjectTests
{
    /// <summary>
    /// Tests that the object can be constructed correctly.
    /// </summary>
    [Fact]
    public void Constructor_ShouldCreateInstance()
    {
        // Arrange
        const string data = "test-data";
        const string idempotencyId = "test-idempotency-id";

        // Act
        var storeObject = new StreamStoreObject<string>(data, idempotencyId);

        // Assert
        _ = storeObject.ShouldNotBeNull();
        storeObject.Data.ShouldBe(data);
        storeObject.IdempotencyId.ShouldBe(idempotencyId);
    }

    /// <summary>
    /// Tests that an object is not equal to null.
    /// </summary>
    [Fact]
    public void Equals_WithNull_ShouldBeFalse()
    {
        // Arrange
        var storeObject = new StreamStoreObject<string>("data", "idempotency-id");

        // Act
        bool areEqual = storeObject.Equals(null);

        // Assert
        areEqual.ShouldBeFalse();
    }

    /// <summary>
    /// Tests that an object is equal to itself.
    /// </summary>
    [Fact]
    public void Equals_WithSameReference_ShouldBeTrue()
    {
        // Arrange
        var storeObject = new StreamStoreObject<string>("data", "idempotency-id");

        // Act
        bool areEqual = storeObject.Equals(storeObject);

        // Assert
        areEqual.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that two objects with the same values are equal.
    /// </summary>
    [Fact]
    public void Equals_WithSameValues_ShouldBeTrue()
    {
        // Arrange
        var object1 = new StreamStoreObject<string>("data", "idempotency-id");
        var object2 = new StreamStoreObject<string>("data", "idempotency-id");

        // Act
        bool areEqual = object1.Equals(object2);

        // Assert
        areEqual.ShouldBeTrue();
        (object1 == object2).ShouldBeTrue();
        (object1 != object2).ShouldBeFalse();
    }

    /// <summary>
    /// Tests that the object implements IStreamStoreObject interface.
    /// </summary>
    [Fact]
    public void Object_ShouldImplementIStreamStoreObject()
    {
        // Arrange
        var storeObject = new StreamStoreObject<string>("data", "idempotency-id");

        // Assert
        _ = storeObject.ShouldBeAssignableTo<IStreamStoreObject<string>>();
    }
}