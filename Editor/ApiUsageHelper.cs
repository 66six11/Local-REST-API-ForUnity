using UnityEditor;
using UnityEngine;

namespace LocalRestAPI
{
    public class ApiUsageHelper
    {
        [MenuItem("Tools/Local REST API/使用说明")]
        public static void ShowUsageInstructions()
        {
            string instructions = 
                "Local REST API 使用说明\n\n" +
                "1. 启动服务:\n" +
                "   - 选择 'Tools/Local REST API/主控制台'\n" +
                "   - 点击 '启动服务' 按钮\n" +
                "   - 如遇到权限错误，请尝试以管理员身份运行Unity\n\n" +
                
                "2. 访问令牌:\n" +
                "   - 服务启动后会生成访问令牌（仅首次）\n" +
                "   - 访问令牌以明文形式显示，可直接编辑\n" +
                "   - 使用'重新生成令牌'按钮生成新令牌\n" +
                "   - 使用'复制令牌'按钮将令牌复制到剪贴板\n\n" +
                
                "3. 调用API:\n" +
                "   - 所有请求需包含访问令牌\n" +
                "   - 格式: Authorization: Bearer {token}\n" +
                "   - 或查询参数: ?token={token}\n\n" +
                
                "4. 查看日志:\n" +
                "   - 选择 'Tools/Local REST API/日志查看器'\n" +
                "   - 可以选择和复制日志内容\n" +
                "   - 使用'复制日志'按钮将日志复制到剪贴板\n" +
                "   - 使用'导出日志'按钮将日志保存到文件\n\n" +
                
                "5. 性能监控:\n" +
                "   - 选择 'Tools/Local REST API/性能监控'\n" +
                "   - 只显示以 /api/ 开头的API端点请求\n" +
                "   - 其他HTTP请求不会显示在监控中\n\n" +
                
                "6. 测试API:\n" +
                "   - 使用 'Tools/Local REST API/API测试工具' 打开测试窗口\n" +
                "   - 在测试窗口中可以选择特定的API接口进行测试\n" +
                "   - 可以自定义请求方法、URL和请求体\n" +
                "   - 测试窗口会显示实时的服务状态\n" +
                "   - 请求会异步处理，避免界面冻结\n" +
                "   - 可以使用'取消请求'按钮停止长时间运行的请求\n\n" +
                
                "7. 快速导航:\n" +
                "   - 在主控制台的路由列表中点击路由条目可跳转到对应代码\n" +
                "   - 使用'跳转到API服务器'按钮可快速打开核心代码\n\n" +
                
                "8. 创建自定义API:\n" +
                "   - 创建控制器类\n" +
                "   - 使用 [GetRoute] 或 [PostRoute] 属性标记方法\n" +
                "   - 方法将自动注册为API端点";
            
            EditorUtility.DisplayDialog("Local REST API 使用说明", instructions, "确定");
        }
    }
}