using System.Collections;
using System.Reflection;
using CommandLine;

namespace UnrealSharpBuildTool;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class ActionAttribute : Attribute
{
    public string Name { get; set; }
    public string Description { get; set; }

    public ActionAttribute(string name, string description)
    {
        Name = name;
        Description = description;
    }
}

public static class ActionManager
{
    public static IList<MethodInfo> GetActions()
    {
        IList<MethodInfo> actions = new List<MethodInfo>();
        Assembly assembly = Assembly.GetExecutingAssembly();

        foreach (Type type in assembly.GetTypes())
        {
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                ActionAttribute? actionAttribute = method.GetCustomAttribute<ActionAttribute>();
                if (actionAttribute != null)
                {
                    actions.Add(method);
                }
            }
        }
        return actions;
    }

    public static void RunAction(string action, IEnumerable<string> args)
    {
        IList<MethodInfo> actions = GetActions();
        
        MethodInfo? methodToRun = null;
        foreach (MethodInfo actionMethod in actions)
        {
            ActionAttribute? actionAttribute = actionMethod.GetCustomAttribute<ActionAttribute>();
            if (actionAttribute == null || !actionAttribute.Name.Equals(action, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            
            methodToRun = actionMethod;
            break;
        }

        if (methodToRun == null)
        {
            Console.WriteLine($"Action '{action}' not found.");
            return;
        }

        ParameterInfo[] parameters = methodToRun.GetParameters();
        object?[] parameterValues = new object[parameters.Length];
        
        for (int i = 0; i < parameters.Length; i++)
        {
            ParameterInfo parameter = parameters[i];
            object? parameterValue = Activator.CreateInstance(parameter.ParameterType);
            
            if (parameterValue == null)
            {
                throw new InvalidOperationException($"Failed to create an instance of type '{parameter.ParameterType.Name}' for action parameter {methodToRun.Name}.{parameter.Name}.");
            }
            
            parameterValues[i] = parameterValue;
            IterateTypeMembers(args, parameter.ParameterType, parameterValue);
        }
        
        methodToRun.Invoke(null, parameterValues);
    }

    private static void IterateTypeMembers(IEnumerable<string> args, Type parameterType, object instance)
    {
        foreach (PropertyInfo member in parameterType.GetProperties())
        {
            Type memberType = member.PropertyType;
                    
            OptionAttribute? optionAttribute = member.GetCustomAttribute<OptionAttribute>();
            if (optionAttribute == null)
            {
                continue;
            }
                    
            if (IsStructOrClass(memberType))
            {
                object? value = Activator.CreateInstance(memberType);
                
                if (value == null)
                {
                    throw new InvalidOperationException($"Failed to create an instance of type '{memberType.Name}' for action option {member.DeclaringType!.Name}.{member.Name}.");
                }
                
                IterateTypeMembers(args, member.PropertyType, value);
                member.SetValue(instance, value);
            }
            else
            {
                IEnumerable<string> optionValue = args.Where(arg => 
                    arg.StartsWith($"{optionAttribute.LongName}=", StringComparison.OrdinalIgnoreCase) || 
                    arg.StartsWith($"{optionAttribute.ShortName}=", StringComparison.OrdinalIgnoreCase)
                );

                bool hasValue = optionValue.Any();
                
                if (!hasValue && optionAttribute.Required)
                {
                    throw new ArgumentException($"Required option '{optionAttribute.LongName}' is missing for action option {member.DeclaringType!.Name}.{member.Name}.");
                }
                            
                if (!hasValue)
                {
                    continue;
                }
                
                string value = optionValue.First().Split('=')[1];

                if (memberType.IsArray)
                {
                    string[] values = value.Split(',');
                    Array array = Array.CreateInstance(memberType.GetElementType()!, values.Length);

                    for (int i = 0; i < values.Length; i++)
                    {
                        object convertedValue;
                        string itemValue = values[i].Trim();
                        if (memberType.GetElementType()!.IsEnum)
                        {
                            if (!Enum.TryParse(memberType.GetElementType()!, itemValue, true, out object? enumValue))
                            {
                                throw new ArgumentException($"Invalid value '{itemValue}' for enum type '{memberType.Name}' in action option {member.DeclaringType!.Name}.{member.Name}.");
                            }
                            convertedValue = enumValue!;
                        }
                        else
                        {
                            convertedValue = Convert.ChangeType(itemValue, memberType.GetElementType());
                        }
                        array.SetValue(convertedValue, i);
                    }

                    member.SetValue(instance, array);
                }
                else
                {
                    object convertedValue;
                    if (memberType.IsEnum)
                    {
                        if (!Enum.TryParse(memberType, value, true, out object? outValue))
                        {
                            throw new ArgumentException($"Invalid value '{value}' for enum type '{memberType.Name}' in action option {member.DeclaringType!.Name}.{member.Name}.");
                        }
                        convertedValue = outValue!;
                    }
                    else
                    {
                        convertedValue = Convert.ChangeType(value, member.PropertyType);
                    }

                    member.SetValue(instance, convertedValue);
                }
            }
        }
    }
    
    public static void PrintActions()
    {
        Console.WriteLine("Available Actions:");
        Console.WriteLine("========================================");
        Console.WriteLine();

        IList<MethodInfo> actions = GetActions();
        foreach (MethodInfo action in actions)
        {
            ActionAttribute? actionAttribute = action.GetCustomAttribute<ActionAttribute>();
            if (actionAttribute == null)
            {
                continue;
            }

            Console.WriteLine($"Action: {actionAttribute.Name}");
            Console.WriteLine($"Description: {actionAttribute.Description}");
            
            if (action.GetParameters().Length > 0)
            {
                Console.WriteLine("Options:");
                foreach (ParameterInfo parameter in action.GetParameters())
                {
                    RecursivePrintOptions(parameter.ParameterType, 2);
                }
            }
            
            Console.WriteLine();
        }

        void RecursivePrintOptions(Type type, int indentLevel)
        {
            if (type == typeof(string) || type.IsPrimitive || type.IsEnum)
            {
                return;
            }
            
            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                OptionAttribute? option = property.GetCustomAttribute<OptionAttribute>();
                if (option == null)
                {
                    continue;
                }
                
                string shortName = string.IsNullOrEmpty(option.ShortName) ? string.Empty : $" ({option.ShortName})";
                Console.WriteLine($"{new string(' ', indentLevel * 2)}--{option.LongName}{shortName}: {option.HelpText} (Required: {option.Required})");
                
                if (IsStructOrClass(property.PropertyType))
                {
                    RecursivePrintOptions(property.PropertyType, indentLevel + 1);
                }
            }
        }
        
        Console.WriteLine("========================================");
    }
    
    static bool IsStructOrClass(Type type)
    {
        return (type.IsClass || type.IsValueType) && type != typeof(string) && !type.IsPrimitive && !type.IsEnum;
    }
}