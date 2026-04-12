# Dart Host TCP Protocol v1

## 1. Summary

本协议用于本机 Avalonia Host 与容器内 RMCS Dart 桥接组件之间的 TCP 通信。

- 传输层：`TCP`
- 编码：`UTF-8`
- 帧格式：`JSON Lines`
- 协议版本：`1`
- 连接模式：单连接
- 默认监听：本机回环地址
- 首版范围：控制命令、状态同步、错误回报、心跳保活

首版不包含：

- 鉴权
- 图像流
- 参数写入
- 二进制消息
- 增量状态 patch

## 2. Framing

每条消息必须满足以下约束：

- 一条消息对应一行完整 JSON
- 每条消息以 `\n` 结尾
- 不允许多行 JSON
- 单条消息最大长度为 `64 KiB`
- 桥接层收到非法 JSON、缺少关键字段、字段类型错误或超长消息时，应返回 `error`

示例：

```json
{"type":"heartbeat","protocol_version":1,"request_id":"","timestamp_ms":1712937601300,"payload":{"session_id":"sess-1"}}
```

## 3. Common Envelope

所有消息统一使用以下信封结构：

```json
{
  "type": "command",
  "protocol_version": 1,
  "request_id": "req-2",
  "timestamp_ms": 1712937600200,
  "payload": {}
}
```

字段定义：

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `type` | `string` | 消息类型 |
| `protocol_version` | `integer` | 固定为 `1` |
| `request_id` | `string` | 请求相关消息必须非空；推送消息可为空字符串 |
| `timestamp_ms` | `integer` | Unix epoch 毫秒时间戳，使用 `int64` |
| `payload` | `object` | 业务数据，不允许为 `null` |

通用约定：

- TCP 层所有枚举都序列化为字符串
- 空字符串统一使用 `""`
- 无对象时使用空对象 `{}`，不使用 `null`
- 未识别字段允许忽略
- 未识别 `type` 视为协议错误

## 4. Message Types

### 4.1 `hello`

Host 在建立 TCP 连接后发送的第一条消息，用于声明客户端信息。

```json
{
  "type": "hello",
  "protocol_version": 1,
  "request_id": "req-1",
  "timestamp_ms": 1712937600123,
  "payload": {
    "client_name": "dart-host",
    "client_version": "0.1.0",
    "capabilities": ["command", "state_subscribe"]
  }
}
```

`payload` 字段：

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `client_name` | `string` | 客户端名称 |
| `client_version` | `string` | 客户端版本 |
| `capabilities` | `string[]` | 当前客户端能力集合 |

### 4.2 `hello_ack`

桥接层对 `hello` 的应答。发送后应立即补发一帧 `manager_state` 全量状态。

```json
{
  "type": "hello_ack",
  "protocol_version": 1,
  "request_id": "req-1",
  "timestamp_ms": 1712937600130,
  "payload": {
    "server_name": "rmcs-dart-bridge",
    "server_version": "0.1.0",
    "session_id": "sess-1",
    "heartbeat_interval_ms": 1000,
    "state_push_interval_ms": 200,
    "supported_commands": [
      "launch_prepare",
      "launch_cancel",
      "fire_preload",
      "manual_control",
      "recover",
      "cancel"
    ]
  }
}
```

`payload` 字段：

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `server_name` | `string` | 服务端名称 |
| `server_version` | `string` | 服务端版本 |
| `session_id` | `string` | 当前连接会话 ID |
| `heartbeat_interval_ms` | `integer` | 心跳发送周期，单位毫秒 |
| `state_push_interval_ms` | `integer` | 状态推送最大发送间隔，单位毫秒 |
| `supported_commands` | `string[]` | 当前协议支持的命令列表 |

### 4.3 `command`

Host 发出的控制命令。

```json
{
  "type": "command",
  "protocol_version": 1,
  "request_id": "req-2",
  "timestamp_ms": 1712937600200,
  "payload": {
    "name": "launch_prepare",
    "args": {}
  }
}
```

`payload` 字段：

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `name` | `string` | 命令名称 |
| `args` | `object` | 首版保留字段，固定传空对象 `{}` |

支持的命令值：

- `launch_prepare`
- `launch_cancel`
- `fire_preload`
- `manual_control`
- `recover`
- `cancel`

说明：

- `command` 被接受，只表示桥接层已接收并写入 manager 命令入口
- `command` 被接受，不等于实际任务执行成功

### 4.4 `command_ack`

桥接层在接受 `command` 后返回的同步应答。

```json
{
  "type": "command_ack",
  "protocol_version": 1,
  "request_id": "req-2",
  "timestamp_ms": 1712937600205,
  "payload": {
    "accepted": true,
    "command_name": "launch_prepare"
  }
}
```

`payload` 字段：

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `accepted` | `boolean` | 是否已接受命令 |
| `command_name` | `string` | 回显收到的命令名 |

### 4.5 `manager_state`

桥接层向 Host 推送的 DartManager 全量状态快照。

- 连接建立并完成握手后立即发送一次
- 后续在状态变化时发送
- 若长时间无变化，则由心跳负责保活

```json
{
  "type": "manager_state",
  "protocol_version": 1,
  "request_id": "",
  "timestamp_ms": 1712937600300,
  "payload": {
    "lifecycle_state": "RUNNING",
    "current_task": "launch_prepare",
    "current_action": "belt_up",
    "fire_count": 1,
    "queue": [
      {
        "task_name": "fire_preload",
        "display_name": "发射并预装填",
        "status": "queued"
      }
    ],
    "last_error": null
  }
}
```

`payload` 字段：

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `lifecycle_state` | `string` | 生命周期状态 |
| `current_task` | `string` | 当前任务名，无任务时为空字符串 |
| `current_action` | `string` | 当前动作名，无动作时为空字符串 |
| `fire_count` | `integer` | 已发射计数，使用 `uint32` |
| `queue` | `QueueItem[]` | 当前排队任务列表 |
| `last_error` | `LastError \| null` | 最近一次错误，无错误时为 `null` |

`lifecycle_state` 可选值：

- `IDLE`
- `RUNNING`
- `ERROR`

`QueueItem` 定义：

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `task_name` | `string` | 内部任务名 |
| `display_name` | `string` | UI 展示名 |
| `status` | `string` | 首版固定为 `queued` |

`LastError` 定义：

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `task_name` | `string` | 失败任务名 |
| `action_name` | `string` | 失败动作名 |
| `reason` | `string` | 失败原因字符串 |
| `message` | `string` | 面向 UI 的错误描述 |
| `timestamp_ms` | `integer` | 错误时间戳 |

桥接层实现约定：

- 内部从 DartManager 侧读取 `queue_json` 和 `last_error_json`
- 桥接层负责在服务端将它们解析为结构化对象后再发送给 Host
- 如果解析失败：
  - `queue` 回退为 `[]`
  - `last_error` 回退为 `null`
  - 同时记录服务端日志

### 4.6 `heartbeat`

桥接层用于保活的空闲消息。当一个心跳周期内没有发送任何 `manager_state` 或 `error` 时，发送 `heartbeat`。

```json
{
  "type": "heartbeat",
  "protocol_version": 1,
  "request_id": "",
  "timestamp_ms": 1712937601300,
  "payload": {
    "session_id": "sess-1"
  }
}
```

`payload` 字段：

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `session_id` | `string` | 当前连接会话 ID |

### 4.7 `error`

协议错误、非法命令或服务端内部异常统一通过 `error` 返回。

```json
{
  "type": "error",
  "protocol_version": 1,
  "request_id": "req-2",
  "timestamp_ms": 1712937600210,
  "payload": {
    "code": "invalid_command",
    "message": "Unsupported command: launch_now",
    "details": {}
  }
}
```

`payload` 字段：

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `code` | `string` | 错误码 |
| `message` | `string` | 错误描述 |
| `details` | `object` | 附加信息，首版可为空对象 |

`code` 固定枚举：

- `bad_request`
- `invalid_json`
- `unsupported_type`
- `invalid_command`
- `internal_error`
- `protocol_version_mismatch`
- `session_replaced`

## 5. Connection Lifecycle

连接时序固定如下：

1. Host 建立 TCP 连接
2. Host 必须在 `3s` 内发送 `hello`
3. 桥接层返回 `hello_ack`
4. 桥接层立即发送一帧 `manager_state`
5. 后续 Host 可发送 `command`
6. 桥接层接收命令后尽快返回 `command_ack`
7. 状态变化时，桥接层推送 `manager_state`
8. 空闲时，桥接层按照心跳周期发送 `heartbeat`

超时和断线规则：

- 桥接层在连接建立后 `3s` 内未收到 `hello`，应断开连接
- Host 若连续 `5s` 未收到 `manager_state`、`heartbeat` 或 `error`，应判定连接失效并重连
- 新客户端接入时，旧连接应收到一条 `error(code=session_replaced)` 后断开

## 6. Type Mapping

### 6.1 C# 侧 DTO

建议固定以下 DTO：

- `Envelope`
- `HelloPayload`
- `HelloAckPayload`
- `CommandPayload`
- `CommandAckPayload`
- `ManagerStatePayload`
- `QueueItemDto`
- `LastErrorDto`
- `HeartbeatPayload`
- `ErrorPayload`

### 6.2 C++ 侧结构体

建议固定以下结构体：

- `Envelope`
- `HelloPayload`
- `HelloAckPayload`
- `CommandPayload`
- `CommandAckPayload`
- `ManagerStatePayload`
- `QueueItem`
- `LastError`
- `HeartbeatPayload`
- `ErrorPayload`

序列化约定：

- 时间戳统一使用 `int64`
- `fire_count` 在业务侧使用 `uint32`
- 所有枚举在 TCP 层一律输出字符串
- 没有值的字符串字段使用 `""`
- 没有值的对象字段使用 `{}` 或 `null`，仅 `last_error` 允许为 `null`

## 7. Validation Rules

桥接层至少需要验证以下内容：

- `protocol_version == 1`
- `type` 属于已知消息类型
- `payload` 为对象
- `command.name` 在允许列表中
- 单条消息长度不超过 `64 KiB`
- `hello` 必须是连接后的第一条业务消息

Host 至少需要验证以下内容：

- `hello_ack` 中的 `protocol_version`
- `manager_state` 的关键字段类型正确
- `error` 消息中的 `code` 与 `message` 可被正常显示
- 非法或未知消息不会导致 UI 崩溃

## 8. Test Cases

必须覆盖以下场景：

- 合法握手流程：`hello -> hello_ack -> manager_state`
- 六个合法命令都能通过 `command -> command_ack`
- 非法命令返回 `error(code=invalid_command)`
- 粘包、拆包情况下按换行正确组帧
- `queue_json` 和 `last_error_json` 解析失败时，`manager_state` 仍能发送
- 服务端无状态变化时按周期发送 `heartbeat`
- 单连接替换策略能触发 `session_replaced`
- Host 断线重连后能重新收到全量 `manager_state`

## 9. Defaults

首版默认值：

- `protocol_version = 1`
- `heartbeat_interval_ms = 1000`
- `state_push_interval_ms = 200`
- 单连接模式
- 本机回环地址监听
- `command.args = {}`
- `manager_state` 使用全量快照，不发送增量更新
