### NexaBox CLI 命令完全列表 (Build 4065)

| 主命令 | 别名 | 语法格式 | 描述 |
|--------|------|----------|------|
| `login` | — | `login <user> <pass>` | 登录到 Nexabox（启动参数或交互输入） |
| `where` | `whereis`, `grep` | `where <文件名/文件夹>` | 搜索云端文件（支持 `*.txt` 通配） |
| `show` | `fetch` | `show <id>` | 展示文件属性/元数据 |
| `sed` | `create` | `sed <文件名>` | 启动内置编辑器创建/编辑文件 |
| `from` | `download-to` | `from <文件> to <本地路径>` | 从云端下载文件 |
| `push` | `pushto` | `push <本地文件>` | 上传文件到云端 |
| `msg` | `send`, `write`, `msgo` | `msg <消息内容> to <用户名>` | 发送消息 |
| `passwd` | — | `passwd` | 修改当前用户密码 |
| `mkuser` | `invit` | `mkuser <用户名> with <初始密码>` | 管理员：创建新用户 |
| `chprmis` | — | `chprmis <用户名> into <权限列表>` | 管理员：修改用户权限（权限用 `@` 分隔） |
| `rmuser` | — | `rmuser <用户名>` | 管理员：删除用户 |
| `syscall` | — | `syscall <命令>` | 执行本地系统命令（跨平台） |
| `call` | — | `call <指令>` | 执行工作台内部指令（待实现） |
| `whoami` | `i?` | `whoami` | 显示当前登录用户信息 |
| `help` | — | `help` | 显示此帮助列表 |
| `exit` | — | `exit` | 退出 CLI |
| `cd` | — | `cd <路径>` | 切换本地工作目录 |
| `copy` | `cp` | `copy <源> to <目标>` | 复制本地文件 |
| `plks` | — | `plks <链接@链接…>` to <路径> | 批量解析分享链接并下载（支持 `*` 前缀） |
| `mkls` | — | `mkls <文件ID> with <密码>` | 创建文件分享链接（密码可选） |
