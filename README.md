# Local REST API

一个本地部署的API服务，用于将Unity客户端与外部服务进行交互。

## 功能特性

1. **访问令牌认证** - 提供安全的API访问控制
2. **基于属性反射的API自动生成** - 通过在方法上添加特定属性，自动将方法暴露为API端点
3. **接口文档自动生成** - 自动生成API文档便于外部调用者理解
4. **接口访问日志记录** - 记录所有API调用详情用于审计和调试
5. **接口列表可视化** - 提供图形界面展示所有已注册的API端点
6. **接口测试工具** - 内置API测试工具便于开发调试
7. **接口性能监控** - 监控API响应时间和资源消耗
8. **接口错误日志记录** - 记录API调用过程中的错误信息

## 使用场景

1. 外部AI使用MCP协议驱动Unity中工具暴露的API接口
2. 自己开发的工具需要暴露API接口供其他工具调用

## 快速开始

1. 在Unity编辑器中打开项目
2. 从菜单栏选择 `Tools/Local REST API/主控制台` 打开主控制台窗口
3. 点击"启动服务"按钮启动API服务
4. 服务启动后，可以使用访问令牌调用API接口

## API使用说明

### 认证

所有API请求都需要提供访问令牌。可以通过以下方式提供：

1. 在请求头中添加 `Authorization: Bearer {token}`
2. 在查询参数中添加 `token={token}`

### 示例API端点

- `GET /api/sample/hello?name=World` - 返回问候消息
- `POST /api/sample/echo` - 回显消息
- `GET /api/unity/scene` - 获取当前场景信息
- `GET /api/unity/objects` - 获取场景中的对象列表
- `POST /api/unity/log` - 在Unity控制台中记录消息

## 开发自定义API

要创建自定义API端点，请按照以下步骤操作：

1. 在 `LocalRestAPI` 命名空间中创建一个新的类
2. 为需要暴露为API的方法添加 `GetRoute` 或 `PostRoute` 属性
3. 定义方法参数，它们将自动从查询参数或POST数据中解析
4. 返回值将自动序列化为JSON响应

### 示例代码

```csharp
public class MyController
{
    [GetRoute("/api/my/hello")]
    public HelloResponse Hello(string name = "World")
    {
        return new HelloResponse
        {
            message = $"Hello, {name}!",
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };
    }
}

public class HelloResponse
{
    public string message;
    public string timestamp;
}
```

## 工具窗口

- **主控制台** - 服务控制、令牌管理、性能监控
- **日志查看器** - 查看详细的API请求和响应日志
- **性能监控** - 实时监控API性能指标

## 使用技巧

- 在主控制台的"已注册API路由"列表中，点击任意路由条目可直接跳转到对应的代码位置
- 使用"跳转到API服务器"按钮可快速打开API服务器核心代码
- 所有API端点都通过属性自动注册，无需手动配置路由
- 访问令牌在首次启动时生成，之后保持不变，可直接编辑
- 性能监控只显示以 /api/ 开头的API端点请求
- 日志记录只记录Local REST API服务处理的请求
- 在日志面板中可以直接选择和复制日志内容
- 使用"复制日志"按钮将日志复制到剪贴板
- 使用"导出日志"按钮将日志保存到文件
- 使用"API测试工具"可以测试特定的API接口
- API测试工具使用异步请求处理，避免界面冻结
- 可以使用"取消请求"按钮停止长时间运行的请求
- API测试工具在服务未运行时会显示清晰的状态信息

## 故障排除

- 如果启动服务时遇到"No path specified"错误，请检查URL格式是否正确（应以"http://"开头并以"/"结尾）
- 如果遇到权限错误，请尝试以管理员身份运行Unity
- 确保指定的端口没有被其他应用程序占用