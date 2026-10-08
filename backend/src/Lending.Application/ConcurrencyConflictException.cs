namespace Lending.Application;

public sealed class ConcurrencyConflictException(Exception inner)
    : Exception("A concurrent request modified the same customer.", inner);
