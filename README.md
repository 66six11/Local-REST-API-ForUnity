# Local REST API

一个本地部署的API服务，用于将Unity客户端与外部服务进行交互。

## TODO List

-[ ] 简易mcp集成和部署
- [ ] 性能优化
- [ ] 测试集成

## 功能特性

1. **访问令牌认证** - 提供安全的API访问控制
2. **基于属性反射的API自动生成** - 通过在方法上添加特定属性，自动将方法暴露为API端点
3. **接口文档自动生成** - 自动生成API文档便于外部调用者理解
4. **接口访问日志记录** - 记录所有API调用详情用于审计和调试
5. **接口列表可视化** - 提供图形界面展示所有已注册的API端点
6. **接口测试工具** - 内置API测试工具便于开发调试
7. **接口性能监控** - 监控API响应时间和资源消耗
8. **接口错误日志记录** - 记录API调用过程中的错误信息
9. **配置持久化** - 支持服务地址和访问令牌的持久化存储
10. **端口冲突检测** - 自动检测端口可用性并提供故障排除指南
11. **路由代码跳转** - 可直接从路由列表跳转到对应的代码实现
12. **URL格式验证** - 验证URL格式正确性，确保服务正常启动

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
  - 服务状态监控和控制（启动/停止）
  - 服务地址配置和持久化
  - 访问令牌管理和重新生成
  - 实时性能监控（请求速率、总请求数、平均响应时间）
  - 已注册API路由列表，支持点击跳转到代码实现
  - 详细的服务日志查看和导出
- **日志查看器** - 查看详细的API请求和响应日志
- **性能监控** - 实时监控API性能指标
- **API测试工具** - 内置API测试工具便于开发调试

## 故障排除

如果遇到服务启动失败的问题，请参考以下指南：

1. **端口占用问题**:
   - 确保指定的端口没有被其他应用程序占用
   - 尝试更改端口（例如从8080改为8081）
   - 使用内置的端口检查功能查看使用情况
2. **权限问题**:
   - 在某些系统上，可能需要管理员权限来绑定到特定端口
   - 尝试以管理员身份运行Unity编辑器
3. **URL保留问题**:
   - Windows需要为HTTP服务器保留URL前缀
   - 如果看到错误代码5或183，请检查是否已有其他程序保留了相同的URL
4. **防火墙/安全软件**:
   - 检查防火墙设置是否阻止了应用程序
   - 临时禁用防火墙测试（记得之后重新启用）
5. **URL格式**:
   - 确保URL格式正确，例如: http://localhost:8080/
   - URL必须以/结尾
6. **检查是否有其他实例**:
   - 确保没有其他LocalRestAPI实例正在运行
   - 查看任务管理器中是否有其他Unity进程在运行API服务

## 配置管理

API配置（服务地址和访问令牌）会自动保存在项目目录下的 `.localrestapi/config.json` 文件中，确保配置在项目中持久化存储。
