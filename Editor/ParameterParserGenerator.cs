using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace LocalRestAPI
{
    /// <summary>
    /// 参数解析器代码生成器（基于模板）
    /// 模板使用运行时反射在 ParseParameters 内读取 {{CONTROLLER_FULL_NAME}}.{{METHOD_NAME}} 的参数签名，
    /// 从而避免在生成时硬编码参数数组。
    /// </summary>
    public static class ParameterParserGenerator
    {
        public static void GenerateParameterParserClass(StringBuilder sb, string className, MethodInfo methodInfo)
        {
            var templatePath = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Assets", "LocalRestAPI", "Editor", "ParameterParserTemplate.txt");
            if (!File.Exists(templatePath))
            {
                Debug.LogError($"ParameterParserTemplate.txt 模板文件不存在: {templatePath}");
                return;
            }

            string template = File.ReadAllText(templatePath, Encoding.UTF8);

            // 占位符替换
            template = template.Replace("{{PARAMETER_PARSER_CLASS_NAME}}", className);
            template = template.Replace("{{CONTROLLER_FULL_NAME}}", methodInfo.DeclaringType.FullName.Replace("+", "."));
            template = template.Replace("{{METHOD_NAME}}", methodInfo.Name);

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
                if (p.ParameterType == typeof(string))
                {
                    casts[i] = $"parameters[{i}] as string";
                }
                else
                {
                    casts[i] = $"({p.ParameterType.FullName.Replace("+", ".")})parameters[{i}]";
                }
            }
            return casts;
        }
    }
}