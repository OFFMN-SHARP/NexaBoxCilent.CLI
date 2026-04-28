| 主命令 | 别名 | 来源 | 描述 |
|--------|------|------|------|
| `where <file/folder>` | `whereis`, `grep` | SQL / Linux | 搜索文件或文件夹 |
| `show <file>` | `fetch` | C# / HTTP | 展示文件属性/元数据 |
| `sed <file> to <path>` | `create_to` | Linux (Stream EDitor) | 创建并编辑文件，发送到目标路径 |
| `from <file> to <path>` | `download-to` | SQL | 从远端下载文件到指定路径 |
| `syscall <cmd>` | — | ASM | 执行本地系统命令 |
| `call <cmd>` | — | ASM | 执行工作台内部指令 |
| `whoami` | `i?` | Linux / 自创 | 显示当前登录用户身份 |
|`msg <message> to <touser>`|`send`,`write`,`msgo`|Windows|发送消息|
|`passwd <password>`|—|Linux|修改密码|
|`mkuser <username> with <initpassword>`|`invit`|—|创建用户|
|`chprmis <username> into <permisssions[]>`|—|—|修改用户权限|
|`rmuser`|—|—|删除用户|
|`push <file>`|`pushto`|Git|上传文件|