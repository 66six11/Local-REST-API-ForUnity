using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace LocalRestAPI
{
    /// <summary>
    /// 生成处理器类（预生成 Handler），与预生成的参数解析器配合（基于模板）
    /// </summary>
    public static class HandlerClassGenerator
    {
        public static void GenerateHandlerClass(
            StringBuilder sb,
            string handlerName,
            MethodInfo methodInfo,
            Type controllerType,
            string parameterParserName)
        {
            // 读取模板
            var templatePath = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Assets", "LocalRestAPI", "Editor", "ApiHandlerTemplate.txt");
            if (!File.Exists(templatePath))
            {
                Debug.LogError($"ApiHandlerTemplate.txt 模板文件不存在: {templatePath}");
                return;
            }

            string template = File.ReadAllText(templatePath, Encoding.UTF8);

            // 占位符替换
            template = template.Replace("{{HANDLER_CLASS_NAME}}", handlerName);
            template = template.Replace("{{CONTROLLER_FULL_NAME}}", controllerType.FullName.Replace("+", "."));
            template = template.Replace("{{METHOD_NAME}}", methodInfo.Name);
            template = template.Replace("{{PARAMETER_PARSER_CLASS_NAME}}", parameterParserName);

            // 生成参数强制转换列表（假定 ApiParameterParser 已把类型转成目标类型或默认值）
            var castList = string.Join(", ", ParameterParserGenerator.GenerateParameterCastList(methodInfo));
            template = template.Replace("{{PARAMETER_CAST_LIST}}", castList);

            // 追加到生成缓冲
            sb.AppendLine(template);
            sb.AppendLine();
        }
    }
}