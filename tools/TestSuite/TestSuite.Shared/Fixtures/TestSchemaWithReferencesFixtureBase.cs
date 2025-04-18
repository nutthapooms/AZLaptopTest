﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.ClientLibrary;
using TestSuite.Model;

namespace TestSuite.Fixtures;

public abstract class TestSchemaWithReferencesFixtureBase(string schemaName) : CreatedAppFixture
{
    public IContentsClient<TestEntityWithReferences, TestEntityWithReferencesData> Contents { get; private set; }

    public string SchemaName { get; } = schemaName;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        await Factories.CreateAsync($"{nameof(TestEntityWithReferences)}_{SchemaName}", async () =>
        {
            try
            {
                await TestEntityWithReferences.CreateSchemaAsync(Client.Schemas, SchemaName);
            }
            catch (SquidexException ex)
            {
                if (ex.StatusCode != 400)
                {
                    throw;
                }
            }

            return true;
        });

        Contents = Client.Contents<TestEntityWithReferences, TestEntityWithReferencesData>(SchemaName);
    }
}
