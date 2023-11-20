// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Style", "IDE0063:Use simple 'using' statement", Justification = "NAI, Dev wants for readability.", Scope = "namespace", Target = "~N:Chizl.Crypto.aes")]
[assembly: SuppressMessage("Style", "IDE0066:Convert switch statement to expression", Justification = "NAI, 'coalescing assignment' is not available in C# 7.3", Scope = "namespace", Target = "~N:Chizl.Crypto.aes")]
[assembly: SuppressMessage("Style", "IDE0054:Use compound assignment", Justification = "NAI, 'coalescing assignment' is not available in C# 7.3", Scope = "namespace", Target = "~N:Chizl.JsonTables")]
