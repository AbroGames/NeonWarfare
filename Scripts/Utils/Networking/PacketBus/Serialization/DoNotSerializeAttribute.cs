using System;

namespace NeonWarfare.Scripts.Utils.Networking.PacketBus.Serialization;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class DoNotSerializeAttribute : Attribute;
