# deploymentrobotservice

#### 发布服务 (DeploymentRobotService)

+ 命令行解释服务
+ 命令执行/管理服务
+ 企业微信服务
+ 对外暴露服务
+ 应用管理服务
+ Web页面服务

#### 发布执行器 (MyDeploymentMonitor)

+ 通知机器人
  - 钉钉机器人
  - 企业微信机器人
  - 飞书机器人
  - 自定义机器人

+ 执行器
   - 执行者调度器
   - 进度管理及预测
   - 在线日志管理

+ 发布器
   - kubeSphereV2
   - kubeSphereV3
   - bamboo
   - rancher

+ 监视器
   - 定时监视器
   - gitlab监视器
   - rancher容器监视器


![image](https://github.com/lulianqi/DeploymentRobotService/assets/23115455/8c89eb6a-e8f4-4d8e-9f6c-8471b4ff78cf)
其中DeploymentRobotService为启动工程，其调试需要配置企业微信/钉钉/飞书应用回调及内外穿透
其中MyDeploymentMonitor执行器可独立以命令行终端的形式运行于本地
mac： dotnet MyBambooMonitor.dll
liunx： dotnet MyBambooMonitor.dll
windows： 双击 MyBambooMonitor.exe

DeploymentRobotService 以服务形式启动，同时提供web api及html web页面。
已包含dockerfile可以容器形式部署。

下面仅供参考，不同团队使用的CI平台可能不一样，需要实现自己的发布器。项目中配置文件关键/敏感信息已经删除，项目无法直接启动。

