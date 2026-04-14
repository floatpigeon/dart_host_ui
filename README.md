# DartHost.App

统一 C# Host 应用。该项目同时包含：

- Avalonia UI
- RMCS Dart TCP 协议客户端
- 应用内连接状态、命令发送、错误聚合

## Run

桌面模式：

```bash
dotnet run --project host/DartHost.App/DartHost.App.csproj -- --desktop
```

Linux DRM / framebuffer 路径：

```bash
dotnet run --project host/DartHost.App/DartHost.App.csproj -- --drm
```

如需显式走 fbdev：

```bash
dotnet run --project host/DartHost.App/DartHost.App.csproj -- --fbdev
```

也可以通过环境变量在 Linux 上强制启用 framebuffer 检测：

```bash
DART_HOST_USE_FRAMEBUFFER=1 dotnet run --project host/DartHost.App/DartHost.App.csproj
```

## Runtime Notes

- 默认连接 `127.0.0.1:37601`
- 启动后自动发送 `hello`，等待 `hello_ack` 和首帧 `manager_state`
- 断连后会自动重试连接
- `recover` / `cancel` 以及任务按钮都受 `hello_ack.supported_commands` 控制
