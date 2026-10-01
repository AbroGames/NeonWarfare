using Mono.Cecil;
using Mono.Cecil.Cil;

namespace NeonWarfare.RepoTests.Infrastructure;

/// <summary>
/// One place a type definition refers to another type. <see cref="From"/> is the member the reference is
/// written in; <see cref="Via"/> is the field or method of the referenced type when the reference is an
/// IL operand, so a rule can allow one member of a type and forbid the rest.
/// </summary>
public sealed record TypeReferenceSite(TypeReference Type, IMemberDefinition From, MemberReference? Via);

/// <summary>
/// Every type a type definition refers to: base type, interfaces, custom attributes, field, property and
/// event types, method signatures and generic constraints, local variables, caught exceptions and the IL
/// operands of method bodies — type, method and field references with their declaring type, signature
/// and generic arguments. Arrays, by-refs, pointers, modifiers and generic instances are unwrapped to
/// their element and argument types; generic parameters are not types and are left out.
/// <br/>
/// Only the type's own members are walked. Nested types, lambdas and state machines among them, are
/// separate entries of <see cref="GameAssembly.Types"/>; a rule decides whom they belong to.
/// </summary>
public static class TypeReferences
{
    private delegate void AddSite(TypeReference? reference, IMemberDefinition from, MemberReference? via);

    public static IEnumerable<TypeReferenceSite> Of(TypeDefinition type)
    {
        List<TypeReferenceSite> sites = [];
        void Add(TypeReference? reference, IMemberDefinition from, MemberReference? via = null)
        {
            foreach (TypeReference unwrapped in Unwrap(reference))
            {
                sites.Add(new TypeReferenceSite(unwrapped, from, via));
            }
        }

        Add(type.BaseType, type);
        foreach (InterfaceImplementation implementation in type.Interfaces)
        {
            Add(implementation.InterfaceType, type);
        }

        AddAttributes(type, type, Add);
        AddConstraints(type, type, Add);

        foreach (FieldDefinition field in type.Fields)
        {
            Add(field.FieldType, field);
            AddAttributes(field, field, Add);
        }

        foreach (PropertyDefinition property in type.Properties)
        {
            Add(property.PropertyType, property);
            AddAttributes(property, property, Add);
        }

        foreach (EventDefinition @event in type.Events)
        {
            Add(@event.EventType, @event);
            AddAttributes(@event, @event, Add);
        }

        foreach (MethodDefinition method in type.Methods)
        {
            AddMethod(method, Add);
        }

        return sites;
    }

    private static void AddMethod(MethodDefinition method, AddSite add)
    {
        add(method.ReturnType, method, null);
        AddAttributes(method, method, add);
        AddAttributes(method.MethodReturnType, method, add);
        AddConstraints(method, method, add);
        foreach (ParameterDefinition parameter in method.Parameters)
        {
            add(parameter.ParameterType, method, null);
            AddAttributes(parameter, method, add);
        }

        if (!method.HasBody)
        {
            return;
        }

        foreach (VariableDefinition variable in method.Body.Variables)
        {
            add(variable.VariableType, method, null);
        }

        foreach (ExceptionHandler handler in method.Body.ExceptionHandlers)
        {
            add(handler.CatchType, method, null);
        }

        foreach (Instruction instruction in method.Body.Instructions)
        {
            switch (instruction.Operand)
            {
                case TypeReference operand:
                    add(operand, method, null);
                    break;
                case FieldReference field:
                    add(field.DeclaringType, method, field);
                    add(field.FieldType, method, field);
                    break;
                case MethodReference called:
                    add(called.DeclaringType, method, called);
                    add(called.ReturnType, method, called);
                    foreach (ParameterDefinition parameter in called.Parameters)
                    {
                        add(parameter.ParameterType, method, called);
                    }

                    if (called is GenericInstanceMethod generic)
                    {
                        foreach (TypeReference argument in generic.GenericArguments)
                        {
                            add(argument, method, called);
                        }
                    }

                    break;
            }
        }
    }

    private static void AddAttributes(
        ICustomAttributeProvider provider,
        IMemberDefinition from,
        AddSite add)
    {
        if (!provider.HasCustomAttributes)
        {
            return;
        }

        foreach (CustomAttribute attribute in provider.CustomAttributes)
        {
            add(attribute.AttributeType, from, attribute.Constructor);
            IEnumerable<CustomAttributeArgument> arguments = attribute.ConstructorArguments
                .Concat(attribute.Fields.Select(named => named.Argument))
                .Concat(attribute.Properties.Select(named => named.Argument));
            foreach (CustomAttributeArgument argument in arguments)
            {
                // typeof(X) in an attribute is a reference to X as well.
                if (argument.Value is TypeReference typeOf)
                {
                    add(typeOf, from, attribute.Constructor);
                }
            }
        }
    }

    private static void AddConstraints(
        IGenericParameterProvider provider,
        IMemberDefinition from,
        AddSite add)
    {
        if (!provider.HasGenericParameters)
        {
            return;
        }

        foreach (GenericParameterConstraint constraint in provider.GenericParameters.SelectMany(p => p.Constraints))
        {
            add(constraint.ConstraintType, from, null);
        }
    }

    private static IEnumerable<TypeReference> Unwrap(TypeReference? reference)
    {
        switch (reference)
        {
            case null:
            case GenericParameter:
                yield break;
            case GenericInstanceType generic:
                foreach (TypeReference inner in Unwrap(generic.ElementType)
                             .Concat(generic.GenericArguments.SelectMany(Unwrap)))
                {
                    yield return inner;
                }

                break;
            case FunctionPointerType pointer:
                foreach (TypeReference inner in Unwrap(pointer.ReturnType)
                             .Concat(pointer.Parameters.SelectMany(parameter => Unwrap(parameter.ParameterType))))
                {
                    yield return inner;
                }

                break;
            // Arrays, by-refs, pointers, pinned and modified types.
            case TypeSpecification specification:
                foreach (TypeReference inner in Unwrap(specification.ElementType))
                {
                    yield return inner;
                }

                if (specification is IModifierType modified)
                {
                    foreach (TypeReference inner in Unwrap(modified.ModifierType))
                    {
                        yield return inner;
                    }
                }

                break;
            default:
                yield return reference;
                break;
        }
    }
}
