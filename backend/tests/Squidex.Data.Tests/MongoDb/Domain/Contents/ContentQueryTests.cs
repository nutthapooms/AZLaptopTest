﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using NodaTime.Text;
using Squidex.Domain.Apps.Core;
using Squidex.Domain.Apps.Core.Schemas;
using Squidex.Domain.Apps.Entities.Contents;
using Squidex.Domain.Apps.Entities.Contents.Operations;
using Squidex.Domain.Apps.Entities.TestHelpers;
using Squidex.Infrastructure;
using Squidex.Infrastructure.Queries;
using Squidex.MongoDb.TestHelpers;
using ClrFilter = Squidex.Infrastructure.Queries.ClrFilter;

namespace Squidex.MongoDb.Domain.Contents;

public class ContentQueryTests : GivenContext
{
    static ContentQueryTests()
    {
        MongoContentEntity.RegisterClassMap();

        MongoTestUtils.SetupBson();
    }

    public ContentQueryTests()
    {
        Schema = Schema with { Version = 3 };
        Schema = Schema
            .AddString(1, "firstName", Partitioning.Language,
                new StringFieldProperties())
            .AddString(2, "lastName", Partitioning.Language,
                new StringFieldProperties())
            .AddBoolean(3, "isAdmin", Partitioning.Invariant,
                new BooleanFieldProperties())
            .AddNumber(4, "age", Partitioning.Invariant,
                new NumberFieldProperties())
            .AddDateTime(5, "birthday", Partitioning.Invariant,
                new DateTimeFieldProperties())
            .AddAssets(6, "pictures", Partitioning.Invariant,
                new AssetsFieldProperties())
            .AddReferences(7, "friends", Partitioning.Invariant,
                new ReferencesFieldProperties())
            .AddString(8, "dashed-field", Partitioning.Invariant,
                new StringFieldProperties())
            .AddJson(9, "json", Partitioning.Invariant,
                new JsonFieldProperties())
            .AddArray(10, "hobbies", Partitioning.Invariant, a => a
                .AddString(101, "name"));
    }

    [Fact]
    public void Should_make_query_with_id()
    {
        var id = Guid.NewGuid();

        var filter = ClrFilter.Eq("id", id);

        AssertQuery($"{{ '_id' : '{AppId.Id}--{id}' }}", filter);
    }

    [Fact]
    public void Should_make_query_with_id_string()
    {
        var id = DomainId.NewGuid().ToString();

        var filter = ClrFilter.Eq("id", id);

        AssertQuery($"{{ '_id' : '{AppId.Id}--{id}' }}", filter);
    }

    [Fact]
    public void Should_make_query_with_id_list()
    {
        var id = Guid.NewGuid();

        var filter = ClrFilter.In("id", new List<Guid> { id });

        AssertQuery($"{{ '_id' : {{ '$in' : ['{AppId.Id}--{id}'] }} }}", filter);
    }

    [Fact]
    public void Should_make_query_with_id_string_list()
    {
        var id = DomainId.NewGuid().ToString();

        var filter = ClrFilter.In("id", new List<string> { id });

        AssertQuery($"{{ '_id' : {{ '$in' : ['{AppId.Id}--{id}'] }} }}", filter);
    }

    [Fact]
    public void Should_make_query_with_lastModified()
    {
        var time = "1988-01-19T12:00:00Z";

        var filter = ClrFilter.Eq("lastModified", InstantPattern.ExtendedIso.Parse(time).Value);

        AssertQuery("{ 'mt' : ISODate('[value]') }", filter, time);
    }

    [Fact]
    public void Should_make_query_with_lastModifiedBy()
    {
        var filter = ClrFilter.Eq("lastModifiedBy", "me");

        AssertQuery("{ 'mb' : 'me' }", filter);
    }

    [Fact]
    public void Should_make_query_with_created()
    {
        var time = "1988-01-19T12:00:00Z";

        var filter = ClrFilter.Eq("created", InstantPattern.ExtendedIso.Parse(time).Value);

        AssertQuery("{ 'ct' : ISODate('[value]') }", filter, time);
    }

    [Fact]
    public void Should_make_query_with_createdBy()
    {
        var filter = ClrFilter.Eq("createdBy", "subject:me");

        AssertQuery("{ 'cb' : 'subject:me' }", filter);
    }

    [Fact]
    public void Should_make_query_with_version()
    {
        var filter = ClrFilter.Eq("version", 2L);

        AssertQuery("{ 'vs' : NumberLong(2) }", filter);
    }

    [Fact]
    public void Should_make_query_with_datetime_data()
    {
        var time = "1988-01-19T12:00:00Z";

        var filter = ClrFilter.Eq("data/birthday/iv", InstantPattern.General.Parse(time).Value);

        AssertQuery("{ 'do.birthday.iv' : '[value]' }", filter, time);
    }

    [Fact]
    public void Should_make_query_with_underscore_field()
    {
        var filter = ClrFilter.Eq("data/dashed_field/iv", "Value");

        AssertQuery("{ 'do.dashed-field.iv' : 'Value' }", filter);
    }

    [Fact]
    public void Should_make_query_with_json_dot_field()
    {
        var filter = ClrFilter.Eq("data/json/iv/with\\.dot", "Value");

        AssertQuery("{ 'do.json.iv.with_§§_dot' : 'Value' }", filter);
    }

    [Fact]
    public void Should_make_query_with_json_slash_field()
    {
        var filter = ClrFilter.Eq("data/json/iv/with\\/slash", "Value");

        AssertQuery("{ 'do.json.iv.with/slash' : 'Value' }", filter);
    }

    [Fact]
    public void Should_make_query_with_references_equals()
    {
        var filter = ClrFilter.Eq("data/friends/iv", "guid");

        AssertQuery("{ 'do.friends.iv' : 'guid' }", filter);
    }

    [Fact]
    public void Should_make_query_with_array_field()
    {
        var filter = ClrFilter.Eq("data/hobbies/iv/name", "PC");

        AssertQuery("{ 'do.hobbies.iv.name' : 'PC' }", filter);
    }

    [Fact]
    public void Should_make_query_with_assets_equals()
    {
        var filter = ClrFilter.Eq("data/pictures/iv", "guid");

        AssertQuery("{ 'do.pictures.iv' : 'guid' }", filter);
    }

    [Fact]
    public void Should_make_orderby_with_single_field()
    {
        var sorting = SortBuilder.Descending("data/age/iv");

        AssertSorting("{ 'do.age.iv' : -1 }", sorting);
    }

    [Fact]
    public void Should_make_orderby_with_multiple_fields()
    {
        var sorting1 = SortBuilder.Ascending("data/age/iv");
        var sorting2 = SortBuilder.Descending("data/firstName/en");

        AssertSorting("{ 'do.age.iv' : 1, 'do.firstName.en' : -1 }", sorting1, sorting2);
    }

    private void AssertQuery(string expected, FilterNode<ClrValue> filter, object? arg = null)
    {
        AssertQuery(new ClrQuery { Filter = filter }, expected, arg);
    }

    private void AssertQuery(ClrQuery query, string expected, object? arg = null)
    {
        var filter = query.AdjustToContentModel(AppId.Id).BuildFilter<MongoContentEntity>(false).Filter!;

        var rendered =
            filter.Render(
                new RenderArgs<MongoContentEntity>(
                    BsonSerializer.SerializerRegistry.GetSerializer<MongoContentEntity>(),
                    BsonSerializer.SerializerRegistry))
            .ToString();

        Assert.Equal(Cleanup(expected, arg), rendered);
    }

    private void AssertSorting(string expected, params SortNode[] sort)
    {
        var cursor = A.Fake<IFindFluent<MongoContentEntity, MongoContentEntity>>();

        var rendered = string.Empty;

        A.CallTo(() => cursor.Sort(A<SortDefinition<MongoContentEntity>>._))
            .Invokes((SortDefinition<MongoContentEntity> sortDefinition) =>
            {
                rendered = sortDefinition.Render(
                    new RenderArgs<MongoContentEntity>(
                        BsonSerializer.SerializerRegistry.GetSerializer<MongoContentEntity>(),
                        BsonSerializer.SerializerRegistry))
                    .ToString();
            });

        cursor.QuerySort(new ClrQuery { Sort = sort.ToList() }.AdjustToContentModel(AppId.Id));

        Assert.Equal(Cleanup(expected), rendered);
    }

    private static string Cleanup(string filter, object? arg = null)
    {
        return filter.Replace('\'', '"').Replace("[value]", arg?.ToString(), StringComparison.Ordinal);
    }
}
