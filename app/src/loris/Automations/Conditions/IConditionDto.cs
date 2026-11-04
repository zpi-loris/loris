using System.Text.Json.Serialization;
using Loris.Automations.Conditions.Test;

namespace Loris.Automations.Conditions;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(TestConditionDto), "test")]
public interface IConditionDto { }
