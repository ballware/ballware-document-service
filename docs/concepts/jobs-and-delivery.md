# Jobs and delivery

All background work runs in the **Service host** on Quartz.NET.

## Scheduler configuration

Production profile (`src/Ballware.Document.Service/appsettings.json`): clustered
`AdoJobStore` (`JobStoreTX`, PostgreSQL delegate, JSON serialization, `instanceId: AUTO`), so
multiple Service instances share one schedule; the host waits for running jobs on shutdown
(`src/Ballware.Document.Jobs/ServiceCollectionExtensions.cs:42-45`). A Swagger-only profile
switches to `RAMJobStore` (see [../openapi/README.md](../openapi/README.md)).

## Durable jobs

Registered durably in `src/Ballware.Document.Jobs/ServiceCollectionExtensions.cs:17-31`:

| Job key | Triggered by | JobDataMap keys |
| --- | --- | --- |
| `import` @ `document` / `notification` / `subscription` | `POST /document/<entity>/import` | `tenantId`, `userId`, `identifier`, `claims`, `file` (temporary storage id), `jobId` |
| `updatedatasources` @ `document` | `POST /document/document/updatedatasources` | `tenantId`, `userId`, `documentId` |
| `trigger` @ `subscription` | `POST /document/subscription/trigger`, cron frequency job | `tenantId`, optional `userId`, `subscriptionId` |
| `frequency` @ `subscription` | cron triggers from `Trigger:Active` | `frequency` |

`Trigger:Active` entries map a cron schedule to a frequency bucket:

```jsonc
{ "Name": "daily",  "Cron": "0 0 6 * * ?",   "Frequency": 1 }
```

Each due trigger runs `SubscriptionFrequencyJob`, which asks the Meta Service for report-allowed
tenants, loads `Active` subscriptions per tenant matching `Frequency`, and fans out one
`trigger` job per subscription (`src/Ballware.Document.Jobs/Internal/SubscriptionFrequencyJob.cs`).

## Import chain

`import` uploads are not processed inline: the endpoint stores the file as a temporary file via
the Storage Service, creates a job record in the Meta Service, and schedules the durable
`import` job; the job then runs the entity's import query definition per item and checks
`meta.<entity>.<identifier>` per item. See
[../api/generic-crud.md](../api/generic-crud.md#import).

## Delivery chain (`SubscriptionTriggerJob`)

`src/Ballware.Document.Jobs/Internal/SubscriptionTriggerJob.cs`:

1. Load the `Subscription` and its `Notification`; both must exist and the notification must
   reference a `DocumentId`.
2. Deserialize `Notification.DocumentParams` into report parameters and render the document
   (DevExpress core, PDF via SkiaSharp) — inline mail body or attachment named
   `Subscription.AttachmentFileName`.
3. Sending gate: `Mail:AllowedTenants` — when the list is non-empty, tenants not listed are
   rejected (`:74-77`); sending only happens when `Subscription.Mail` is set.
4. SMTP send with `Mail:Smtp*` settings; afterwards the send result is written back to the
   subscription: `"OK"` on success, otherwise the exception message (`:107-113` via
   `SubscriptionMetadataProvider.SetSendResultForSubscriptionAsync` → repository
   `SetLastErrorAsync`, which stamps `LastSendStamp` and stores the message in `LastError`).

Internal callers of the Service API can feed the same write-back path from outside via
`POST /document/subscription/setsendresult/{tenantId}/{id}` — the raw string body becomes
`LastError` ([../api/service-api.md](../api/service-api.md)).

## Datasource refresh (`DocumentUpdateDatasourcesJob`)

Runs `IDocumentModificationProvider.UpdateDatasourcesAsync(tenantId, userId, documentId)` — it
re-resolves the tenant's datasource definitions (Meta/Generic/ML) into the stored report
definition so reports track changed platform metadata. Triggered per document id via
`POST /document/document/updatedatasources`
(`src/Ballware.Document.Api/Endpoints/DocumentMetaEndpoint.cs:189-212`, ids are repeated `id`
**query** params).
