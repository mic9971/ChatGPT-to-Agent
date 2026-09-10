# .NET Architecture Reference

Prefer a small number of meaningful projects over ceremonial Clean Architecture layers. A boundary earns a project/assembly when it protects dependency direction, security, packaging or independent testing.

Core rules:

- Domain/Core must not know ASP.NET endpoints, `Process`, Cloudflare, Git CLI or physical persistence.
- Infrastructure implements Core ports.
- Host is the composition root.
- CLI orchestrates application services and does not duplicate business/security logic.
- Avoid repository/service interfaces that simply mirror every method of a concrete class without a substitution/testing reason.
- Avoid generic “Manager/Helper/Common/Util” buckets; name by responsibility.
- Prefer cohesive vertical capability modules within projects over enormous folders by technical type.
- New abstraction requires at least two plausible implementations, a security/test seam, or isolation from a volatile dependency. Do not create interfaces for every class mechanically.
