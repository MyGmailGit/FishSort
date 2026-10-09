# FishSort

此仓库根目录是可独立导入的Unity项目。运行脚本、场景、资源及.meta、Packages锁文件和ProjectSettings与原接受工程保持相同；不包含原工程的Library、构建包、历史备份、测试输出、原APK或研究报告。不包含个人凭据。

## 打开与运行

1. 使用 `git clone https://github.com/MyGmailGit/FishSort.git`，安装 **Unity 6000.4.6f1，revision 0b051c2e5d54**。本次在macOS以该版本验证；其他平台未实测。
2. Unity Hub选择Add，添加本目录，使用上述Editor打开。首次导入会生成Library缓存，等编译完成。
3. 打开 `Assets/Scenes/FishSort.unity`，Game视图选择约540×1320的竖屏比例，点击Play。场景主要提供Camera；`FishSortGame.Boot()`在场景加载后创建游戏对象。
4. 默认国家是`us`。首次礼包和关卡均为本地研究玩法，显示金额没有现金价值。

包清单只有Unity自带1.0.0模块（audio、imageconversion、imgui、jsonserialize、screencapture、physics、ui），锁文件source均为builtin；没有第三方包、原生插件或广告SDK安装依赖。字体、音效、图集、350关卡、10鱼种rig、51条动画元数据及金币轨道均已保留。运行通过Resources动态取资源，不能仅依据场景引用删除Resources内容或按文件名删除partial类脚本；现有检查代码被生产类引用，故也保留。

## 构建

macOS：在Unity的File > Build Profiles中选择macOS，安装对应构建支持，确认唯一启用场景为 `Assets/Scenes/FishSort.unity`，再Build到本地Builds目录。另有原工程保留的批处理入口 `RepairBuild.Run`：

```sh
"/Applications/Unity/Hub/Editor/6000.4.6f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -quit -projectPath "/absolute/path/to/FishSort" \
  -executeMethod RepairBuild.Run -logFile "/absolute/path/to/build.log"
```

此方法按当前项目相对路径输出 `Builds/FishSort Local Prototype.app` 和 `RepairEvidence/build-result.txt`；两者被.gitignore排除。其他Editor安装路径请相应替换。其他平台可通过Build Profiles选择目标；未实测，也没有在此配置Android签名、广告或支付。

本项目为了维持已有图像显示使用未压缩动画纹理。源码资源约165MB，生成的当前macOS APP仍约2.23GB，Library也会达到数GB；精简交付去掉了生成物，没有改变图像压缩或游戏逻辑。不要把生成的Library/APP提交到Git。

## 国家API、配置与广告接入

- 国家：实现 `FishSortLocal.ICountryProvider.GetCountry()`，经 `FishSortGame.SetCountryProvider(provider)` 注入。默认 `UnitedStatesCountryProvider` 返回`us`，保留其他国家的贴图及金额显示；本地不发国家API请求。提供器为同步接口，未来异步国家API应由接入层先取得结果再注入。
- 广告：实现 `IAdProvider.Show(AdRequest, Action<AdOutcome>)` 和 `Cancel()`，经 `SetAdProvider(provider)` 注入。现有普通启动使用显式本地广告模拟器；`SdkAdProviderPlaceholder`返回Unavailable。只有SDK确认满足可发奖条件的完成回调才映射 `EligibleClosed`；失败/取消/Unavailable保持对应结果。处理取消、重复与过期回调的协调器保持原实现。
- 规则：`IRuleConfigProvider`和`DefaultRuleConfigProvider`供本地静态默认配置使用。`configProvider`目前是游戏partial类内部成员，尚无公开SetConfigProvider方法；未来接入需另行改造，不能把“有接口”当作已经完成线上接入。
- 原生任务：`ITimeLimitTaskProvider`为本地替换接口，当前默认Unavailable；F8中的成功操作是明确本地模拟。原账户、任务/猪提现资格、白模式、SDK频控及实际支付/提现结算均未取得，也没有任何真实支付。

## 本地操作与验证

Z撤销、H提示、R重开、Escape关闭、F8研究面板、F12本地截图。存档写入Unity的 `Application.persistentDataPath/progress-v1.json`；测试须使用独立 `--evidence`路径，避免用户存档。

构建后可运行：

```sh
"Builds/FishSort Local Prototype.app/Contents/MacOS/FishSort Local Prototype" \
  --business-check --evidence "/absolute/path/to/isolated-checks" \
  -logFile "/absolute/path/to/player.log"
```

源视觉检查使用 `--visual-test --spine-visual-check --evidence <独立目录>`。这些检查会打开自己的测试窗口并自动结束，不需原APK、服务器或原生鼠标注入。本项目在独立无Library缓存副本中，以Unity6000.4.6f1完成全新导入、编译和macOS构建（0错误0警告）；同一新构建通过434项业务与98项源视觉断言，生成27张实际Unity渲染PNG。测试使用/tmp独立存档，日志和截图只保留在本地，不混入仓库。未宣称其他设备或平台均通过，也没有人工或原生鼠标验收。

## 版本控制与素材边界

保留全部.meta、ProjectSettings及Packages/manifest.json和packages-lock.json。当前最大单文件约6.54MB，没有Git LFS规则或LFS工具依赖；当前所有文件都低于GitHub常规Git的100MiB单文件限制（[官方说明](https://docs.github.com/en/repositories/working-with-files/managing-large-files/about-large-files-on-github)），无需LFS。后续引入大二进制时再选择性配置，避免对所有文件使用LFS。

本目录包含从第三方游戏取得的本地研究美术、字体、音效和关卡，不授予这些内容的商业使用或再分发权。分享或上传本项目不授予第三方内容的商业使用或再分发权；其他参与者应自行确认相应使用授权。
