# Performance Reference

Microsoft ASP.NET Core guidance favors non-blocking request paths, bounded/paged large outputs, smaller allocations and optimized hot paths.

For this project:

- stream/chunk file/log reads;
- paginate diff/search/list results;
- never load an arbitrarily large file/diff into a single string;
- use async process stream reading to avoid deadlocks;
- keep tool metadata deterministic/cacheable;
- do not use Span/pooling complexity until profiling shows an allocation hotspot;
- prefer fewer subprocess invocations only when it does not weaken security filtering;
- benchmark search/diff only after functional/security correctness.
