# Local REST API 项目说明

## 项目概述

Local REST API 是一个为Unity项目设计的本地部署API服务，允许Unity客户端与外部服务进行交互。该项目通过基于属性反射的API自动生成机制，让开发者能够轻松地将Unity方法暴露为API端点。

主要功能包括：
- 访问令牌认证
- 基于属性反射的API自动生成
- 接口文档自动生成
- 接口访问日志记录
- 接口列表可视化
- 接口测试工具
- 接口性能监控
- 配置持久化
- 端口冲突检测
- 路由代码跳转

## 架构与核心组件

### 核心类

1. **ApiServer** (`Runtime\ApiServer.cs`): 主服务器类，负责启动HTTP监听器、路由注册、请求处理和响应发送。
2. **ApiRouteAttribute** (`Runtime\ApiRouteAttribute.cs`): 定义GET/POST路由的特性，用于标记API方法。
3. **ApiParameterParser** (`Runtime\ApiParameterParser.cs`): 负责解析HTTP请求中的参数，支持查询字符串、表单数据和JSON格式。
4. **MainThreadDispatcher** (`Runtime\MainThreadDispatcher.cs`): Unity主线程调度器，确保Unity相关操作在主线程执行。
5. **ApiRouteFinder** (`Editor\ApiRouteFinder.cs`): 在编辑器中查找所有标记了ApiRouteAttribute的方法。
6. **ApiCodeGenerator** (`Editor\ApiCodeGenerator.cs`): 生成API处理器代码和参数解析器代码。
7. **RouteRegistrarGenerator** (`Editor\RouteRegistrarGenerator.cs`): 生成路由注册代码。

### API定义示例

```csharp
public class SampleController
{
    [GetRoute("/api/sample/hello")]
    public HelloResponse Hello(string name = "World")
    {
        return new HelloResponse
        {
            message = $"Hello, {name}!",
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };
    }
}
```

### 自动生成机制

该项目采用代码生成机制来提高运行时性能：
1. 在编辑器中扫描所有标记了`[GetRoute]`或`[PostRoute]`特性的方法
2. 生成对应的API处理器类和参数解析器类
3. 生成路由注册代码
4. 在运行时直接使用预生成的类来处理请求

## 主要目录结构

```
LocalRestAPI/
├── Editor/                    # 编辑器相关代码
│   ├── ApiCodeGenerator.cs    # API代码生成器
│   ├── ApiRouteFinder.cs      # API路由查找器
│   ├── HandlerClassGenerator.cs # 处理器类生成器
│   ├── RouteRegistrarGenerator.cs # 路由注册器生成器
│   └── ...                    # 其他编辑器工具
├── Runtime/                   # 运行时相关代码
│   ├── ApiServer.cs           # API服务器核心
│   ├── ApiRouteAttribute.cs   # 路由特性
│   ├── ApiParameterParser.cs  # 参数解析器
│   └── ...                    # 其他运行时类
├── SampleController.cs        # 示例控制器
├── GeneratedApiHandlers.cs    # 自动生成的API处理器
├── GeneratedRouteRegistrar.cs # 自动生成的路由注册器
└── README.md                  # 项目说明文档
```

## 使用方法

### 启动服务

1. 在Unity编辑器中打开项目
2. 从菜单栏选择 `Tools/Local REST API/主控制台` 打开主控制台窗口
3. 点击"启动服务"按钮启动API服务
4. 服务启动后，可以使用访问令牌调用API接口

### API认证

所有API请求都需要提供访问令牌。可以通过以下方式提供：
1. 在请求头中添加 `Authorization: Bearer {token}`
2. 在查询参数中添加 `token={token}`

### 开发自定义API

要创建自定义API端点，请按照以下步骤操作：
1. 在 `LocalRestAPI` 命名空间中创建一个新的类
2. 为需要暴露为API的方法添加 `GetRoute` 或 `PostRoute` 属性
3. 定义方法参数，它们将自动从查询参数或POST数据中解析
4. 返回值将自动序列化为JSON响应

## 开发与调试

### 代码生成

项目使用代码生成来优化性能。当定义了新的API端点后，需要运行：
`Tools/Local REST API/生成API处理代码`

这将生成以下文件：
- `GeneratedApiHandlers.cs` - 包含API处理器和参数解析器
- `GeneratedRouteRegistrar.cs` - 包含路由注册代码

### 故障排除

常见问题包括：
- 端口占用问题
- 权限问题
- URL保留问题
- 防火墙/安全软件问题
- URL格式问题

## 技术特点

1. **反射与代码生成结合**: 既利用反射简化开发，又通过代码生成提升运行时性能
2. **Unity主线程安全**: 通过MainThreadDispatcher确保Unity相关操作的安全执行
3. **支持编辑器/播放器模式**: 在Unity编辑器模式和播放器模式下都能正常工作
4. **参数类型自动转换**: 支持从HTTP请求自动转换到C#类型
5. **完整的日志和监控**: 提供请求/响应日志、性能监控等功能