﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Text.Encodings.Web;
using Fluid;
using Fluid.Ast;
using Fluid.Values;
using Microsoft.Extensions.DependencyInjection;
using Squidex.Assets;
using Squidex.Domain.Apps.Core.Assets;
using Squidex.Domain.Apps.Core.Rules.EnrichedEvents;
using Squidex.Domain.Apps.Core.Templates;
using Squidex.Infrastructure;
using static Parlot.Fluent.Parsers;

namespace Squidex.Domain.Apps.Entities.Assets;

public sealed class AssetsFluidExtension : IFluidExtension
{
    private static readonly FluidValue ErrorNoAsset = new StringValue("NoAsset");
    private static readonly FluidValue ErrorNoImage = new StringValue("NoImage");
    private static readonly FluidValue ErrorTooBig = new StringValue("ErrorTooBig");
    private readonly IServiceProvider serviceProvider;

    public AssetsFluidExtension(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public void RegisterLanguageExtensions(CustomFluidParser parser, TemplateOptions options)
    {
        AddAssetFilter(options);
        AddAssetTextFilter(options);

        parser.RegisterParserTag("asset",
            parser.PrimaryParser.AndSkip(ZeroOrOne(parser.CommaParser)).And(parser.PrimaryParser),
            ResolveAsset);
    }

    private async ValueTask<Completion> ResolveAsset(ValueTuple<Expression, Expression> arguments, TextWriter writer, TextEncoder encoder, TemplateContext context)
    {
        if (context.GetValue("event")?.ToObjectValue() is EnrichedEvent enrichedEvent)
        {
            var (nameArg, idArg) = arguments;

            var assetId = await idArg.EvaluateAsync(context);
            var asset = await ResolveAssetAsync(serviceProvider, enrichedEvent.AppId.Id, assetId);

            if (asset != null)
            {
                var name = (await nameArg.EvaluateAsync(context)).ToStringValue();

                context.SetValue(name, asset);
            }
        }

        return Completion.Normal;
    }

    private void AddAssetFilter(TemplateOptions options)
    {
        options.Filters.AddFilter("asset", async (input, arguments, context) =>
        {
            if (context.GetValue("event")?.ToObjectValue() is EnrichedEvent enrichedEvent)
            {
                var asset = await ResolveAssetAsync(serviceProvider, enrichedEvent.AppId.Id, input);

                if (asset == null)
                {
                    return NilValue.Instance;
                }

                return FluidValue.Create(asset, options);
            }

            return NilValue.Instance;
        });
    }

    private void AddAssetTextFilter(TemplateOptions options)
    {
        options.Filters.AddFilter("assetText", async (input, arguments, context) =>
        {
            if (input is not ObjectValue objectValue)
            {
                return ErrorNoAsset;
            }

            async Task<FluidValue> ResolveAssetTextAsync(AssetRef asset)
            {
                if (asset.FileSize > 256_000)
                {
                    return ErrorTooBig;
                }

                var assetFileStore = serviceProvider.GetRequiredService<IAssetFileStore>();

                var encoding = arguments.At(0).ToStringValue()?.ToUpperInvariant();
                var encoded = await asset.GetTextAsync(encoding, assetFileStore, default);

                return new StringValue(encoded);
            }

            switch (objectValue.ToObjectValue())
            {
                case IAssetEntity asset:
                    return await ResolveAssetTextAsync(asset.ToRef());

                case EnrichedAssetEvent @event:
                    return await ResolveAssetTextAsync(@event.ToRef());
            }

            return ErrorNoAsset;
        });

        options.Filters.AddFilter("assetBlurHash", async (input, arguments, context) =>
        {
            if (input is not ObjectValue objectValue)
            {
                return ErrorNoAsset;
            }

            async Task<FluidValue> ResolveAssetHashAsync(AssetRef asset)
            {
                if (asset.FileSize > 512_000)
                {
                    return ErrorTooBig;
                }

                if (asset.Type != AssetType.Image)
                {
                    return ErrorNoImage;
                }

                var options = new BlurOptions();

                var arg0 = arguments.At(0);
                var arg1 = arguments.At(1);

                if (arg0.Type == FluidValues.Number)
                {
                    options.ComponentX = (int)arg0.ToNumberValue();
                }

                if (arg1.Type == FluidValues.Number)
                {
                    options.ComponentX = (int)arg1.ToNumberValue();
                }

                var assetFileStore = serviceProvider.GetRequiredService<IAssetFileStore>();
                var assetThumbnailGenerator = serviceProvider.GetRequiredService<IAssetThumbnailGenerator>();

                var blur = await asset.GetBlurHashAsync(options, assetFileStore, assetThumbnailGenerator, default);

                return new StringValue(blur);
            }

            switch (objectValue.ToObjectValue())
            {
                case IAssetEntity asset:
                    return await ResolveAssetHashAsync(asset.ToRef());

                case EnrichedAssetEvent @event:
                    return await ResolveAssetHashAsync(@event.ToRef());
            }

            return ErrorNoAsset;
        });
    }

    private static async Task<IAssetEntity?> ResolveAssetAsync(IServiceProvider serviceProvider, DomainId appId, FluidValue id)
    {
        var appProvider = serviceProvider.GetRequiredService<IAppProvider>();

        var app = await appProvider.GetAppAsync(appId);

        if (app == null)
        {
            return null;
        }

        var domainId = DomainId.Create(id.ToStringValue());

        var assetQuery = serviceProvider.GetRequiredService<IAssetQueryService>();

        var requestContext =
            Context.Admin(app).Clone(b => b
                .WithoutTotal());

        var asset = await assetQuery.FindAsync(requestContext, domainId);

        return asset;
    }
}
