﻿// ==========================================================================
//  SchemaUpdated.cs
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex Group
//  All rights reserved.
// ==========================================================================

using Squidex.Core.Schemas;
using Squidex.Infrastructure;
using Squidex.Infrastructure.CQRS.Events;

namespace Squidex.Events.Schemas
{
    [TypeName("SchemaUpdatedEvent")]
    public class SchemaUpdated : IEvent
    {
        public SchemaProperties Properties { get; set; }
    }
}
