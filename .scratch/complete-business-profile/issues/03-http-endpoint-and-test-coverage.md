Status: done

## Parent

PRD: `.scratch/complete-business-profile/PRD.md`

## What to build

Add the `POST /api/businesses/{businessId}/profile` HTTP endpoint.

The endpoint should be thin: it reconstructs the Business from the stream, invokes the `CompleteBusinessProfile` domain command, coordinates the slug reservation, appends the resulting events, and returns the response.

The response includes `BookabilityStatus` and `BookabilityReasons`.

Add integration tests via Alba + PostgreSQL Testcontainer covering: happy path, invalid slug format, duplicate slug claim, invalid timezone, invalid currency, missing business, business with unaccepted manager, already-completed profile.

Add unit tests for the domain command and validation logic.

Update architecture tests to enforce that endpoints remain thin and do not directly depend on Marten, JasperFx event APIs, Npgsql, or Wolverine.Marten infrastructure.

## Acceptance criteria

- [x] `POST /api/businesses/{businessId}/profile` endpoint is implemented and returns correct status codes.
- [x] Integration tests cover all edge cases.
- [x] Unit tests cover domain command and validation.
- [x] Architecture tests continue to enforce thin endpoints.
- [x] All tests pass.

## Blocked by

- `.scratch/complete-business-profile/issues/02-complete-business-profile-domain-command-with-slug-reservation.md`
