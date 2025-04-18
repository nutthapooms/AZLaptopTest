﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Domain.Apps.Core.Rules.Triggers;
using Squidex.Domain.Apps.Core.TestHelpers;
using Squidex.Domain.Apps.Entities.TestHelpers;
using Squidex.Infrastructure.Validation;

namespace Squidex.Domain.Apps.Entities.Rules.DomainObject.Guards.Triggers;

public class UsageTriggerValidationTests : GivenContext, IClassFixture<TranslationsFixture>
{
    [Fact]
    public async Task Should_add_error_if_num_days_less_than_1()
    {
        var trigger = new UsageTrigger { NumDays = 0 };

        var errors = await RuleTriggerValidator.ValidateAsync(AppId.Id, trigger, AppProvider);

        errors.Should().BeEquivalentTo(
            new List<ValidationError>
            {
                new ValidationError("Num days must be between 1 and 30.", "NumDays"),
            });
    }

    [Fact]
    public async Task Should_add_error_if_num_days_greater_than_30()
    {
        var trigger = new UsageTrigger { NumDays = 32 };

        var errors = await RuleTriggerValidator.ValidateAsync(AppId.Id, trigger, AppProvider);

        errors.Should().BeEquivalentTo(
            new List<ValidationError>
            {
                new ValidationError("Num days must be between 1 and 30.", "NumDays"),
            });
    }

    [Fact]
    public async Task Should_not_add_error_if_num_days_is_valid()
    {
        var trigger = new UsageTrigger { NumDays = 20 };

        var errors = await RuleTriggerValidator.ValidateAsync(AppId.Id, trigger, AppProvider);

        Assert.Empty(errors);
    }

    [Fact]
    public async Task Should_not_add_error_if_num_days_is_not_defined()
    {
        var trigger = new UsageTrigger();

        var errors = await RuleTriggerValidator.ValidateAsync(AppId.Id, trigger, AppProvider);

        Assert.Empty(errors);
    }
}
