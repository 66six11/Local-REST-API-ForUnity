using System;
using System.IO;
using System.Reflection;
using System.Text;
using LocalRestAPI.Runtime;
using UnityEngine;

namespace LocalRestAPI
{
    /// <summary>
    /// 参数解析器生成器，用于生成解析API方法参数的类
    /// </summary>
    public static class ParameterParserGenerator
    {
        /// <summary>
        /// 生成参数解析器类
        /// </summary>
        /// <param name="sb">用于构建代码的StringBuilder</param>
        /// <param name="className">参数解析器类名</param>
        /// <param name="methodInfo">API方法信息</param>
        public static void GenerateParameterParserClass(StringBuilder sb, string className, MethodInfo methodInfo)
        {
            var parameters = methodInfo.GetParameters();
            
            sb.AppendLine($"        /// <summary>");
            sb.AppendLine($"        /// {methodInfo.DeclaringType.Name}.{methodInfo.Name} 方法的参数解析器");
            sb.AppendLine($"        /// </summary>");
            sb.AppendLine($"        public class {className} : IApiParameterParser");
            sb.AppendLine($"        {{");
            sb.AppendLine($"            public object[] ParseParameters(HttpListenerRequest request)");
            sb.AppendLine($"            {{");
            sb.AppendLine($"                // 准备参数信息");
            sb.AppendLine($"                var paramTypes = new System.Type[] {{ {string.Join(", ", GenerateParameterTypeList(parameters))} }};");
            sb.AppendLine($"                var paramNames = new string[] {{ {string.Join(", ", GenerateParameterNameList(parameters))} }};");
            sb.AppendLine($"                var paramDefaultValues = new object[] {{ {string.Join(", ", GenerateParameterDefaultValueList(parameters))} }};");
            sb.AppendLine($"");
            sb.AppendLine($"                // 使用公共参数解析器解析参数");
            sb.AppendLine($"                return ApiParameterParser.ParseParameters(request, paramTypes, paramNames, paramDefaultValues);");
            sb.AppendLine($"            }}");
            sb.AppendLine($"        }}");
            sb.AppendLine($"");
        }

        /// <summary>
        /// 生成类型转换后的参数列表字符串
        /// </summary>
        public static string[] GenerateParameterCastList(MethodInfo methodInfo)
        {
            var parameters = methodInfo.GetParameters();
            var paramNames = new string[parameters.Length];
            
            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                var paramType = GetCSTypeName(param.ParameterType);
                
                if (paramType == "string")
                {
                    if (param.DefaultValue == null || param.DefaultValue == DBNull.Value)
                    {
                        paramNames[i] = $"parameters[{i}] as string";
                    }
                    else
                    {
                        paramNames[i] = $"parameters[{i}] as string ?? \"{param.DefaultValue}\"";
                    }
                }
                else if (paramType == "int")
                {
                    var defaultValue = param.DefaultValue != null && param.DefaultValue != DBNull.Value ? param.DefaultValue : 0;
                    paramNames[i] = $"parameters[{i}] != null ? (int)parameters[{i}] : {defaultValue}";
                }
                else if (paramType == "float")
                {
                    var defaultValue = param.DefaultValue != null && param.DefaultValue != DBNull.Value ? 
                        (param.DefaultValue.ToString().EndsWith("f") ? param.DefaultValue.ToString() : $"{param.DefaultValue}f") : 
                        "0.0f";
                    paramNames[i] = $"parameters[{i}] != null ? (float)parameters[{i}] : {defaultValue}";
                }
                else if (paramType == "bool")
                {
                    var defaultValue = param.DefaultValue != null && param.DefaultValue != DBNull.Value ? 
                        param.DefaultValue.ToString().ToLower() : 
                        "false";
                    paramNames[i] = $"parameters[{i}] != null ? (bool)parameters[{i}] : {defaultValue}";
                }
                else
                {
                    paramNames[i] = $"parameters[{i}] != null ? ({paramType})parameters[{i}] : null";
                }
            }
            
            return paramNames;
        }

        /// <summary>
        /// 生成参数类型列表
        /// </summary>
        private static string[] GenerateParameterTypeList(ParameterInfo[] parameters)
        {
            var typeNames = new string[parameters.Length];
            
            for (int i = 0; i < parameters.Length; i++)
            {
                typeNames[i] = $"typeof({GetCSTypeName(parameters[i].ParameterType)})";
            }
            
            return typeNames;
        }

        /// <summary>
        /// 生成参数名称列表
        /// </summary>
        private static string[] GenerateParameterNameList(ParameterInfo[] parameters)
        {
            var paramNames = new string[parameters.Length];
            
            for (int i = 0; i < parameters.Length; i++)
            {
                paramNames[i] = $"\"{parameters[i].Name}\"";
            }
            
            return paramNames;
        }

        /// <summary>
        /// 生成参数默认值列表
        /// </summary>
        private static string[] GenerateParameterDefaultValueList(ParameterInfo[] parameters)
        {
            var defaultValues = new string[parameters.Length];
            
            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                
                if (param.DefaultValue != null && param.DefaultValue != DBNull.Value)
                {
                    if (param.ParameterType == typeof(string))
                    {
                        if (param.DefaultValue == null)
                        {
                            defaultValues[i] = "null";
                        }
                        else
                        {
                            defaultValues[i] = $"\"{param.DefaultValue}\"";
                        }
                    }
                    else if (param.ParameterType.IsValueType)
                    {
                        defaultValues[i] = param.DefaultValue.ToString();
                    }
                    else
                    {
                        defaultValues[i] = "null";
                    }
                }
                else
                {
                    defaultValues[i] = "null";
                }
            }
            
            return defaultValues;
        }

        /// <summary>
        /// 获取C#类型名称
        /// </summary>
        private static string GetCSTypeName(Type type)
        {
            if (type == typeof(string)) return "string";
            if (type == typeof(int)) return "int";
            if (type == typeof(float)) return "float";
            if (type == typeof(double)) return "double";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(long)) return "long";
            if (type == typeof(short)) return "short";
            if (type == typeof(byte)) return "byte";
            if (type == typeof(char)) return "char";
            if (type == typeof(decimal)) return "decimal";
            if (type == typeof(object)) return "object";
            
            return type.FullName.Replace("+", ".");
        }
    }
}