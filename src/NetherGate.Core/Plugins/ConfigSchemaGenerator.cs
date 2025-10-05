using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;
using System.Text.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace NetherGate.Core.Plugins;

/// <summary>
/// 配置模板生成器
/// 从 C# 类自动生成带注释的配置文件模板
/// </summary>
public class ConfigSchemaGenerator
{
    /// <summary>
    /// 生成 YAML 配置模板
    /// </summary>
    public static string GenerateYamlTemplate<T>() where T : new()
    {
        var instance = new T();
        return GenerateYamlTemplate(typeof(T), instance);
    }

    /// <summary>
    /// 生成 YAML 配置模板（带实例）
    /// </summary>
    public static string GenerateYamlTemplate(Type type, object? instance = null)
    {
        var sb = new StringBuilder();

        // 文件头部注释
        sb.AppendLine($"# {type.Name} 配置文件");
        sb.AppendLine($"# 自动生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();

        // 生成配置项
        GenerateYamlProperties(type, instance, sb, 0);

        return sb.ToString();
    }

    /// <summary>
    /// 生成 YAML 属性
    /// </summary>
    private static void GenerateYamlProperties(Type type, object? instance, StringBuilder sb, int indent)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite)
            .OrderBy(p => p.Name);

        foreach (var prop in properties)
        {
            var indentStr = new string(' ', indent);

            // 获取属性特性
            var description = prop.GetCustomAttribute<DescriptionAttribute>();
            var defaultValue = prop.GetCustomAttribute<DefaultValueAttribute>();
            var range = prop.GetCustomAttribute<RangeAttribute>();
            var required = prop.GetCustomAttribute<RequiredAttribute>();

            // 添加描述注释
            if (description != null)
            {
                sb.AppendLine($"{indentStr}# {description.Description}");
            }

            // 添加默认值注释
            if (defaultValue != null)
            {
                sb.AppendLine($"{indentStr}# 默认值: {FormatValue(defaultValue.Value)}");
            }

            // 添加范围注释
            if (range != null)
            {
                sb.AppendLine($"{indentStr}# 范围: {range.Minimum} - {range.Maximum}");
            }

            // 添加必填标记
            if (required != null)
            {
                sb.AppendLine($"{indentStr}# 必填");
            }

            // 获取属性值
            var value = instance != null ? prop.GetValue(instance) : GetDefaultValue(prop.PropertyType);
            var propertyName = ToCamelCase(prop.Name);

            // 生成 YAML
            if (IsComplexType(prop.PropertyType))
            {
                // 复杂类型
                sb.AppendLine($"{indentStr}{propertyName}:");

                if (value != null && !IsCollection(prop.PropertyType))
                {
                    GenerateYamlProperties(prop.PropertyType, value, sb, indent + 2);
                }
                else if (IsCollection(prop.PropertyType))
                {
                    // 集合类型
                    if (value is System.Collections.IEnumerable enumerable && value is not string)
                    {
                        var hasItems = false;
                        foreach (var item in enumerable)
                        {
                            hasItems = true;
                            if (IsSimpleType(item.GetType()))
                            {
                                sb.AppendLine($"{indentStr}  - {FormatValue(item)}");
                            }
                            else
                            {
                                sb.AppendLine($"{indentStr}  -");
                                GenerateYamlProperties(item.GetType(), item, sb, indent + 4);
                            }
                        }

                        if (!hasItems)
                        {
                            sb.AppendLine($"{indentStr}  []  # 空列表");
                        }
                    }
                    else
                    {
                        sb.AppendLine($"{indentStr}  []  # 空列表");
                    }
                }
            }
            else
            {
                // 简单类型
                sb.AppendLine($"{indentStr}{propertyName}: {FormatValue(value)}");
            }

            sb.AppendLine();
        }
    }

    /// <summary>
    /// 生成 JSON 配置模板
    /// </summary>
    public static string GenerateJsonTemplate<T>() where T : new()
    {
        var instance = new T();
        return GenerateJsonTemplate(typeof(T), instance);
    }

    /// <summary>
    /// 生成 JSON 配置模板（带实例）
    /// </summary>
    public static string GenerateJsonTemplate(Type type, object? instance = null)
    {
        var sb = new StringBuilder();

        // 文件头部注释（JSON 格式）
        sb.AppendLine("{");
        sb.AppendLine($"  \"$schema\": \"./schema.json\",");
        sb.AppendLine($"  \"_comment\": \"配置文件，生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\",");
        sb.AppendLine();

        // 生成配置项
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite)
            .ToList();

        for (int i = 0; i < properties.Count; i++)
        {
            var prop = properties[i];
            var value = instance != null ? prop.GetValue(instance) : GetDefaultValue(prop.PropertyType);
            var propertyName = ToCamelCase(prop.Name);

            // 添加描述注释
            var description = prop.GetCustomAttribute<DescriptionAttribute>();
            if (description != null)
            {
                sb.AppendLine($"  \"_{propertyName}_comment\": \"{description.Description}\",");
            }

            // 生成属性值
            sb.Append($"  \"{propertyName}\": {JsonSerializer.Serialize(value)}");

            if (i < properties.Count - 1)
            {
                sb.AppendLine(",");
            }
            else
            {
                sb.AppendLine();
            }
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// 转换为驼峰命名
    /// </summary>
    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        // PascalCase -> camelCase
        return char.ToLowerInvariant(name[0]) + name.Substring(1);
    }

    /// <summary>
    /// 转换为下划线命名
    /// </summary>
    private static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        var sb = new StringBuilder();
        sb.Append(char.ToLowerInvariant(name[0]));

        for (int i = 1; i < name.Length; i++)
        {
            if (char.IsUpper(name[i]))
            {
                sb.Append('_');
                sb.Append(char.ToLowerInvariant(name[i]));
            }
            else
            {
                sb.Append(name[i]);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// 格式化值为 YAML 格式
    /// </summary>
    private static string FormatValue(object? value)
    {
        if (value == null)
            return "null";

        if (value is string str)
            return NeedsQuotes(str) ? $"\"{str}\"" : str;

        if (value is bool b)
            return b ? "true" : "false";

        if (value is DateTime dt)
            return dt.ToString("yyyy-MM-dd HH:mm:ss");

        if (value.GetType().IsEnum)
            return value.ToString()!.ToLowerInvariant();

        return value.ToString() ?? "null";
    }

    /// <summary>
    /// 检查字符串是否需要引号
    /// </summary>
    private static bool NeedsQuotes(string str)
    {
        if (string.IsNullOrEmpty(str))
            return true;

        // 包含特殊字符需要引号
        var specialChars = new[] { ':', '#', '@', '[', ']', '{', '}', ',' };
        return specialChars.Any(c => str.Contains(c)) || 
               str.StartsWith(" ") || 
               str.EndsWith(" ");
    }

    /// <summary>
    /// 判断是否为简单类型
    /// </summary>
    private static bool IsSimpleType(Type type)
    {
        return type.IsPrimitive ||
               type.IsEnum ||
               type == typeof(string) ||
               type == typeof(DateTime) ||
               type == typeof(decimal) ||
               type == typeof(Guid);
    }

    /// <summary>
    /// 判断是否为复杂类型
    /// </summary>
    private static bool IsComplexType(Type type)
    {
        if (IsSimpleType(type))
            return false;

        if (IsCollection(type))
            return true;

        if (type.IsClass || type.IsInterface)
            return true;

        return false;
    }

    /// <summary>
    /// 判断是否为集合类型
    /// </summary>
    private static bool IsCollection(Type type)
    {
        if (type == typeof(string))
            return false;

        return typeof(System.Collections.IEnumerable).IsAssignableFrom(type);
    }

    /// <summary>
    /// 获取类型的默认值
    /// </summary>
    private static object? GetDefaultValue(Type type)
    {
        if (type.IsValueType)
            return Activator.CreateInstance(type);

        if (type == typeof(string))
            return string.Empty;

        if (IsCollection(type) && type.IsGenericType)
        {
            var listType = typeof(List<>).MakeGenericType(type.GetGenericArguments());
            return Activator.CreateInstance(listType);
        }

        return null;
    }
}

/// <summary>
/// 配置模板生成的辅助特性
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ConfigCommentAttribute : Attribute
{
    public string Comment { get; }

    public ConfigCommentAttribute(string comment)
    {
        Comment = comment;
    }
}

/// <summary>
/// 配置项示例特性
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ConfigExampleAttribute : Attribute
{
    public string Example { get; }

    public ConfigExampleAttribute(string example)
    {
        Example = example;
    }
}
