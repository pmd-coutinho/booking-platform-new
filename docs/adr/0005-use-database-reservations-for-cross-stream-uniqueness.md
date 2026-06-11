# Use Database Reservations For Cross-Stream Uniqueness

Cross-business uniqueness rules, starting with `Public Booking Slug`, will be enforced with strongly consistent database reservations or unique indexes alongside the relevant event append. The domain event records the chosen value, but projection-only checks are not sufficient because they can race across Business streams.

## Considered Options

- Projection-based uniqueness checks were rejected because projected read models may be stale and cannot safely prevent concurrent duplicate claims.
- Business-stream-only validation was rejected because slug uniqueness spans all businesses, not just one Business stream.

## Consequences

- Some domain commands may combine event appends with small consistency records in the Booking Database.
- These reservation records enforce invariants; they are not the source of domain history.
