using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace LocalRestAPI
{
    /// <summary>
    /// 参数解析器代码生成器：生成“硬编码参数元数据 + 统一解析器调用”的解析器类
    /// </summary>
    public static class ParameterParserGenerator
    {
        public static void GenerateParameterParserClass(
            StringBuilder sb,
            string className,
            MethodInfo methodInfo)
        {
            var templatePath = Path.Combine(
                Path.GetDirectoryName(Application.dataPath),
                "Assets", "LocalRestAPI", "Editor", "ParameterParserTemplate.txt");

            if (!File.Exists(templatePath))
            {
                Debug.LogError($"ParameterParserTemplate.txt 模板文件不存在: {templatePath}");
                return;
            }

            string template = File.ReadAllText(templatePath, Encoding.UTF8);

            var parameters = methodInfo.GetParameters();

            // 构造 ParamTypes
            var typesBuilder = new StringBuilder();
            // 构造 ParamNames
            var namesBuilder = new StringBuilder();
            // 构造 ParamDefaultValues
            var defaultsBuilder = new StringBuilder();

            for (int i = 0; i < parameters.Length; i++)
            {
                var p = parameters[i];
                bool last = i == parameters.Length - 1;

                // --- 类型数组项 ---
                typesBuilder.Append("        ");
                typesBuilder.Append("typeof(");
                typesBuilder.Append(FormatTypeForTypeof(p.ParameterType));
                typesBuilder.Append(")");
                if (!last) typesBuilder.Append(",");
                typesBuilder.Append(" // ");
                typesBuilder.Append(p.Name);
                typesBuilder.AppendLine();

                // --- 名称数组项 ---
                namesBuilder.Append("        ");
                namesBuilder.Append("\"");
                namesBuilder.Append(p.Name);
                namesBuilder.Append("\"");
                if (!last) namesBuilder.Append(",");
                namesBuilder.AppendLine();

                // --- 默认值数组项 ---
                defaultsBuilder.Append("        ");
                defaultsBuilder.Append(FormatDefaultValueLiteral(p));
                if (!last) defaultsBuilder.Append(",");
                defaultsBuilder.Append(" // ");
                defaultsBuilder.Append(p.Name);
                if (p.HasDefaultValue)
                {
                    defaultsBuilder.Append(" (has default)");
                }
                defaultsBuilder.AppendLine();
            }

            if (parameters.Length == 0)
            {
                // 保证数组不是空的语法错误，而是空数组
                typesBuilder.Append("        // (no parameters)\n");
                namesBuilder.Append("        // (no parameters)\n");
                defaultsBuilder.Append("        // (no parameters)\n");
            }

            template = template.Replace("{{PARAMETER_PARSER_CLASS_NAME}}", className);
            template = template.Replace("{{CONTROLLER_FULL_NAME}}", methodInfo.DeclaringType.FullName.Replace("+", "."));
            template = template.Replace("{{METHOD_NAME}}", methodInfo.Name);
            template = template.Replace("{{PARAMETER_TYPES_ARRAY}}", typesBuilder.ToString().TrimEnd());
            template = template.Replace("{{PARAMETER_NAMES_ARRAY}}", namesBuilder.ToString().TrimEnd());
            template = template.Replace("{{PARAMETER_DEFAULTS_ARRAY}}", defaultsBuilder.ToString().TrimEnd());

            sb.AppendLine(template);
            sb.AppendLine();
        }

        public static string[] GenerateParameterCastList(MethodInfo methodInfo)
        {
            var parameters = methodInfo.GetParameters();
            var casts = new string[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                var p = parameters[i];
                var t = p.ParameterType;

                // Nullable<T>
                if (IsNullable(t, out var underlying))
                {
                    var underlyingName = FormatFriendlyTypeName(underlying);
                    casts[i] = $"parameters[{i}] == null ? ({underlyingName}?)null : ({underlyingName})parameters[{i}]";
                    continue;
                }

                // 引用或数组或泛型：使用 as
                if (!t.IsValueType || t.IsArray || t.IsGenericType)
                {
                    casts[i] = $"parameters[{i}] as {FormatFriendlyTypeName(t)}";
                    continue;
                }

                // 枚举（解析器应已转换为该枚举）
                if (t.IsEnum)
                {
                    casts[i] = $"({FormatFriendlyTypeName(t)})parameters[{i}]";
                    continue;
                }

                // 普通值类型
                casts[i] = $"({FormatFriendlyTypeName(t)})parameters[{i}]";
            }
            return casts;
        }

        #region 辅助: 类型/默认值格式化

        private static bool IsNullable(Type t, out Type underlying)
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                underlying = t.GetGenericArguments()[0];
                return true;
            }
            underlying = null;
            return false;
        }

        // typeof(...) 中使用的类型字符串（泛型、数组、嵌套处理）
        private static string FormatTypeForTypeof(Type t)
        {
            if (t.IsGenericType)
            {
                var defName = t.GetGenericTypeDefinition().FullName;
                // 去掉 `1 之类
                defName = defName.Substring(0, defName.IndexOf('`'));
                var args = t.GetGenericArguments();
                return defName.Replace("+", ".") + "<" + string.Join(",",
                    args.Select(a => a.FullName.Replace("+", "."))) + ">";
            }

            if (t.IsArray)
            {
                return FormatTypeForTypeof(t.GetElementType()) + "[]";
            }

            return t.FullName.Replace("+", ".");
        }

        // 代码友好名称（cast 里）
        private static string FormatFriendlyTypeName(Type t)
        {
            if (t.IsGenericType)
            {
                var genericDefName = t.Name.Substring(0, t.Name.IndexOf('`'));
                var args = t.GetGenericArguments();
                return genericDefName + "<" + string.Join(", ", args.Select(FormatFriendlyTypeName)) + ">";
            }
            if (t.IsArray) return FormatFriendlyTypeName(t.GetElementType()) + "[]";

            if (t == typeof(int)) return "int";
            if (t == typeof(long)) return "long";
            if (t == typeof(short)) return "short";
            if (t == typeof(byte)) return "byte";
            if (t == typeof(bool)) return "bool";
            if (t == typeof(string)) return "string";
            if (t == typeof(char)) return "char";
            if (t == typeof(float)) return "float";
            if (t == typeof(double)) return "double";
            if (t == typeof(decimal)) return "decimal";

            return t.FullName.Replace("+", ".");
        }

        private static string FormatDefaultValueLiteral(ParameterInfo p)
        {
            if (!p.HasDefaultValue) return "null";

            object dv = p.DefaultValue;
            if (dv == null) return "null";

            var t = p.ParameterType;

            // 可空类型默认值若 dv==null -> null
            if (IsNullable(t, out _))
            {
                if (dv == null) return "null";
                // 若有非空默认值（罕见），尝试用内部类型格式
                return FormatLiteral(dv, Nullable.GetUnderlyingType(t));
            }

            return FormatLiteral(dv, t);
        }

        private static string FormatLiteral(object dv, Type type)
        {
            if (type == typeof(string))
            {
                return "\"" + EscapeString((string)dv) + "\"";
            }
            if (type == typeof(char))
            {
                return "'" + EscapeChar((char)dv) + "'";
            }
            if (type == typeof(bool))
            {
                return ((bool)dv) ? "true" : "false";
            }
            if (type.IsEnum)
            {
                var enumName = Enum.GetName(type, dv);
                if (enumName != null)
                {
                    return type.FullName.Replace("+", ".") + "." + enumName;
                }
                // fallback underlying numeric
                return "( " + type.FullName.Replace("+", ".") + ")" + Convert.ToInt64(dv);
            }
            if (type == typeof(float))
            {
                return ((float)dv).ToString(System.Globalization.CultureInfo.InvariantCulture) + "f";
            }
            if (type == typeof(double))
            {
                return ((double)dv).ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
            if (type == typeof(decimal))
            {
                return ((decimal)dv).ToString(System.Globalization.CultureInfo.InvariantCulture) + "m";
            }
            if (type == typeof(long))
            {
                return ((long)dv).ToString(System.Globalization.CultureInfo.InvariantCulture) + "L";
            }
            if (type == typeof(uint))
            {
                return ((uint)dv).ToString(System.Globalization.CultureInfo.InvariantCulture) + "u";
            }
            if (type == typeof(ulong))
            {
                return ((ulong)dv).ToString(System.Globalization.CultureInfo.InvariantCulture) + "UL";
            }
            if (type == typeof(int) || type == typeof(short) || type == typeof(byte) ||
                type == typeof(sbyte) || type == typeof(ushort))
            {
                return Convert.ToString(dv, System.Globalization.CultureInfo.InvariantCulture);
            }
            if (type == typeof(DateTime))
            {
                var dt = (DateTime)dv;
                return $"new System.DateTime({dt.Ticks}L, System.DateTimeKind.{dt.Kind})";
            }
            if (type == typeof(Guid))
            {
                return $"new System.Guid(\"{dv}\")";
            }

            // 引用类型默认值（未特殊处理）一律 null
            return "null";
        }

        private static string EscapeString(string s)
        {
            return s
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");
        }

        private static string EscapeChar(char c)
        {
            return c switch
            {
                '\\' => "\\\\",
                '\'' => "\\'",
                '\r' => "\\r",
                '\n' => "\\n",
                '\t' => "\\t",
                _ => c.ToString()
            };
        }

        #endregion
    }
}