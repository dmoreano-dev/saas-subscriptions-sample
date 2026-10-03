# Runbook: local email capture (Mailpit)

Local email never leaves the machine. `IEmailSender` (Application seam) is implemented by `SmtpEmailSender`
(Infrastructure/Email, MailKit) when `Email:Provider=Smtp`, which talks plain SMTP to a capture service.

## Inspect a message

1. Start the AppHost (`dotnet run --project src/aspire/Saas.Subscription.Sample.AppHost --launch-profile https`).
   Docker must be running.
2. Open the Aspire dashboard, find the `mailpit` resource and copy its `ui` endpoint (a dynamic port).
3. Send the synthetic HTML + text message through the API (Development only, not in the OpenAPI contract):
   `curl -X POST <api-url>/dev/email/test -H 'Content-Type: application/json' -d '{"to":"you@example.test"}'test`
   (`202 Accepted`; an invalid address answers `400`).
4. Open the Mailpit UI and check the HTML and Text tabs.

## Replace Mailpit with MailDev

The API only receives `Email__Smtp__Host` / `Email__Smtp__Port` from the AppHost; no code reads either product's
UI or API. In `AppHost.cs` change the image (`maildev/maildev`) and the target ports (SMTP 1025, UI 1080); nothing
else changes. The integration test `MailpitFixture` reads Mailpit's HTTP API and would need the same swap.

## Hosted

Hosted environments must use `Email:Provider=Https` (an SMTP provider is rejected at startup). Until the hosted
adapter exists (DEP-04) that provider resolves to `UnavailableEmailSender`: the host starts, but sending throws
`DependencyUnavailableException` (HTTP 503 where it surfaces). Capture is never evidence of inbox delivery.
