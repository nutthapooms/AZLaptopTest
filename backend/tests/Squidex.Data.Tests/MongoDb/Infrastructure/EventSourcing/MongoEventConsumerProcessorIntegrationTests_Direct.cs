﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

#pragma warning disable SA1300 // Element should begin with upper-case letter

using Squidex.Events;

namespace Squidex.MongoDb.Infrastructure.EventSourcing;

[Trait("Category", "Dependencies")]
public class MongoEventConsumerProcessorIntegrationTests_Direct(MongoEventStoreFixture_Direct fixture) : EventConsumerProcessorIntegrationTests, IClassFixture<MongoEventStoreFixture_Direct>
{
    public MongoEventStoreFixture _ { get; } = fixture;

    public override IEventStore CreateStore()
    {
        return _.EventStore;
    }
}
