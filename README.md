# Workhorse Fuel · 牛马兴奋剂

在菜单栏或任务栏看这个月已经赚了多少。金额在排班时段内实时增长，休息时暂停。

## 下载与使用

到 [Releases](https://github.com/ZecousJ/Workhorse-Fuel/releases) 下载对应版本。

- **Windows**：下载 `Workhorse-Fuel-win-x64.zip` 或 `Workhorse-Fuel-win-arm64.zip`，解压后运行 `WorkhorseFuel.exe`。点击任务栏通知区域的牛角闪电图标打开计数器。如果图标在折叠菜单里，点击时钟旁的 **^**。右键图标可直接打开设置或退出。程序自带 .NET 运行时。
- **macOS 14 或更新版本**：打开 `Workhorse-Fuel-macOS.dmg`，将应用拖进「应用程序」。打开后，点击菜单栏的牛角闪电图标。应用同时支持 Apple 芯片和 Intel。

在设置中填写人民币月薪、上下班时间和排班。双休是周一至周五上班，单休是周一至周六上班，大小周在工作日之外隔周周六上班。选择「本周六上班」可对齐大小周。

## 计薪口径

本月已赚金额 = 月薪 × 本月已过的计薪工时 ÷ 本月排班总工时。跨午夜的班次按开始那天的排班判断，工时按实际落入的月份拆分。关闭面板、退出或休眠不影响按排班估算，修改设置会重新计算本月金额。设置仅保存在本机。

计数器按固定排班估算，不另扣午休，不识别法定节假日、调休、请假或实际打卡。

## 从源码构建

Windows 使用 .NET 10 SDK：

```powershell
dotnet run --project windows/WorkhorseFuel
dotnet run --project windows/Payroll.Tests
```

macOS 使用 Swift 6（Xcode 16 或更新版本）：

```sh
./macos/build.sh
./macos/test.sh
open "macos/build/牛马兴奋剂.app"
```
