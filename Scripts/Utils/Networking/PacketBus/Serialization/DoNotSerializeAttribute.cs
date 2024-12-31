using System;

namespace NeonWarfare.Utils.Networking;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class DoNotSerializeAttribute : Attribute;