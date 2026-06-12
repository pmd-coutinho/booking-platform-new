# Allow Wolverine Marten Side-Effect Return Types in HTTP Endpoints

Wolverine HTTP endpoints may expose Wolverine/Marten side-effect return types, such as event stream side effects, when that is the mechanism that keeps HTTP handling and event persistence transactionally consistent. Direct persistence sessions and query dependencies remain banned from endpoint logic; the exception is limited to framework return values that describe transactional side effects for Wolverine to execute.
