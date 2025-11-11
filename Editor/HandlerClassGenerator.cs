using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace LocalRestAPI
{
    /// <summary>
    /// 生成处理器类（预生成 Handler），与参数解析器配合（模板驱动）
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
            var templatePath = Path.Combine(
                CodeGenerationConstants.TemplatesDirectory, "ApiHandlerTemplate.txt");

            if (!File.Exists(templatePath))
            {
                Debug.LogError($"ApiHandlerTemplate.txt 模板文件不存在: {templatePath}");
                return;
            }

            string template = File.ReadAllText(templatePath, Encoding.UTF8);

            template = template.Replace("{{HANDLER_CLASS_NAME}}", handlerName);
            template = template.Replace("{{CONTROLLER_FULL_NAME}}", controllerType.FullName.Replace("+","."));
            template = template.Replace("{{METHOD_NAME}}", methodInfo.Name);
            template = template.Replace("{{PARAMETER_PARSER_CLASS_NAME}}", parameterParserName);

            var castList = string.Join(", ",
                ParameterParserGenerator.GenerateParameterCastList(methodInfo));
            template = template.Replace("{{PARAMETER_CAST_LIST}}", castList);

            sb.AppendLine(template);
            sb.AppendLine();
        }
    }
}