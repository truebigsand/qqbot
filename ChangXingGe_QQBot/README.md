## 使用方法

在`ChangXingGe_QQBot`目录下创建`config.json`文件 格式如下：
``` json
{
  "ConnectionStrings": {
    "MongoDB": "..."
  },
  "Config": {
    "OneBotHost": "192.168.1.114",
    "OneBotPort": 8081,
    "SuperUsers": [ 114514, 1919810 ],
    "StartTime": "1970-01-01T00:00:00+08:00",
    "FriendRequestKey": "1145141919810",
    "SetuLimit": 5,
    "PersonalMessageRankLimit": 20
  }
}
```
之后在Visual Studio中将`config.json`文件添加进项目 并右键打开属性菜单将`复制到输出目录`更改为`如果较新则复制`
构建项目并运行即可