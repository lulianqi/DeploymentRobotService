# deploymentrobotservice
### 开发环境 
Dotnet 6.0
#### Windows
Visual Studio Code as IDE for .NET  
Visual Studio 2022 as IDE for .NET  
#### Mac OS
Visual Studio Code as IDE for .NET  
Visual Studio 2022 as IDE for .NET  

### 项目结构 
deploymentrobotservice由2部分组成。DeploymentRobotService服务本身，MyDeploymentMonitor执行器核心功能是实现各种不同发布平台的RPA及状态监控。

#### 发布服务 (DeploymentRobotService)

+ 命令行解释服务
+ 命令执行/管理服务
+ 办公IM企业应用服务
+ 对外API服务
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


其中DeploymentRobotService为启动工程，其调试需要配置企业微信/钉钉/飞书应用回调及内外穿透
其中MyDeploymentMonitor执行器可独立以命令行终端的形式运行于本地
+ mac： dotnet MyBambooMonitor.dll
+ liunx： dotnet MyBambooMonitor.dll
+ windows： 双击 MyBambooMonitor.exe

DeploymentRobotService 以服务形式启动，同时提供web api及html web页面。（自承载一个blazor sever服务，页面组件来自https://github.com/lulianqi/ant-design-blazor）  
已包含dockerfile可以容器形式部署。

*工程由测试程序演进而来，部分实现扩展性欠佳，项目仅供参考。*  
*项目中配置文件关键/敏感信息已经删除，相关服务无法直接初始化。*  
*不同团队使用的CI平台可能不一样，如果使用的不是已经实现发布器之一需要自行实现对应的发布器。*  

