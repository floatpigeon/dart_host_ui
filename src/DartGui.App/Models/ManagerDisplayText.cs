namespace DartGui.App.Models;

public static class ManagerDisplayText
{
    public static string TaskName(string raw) => raw switch
    {
        "" => "空闲",
        "slider_init" => "滑块初始化",
        "launch_prepare" => "发射准备",
        "launch_cancel" => "取消上膛",
        "fire_preload" => "预装填发射",
        "cancel" => "取消 / 停止",
        "recover" => "恢复",
        _ => raw,
    };

    public static string ActionName(string raw) => raw switch
    {
        "" => "空闲",
        "belt_up" => "皮带上行",
        "belt_down" => "皮带下行",
        "trigger_lock" => "扳机锁定",
        "trigger_free" => "扳机释放",
        "filling_lift_up" => "填装升降上行",
        "filling_lift_down" => "填装升降下行",
        "filling_limit_servo" => "限位舵机动作",
        _ => raw,
    };

    public static string ReasonText(string raw) => raw switch
    {
        "bridge_offline" => "GuiBridge 未连接",
        "bridge_disconnected" => "GuiBridge 连接已断开",
        "send_failed" => "命令发送失败",
        "invalid_message" => "桥接消息格式错误",
        "unsupported_command" => "命令当前不受支持",
        "timeout" => "执行超时",
        "stall" => "检测到堵转",
        "external_cancel" => "任务被外部取消",
        "dependency_failure" => "依赖动作失败",
        _ => string.IsNullOrWhiteSpace(raw) ? "未知原因" : raw,
    };

    public static string TimestampText(long timestampMs)
    {
        if (timestampMs <= 0)
        {
            return "--:--:--";
        }

        return DateTimeOffset.FromUnixTimeMilliseconds(timestampMs).ToLocalTime().ToString("HH:mm:ss");
    }
}
